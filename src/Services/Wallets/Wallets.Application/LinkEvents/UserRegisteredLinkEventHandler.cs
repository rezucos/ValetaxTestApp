using Microsoft.Extensions.Logging;
using ValetaxTest.Core.Persistence;
using ValetaxTest.EventBus.Abstractions;
using ValetaxTest.Wallets.Domain.Wallets;

namespace ValetaxTest.Wallets.Application.LinkEvents;

/// <summary>Every user of the partner program has one wallet: it is opened when the user registers.</summary>
public sealed class UserRegisteredLinkEventHandler(
    IWalletRepository wallets,
    WalletRules walletRules,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<UserRegisteredLinkEventHandler> logger) : IMessageHandler<UserRegisteredLinkEvent>
{
    public async Task HandleAsync(UserRegisteredLinkEvent message, MessageContext context, CancellationToken cancellationToken)
    {
        if (await wallets.ExistsAsync(message.UserId, cancellationToken))
        {
            return;
        }

        wallets.Add(walletRules.Open(message.UserId, timeProvider.GetUtcNow()));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Wallet of user {UserId} opened", message.UserId);
    }
}
