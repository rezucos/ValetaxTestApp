using ValetaxTest.Wallets.Application.Payouts.Messages;
using ValetaxTest.Wallets.Domain.Wallets;

namespace ValetaxTest.Wallets.Application.Mappings;

/// <summary>Builds messages sent by the service.</summary>
public static class DomainToContractMapper
{
    extension(WalletPayout payout)
    {
        /// <summary>Reply to the payout saga: <see cref="WalletCredited"/> or <see cref="WalletCreditRejected"/>.</summary>
        public object ToContract() => payout.Status == WalletPayoutStatus.Credited
            ? new WalletCredited(payout.PayoutId, payout.UserId, payout.Amount, payout.ProcessedAt)
            : new WalletCreditRejected(payout.PayoutId, payout.UserId, payout.RejectionReason ?? "Rejected");
    }
}
