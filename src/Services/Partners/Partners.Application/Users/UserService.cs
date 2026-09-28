using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValetaxTest.Core;
using ValetaxTest.Core.Application;
using ValetaxTest.Core.Persistence;
using ValetaxTest.EventBus.Abstractions;
using ValetaxTest.Partners.Application.Abstractions;
using ValetaxTest.Partners.Application.Mappings;
using ValetaxTest.Partners.Domain.Users;

namespace ValetaxTest.Partners.Application.Users;

public sealed class UserService(
    IUserRepository users,
    IPartnerTreeReader reader,
    PartnerTreeService partnerTree,
    IValidator<RegisterUserRequest> registerValidator,
    IValidator<AssignPartnerRequest> assignPartnerValidator,
    IOutbox outbox,
    ITransactionalLock transactionalLock,
    IUnitOfWork unitOfWork,
    IOptions<PartnerTreeOptions> options,
    TimeProvider timeProvider,
    ILogger<UserService> logger) : IUserService
{
    public async Task<UserDto> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (await users.ExistsAsync(request.ExternalId, cancellationToken))
        {
            throw new ConflictException($"User '{request.ExternalId}' is already registered.");
        }

        if (request.PartnerId is not null && !await users.ExistsAsync(request.PartnerId, cancellationToken))
        {
            throw new DomainException($"Partner '{request.PartnerId}' is not registered.");
        }

        var user = partnerTree.Register(request.ExternalId, request.PartnerId, timeProvider.GetUtcNow());
        users.Add(user);

        // Wallets opens a wallet when it gets this message.
        outbox.Enqueue(Topics.UserRegistered, user.ExternalId, user.ToContract(), correlationId: user.ExternalId);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            throw new ConflictException($"User '{request.ExternalId}' is already registered.");
        }

        logger.LogInformation("User {UserId} registered with partner {PartnerId}", user.ExternalId, user.PartnerId ?? "<none>");

        return await reader.GetUserAsync(user.ExternalId, cancellationToken)
            ?? throw new InvalidOperationException($"Registered user '{user.ExternalId}' was not found.");
    }

    public Task<IReadOnlyList<UserDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        Pages.Validate(page, pageSize);
        return reader.ListUsersAsync(page, pageSize, cancellationToken);
    }

    public async Task<UserDto> GetAsync(string userId, CancellationToken cancellationToken) =>
        await reader.GetUserAsync(userId, cancellationToken)
        ?? throw new NotFoundException($"User '{userId}' is not registered.");

    public async Task<UserDto> AssignPartnerAsync(string userId, AssignPartnerRequest request, CancellationToken cancellationToken)
    {
        await assignPartnerValidator.ValidateAndThrowAsync(request, cancellationToken);

        await using (var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            // One link change at a time across all replicas: otherwise linking A to B and B to A at the same
            // time would pass the cycle check twice and create a cycle.
            await transactionalLock.AcquireAsync(LockKeys.PartnerTree, cancellationToken);

            var user = await users.FindAsync(userId, cancellationToken)
                ?? throw new NotFoundException($"User '{userId}' is not registered.");

            if (!await users.ExistsAsync(request.PartnerId, cancellationToken))
            {
                throw new DomainException($"Partner '{request.PartnerId}' is not registered.");
            }

            var partnerUpline = await users.GetUplineIdsAsync(request.PartnerId, cancellationToken);
            if (partnerTree.AssignPartner(user, request.PartnerId, partnerUpline, timeProvider.GetUtcNow()))
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Partner of user {UserId} set to {PartnerId}", user.ExternalId, user.PartnerId);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        return await GetAsync(userId, cancellationToken);
    }

    public async Task<UserDto> RemovePartnerAsync(string userId, CancellationToken cancellationToken)
    {
        await using (var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            // The same lock as in AssignPartnerAsync.
            await transactionalLock.AcquireAsync(LockKeys.PartnerTree, cancellationToken);

            var user = await users.FindAsync(userId, cancellationToken)
                ?? throw new NotFoundException($"User '{userId}' is not registered.");

            if (partnerTree.RemovePartner(user, timeProvider.GetUtcNow()))
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Partner link of user {UserId} removed", user.ExternalId);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        return await GetAsync(userId, cancellationToken);
    }

    public async Task<UplineDto> GetUplineAsync(string userId, int? depth, CancellationToken cancellationToken)
    {
        var maxDepth = options.Value.MaxDepth;
        if (depth is < 1 || depth > maxDepth)
        {
            throw new ValidationException([new ValidationFailure("depth", $"Depth must be between 1 and {maxDepth}.")]);
        }

        var levels = depth ?? maxDepth;

        if (await reader.GetUserAsync(userId, cancellationToken) is null)
        {
            throw new NotFoundException($"User '{userId}' is not registered.");
        }

        var partners = await reader.GetUplineAsync(userId, levels, cancellationToken);
        return new UplineDto(userId, levels, partners);
    }

    public async Task<DownlineDto> GetDownlineAsync(string userId, int? depth, CancellationToken cancellationToken)
    {
        var maxDepth = options.Value.MaxDepth;
        if (depth is < 1 || depth > maxDepth)
        {
            throw new ValidationException([new ValidationFailure("depth", $"Depth must be between 1 and {maxDepth}.")]);
        }

        var levels = depth ?? maxDepth;

        if (await reader.GetUserAsync(userId, cancellationToken) is null)
        {
            throw new NotFoundException($"User '{userId}' is not registered.");
        }

        var maxSize = options.Value.MaxDownlineSize;
        var rows = await reader.GetDownlineAsync(userId, levels, maxSize + 1, cancellationToken);
        var truncated = rows.Count > maxSize;
        if (truncated)
        {
            rows = rows.Take(maxSize).ToList();
        }

        // Rows are sorted by level, so the partner of every returned user is returned too (or is the root user).
        var referralsByPartner = rows.ToLookup(row => row.PartnerId, StringComparer.Ordinal);

        IReadOnlyList<DownlineNodeDto> Build(string partnerId) =>
            referralsByPartner[partnerId]
                .Select(row => new DownlineNodeDto(row.UserId, row.Level, Build(row.UserId)))
                .ToList();

        return new DownlineDto(userId, levels, rows.Count, truncated, Build(userId));
    }
}
