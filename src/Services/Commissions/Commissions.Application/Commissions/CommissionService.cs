using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using ValetaxTest.Commissions.Application.Abstractions;
using ValetaxTest.Commissions.Application.Mappings;
using ValetaxTest.Commissions.Domain.Accrual;
using ValetaxTest.Commissions.Domain.Schemas;
using ValetaxTest.Core.Application;
using ValetaxTest.Core.Persistence;

namespace ValetaxTest.Commissions.Application.Commissions;

public sealed class CommissionService(
    ICommissionReader reader,
    ICommissionSettingsRepository settings,
    CommissionSchemaService schemaService,
    IValidator<SwitchSchemaRequest> switchSchemaValidator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<CommissionService> logger) : ICommissionService
{
    private const int MaxPartnersPageSize = 1000;
    private const int SchemaHistorySize = 20;

    public Task<IReadOnlyList<CommissionDto>> ListAsync(
        string? partnerId,
        string? eventId,
        CommissionStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        Pages.Validate(page, pageSize);
        return reader.ListAsync(partnerId, eventId, status, page, pageSize, cancellationToken);
    }

    public Task<PartnersPage> ListPartnersWithAccruedAsync(string? afterPartnerId, int pageSize, CancellationToken cancellationToken)
    {
        if (pageSize is < 1 or > MaxPartnersPageSize)
        {
            throw new ValidationException([new ValidationFailure(nameof(pageSize), $"Page size must be between 1 and {MaxPartnersPageSize}.")]);
        }

        return reader.ListPartnersWithAccruedAsync(afterPartnerId, pageSize, cancellationToken);
    }

    public Task<CommissionSchemaDto> GetSchemaAsync(CancellationToken cancellationToken) =>
        reader.GetSchemaAsync(SchemaHistorySize, cancellationToken);

    public async Task<CommissionSchemaDto> SwitchSchemaAsync(SwitchSchemaRequest request, CancellationToken cancellationToken)
    {
        await switchSchemaValidator.ValidateAndThrowAsync(request, cancellationToken);
        var schemaType = request.ToDomain();

        await using (var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            var current = await settings.GetForUpdateAsync(cancellationToken);
            var change = schemaService.Switch(current, schemaType, timeProvider.GetUtcNow());
            if (change is not null)
            {
                settings.AddChange(change);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogWarning("Commission schema switched from {Previous} to {Current}", change.PreviousSchemaType, change.SchemaType);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        return await GetSchemaAsync(cancellationToken);
    }
}
