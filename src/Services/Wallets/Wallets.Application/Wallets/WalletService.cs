using Microsoft.Extensions.Logging;
using ValetaxTest.Core.Application;
using ValetaxTest.Core.Persistence;
using ValetaxTest.Wallets.Application.Abstractions;
using ValetaxTest.Wallets.Domain.Wallets;

namespace ValetaxTest.Wallets.Application.Wallets;

public sealed class WalletService(
    IWalletRepository wallets,
    IWalletReader reader,
    WalletRules walletRules,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<WalletService> logger) : IWalletService
{
    public async Task<WalletDto> GetAsync(string userId, CancellationToken cancellationToken) =>
        await reader.GetWalletAsync(userId, cancellationToken)
        ?? throw new NotFoundException($"Wallet of user '{userId}' does not exist.");

    public async Task<IReadOnlyList<WalletPayoutDto>> ListPayoutsAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        Pages.Validate(page, pageSize);
        if (await reader.GetWalletAsync(userId, cancellationToken) is null)
        {
            throw new NotFoundException($"Wallet of user '{userId}' does not exist.");
        }

        return await reader.ListPayoutsAsync(userId, page, pageSize, cancellationToken);
    }

    public async Task<WalletDto> BlockAsync(string userId, CancellationToken cancellationToken)
    {
        await using (var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            var wallet = await wallets.FindForUpdateAsync(userId, cancellationToken)
                ?? throw new NotFoundException($"Wallet of user '{userId}' does not exist.");

            if (walletRules.Block(wallet, timeProvider.GetUtcNow()))
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogWarning("Wallet of user {UserId} is blocked", userId);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        return await GetAsync(userId, cancellationToken);
    }

    public async Task<WalletDto> UnblockAsync(string userId, CancellationToken cancellationToken)
    {
        await using (var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            var wallet = await wallets.FindForUpdateAsync(userId, cancellationToken)
                ?? throw new NotFoundException($"Wallet of user '{userId}' does not exist.");

            if (walletRules.Unblock(wallet, timeProvider.GetUtcNow()))
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogWarning("Wallet of user {UserId} is unblocked", userId);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        return await GetAsync(userId, cancellationToken);
    }
}
