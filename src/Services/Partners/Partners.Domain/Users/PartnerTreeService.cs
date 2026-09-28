using ValetaxTest.Core;

namespace ValetaxTest.Partners.Domain.Users;

/// <summary>
/// Rules of the partner tree: a user cannot be their own partner, and a link must not create a cycle
/// (the chain of partners above a user would never end).
/// </summary>
public sealed class PartnerTreeService
{
    public User Register(string externalId, string? partnerId, DateTimeOffset now)
    {
        if (string.Equals(externalId, partnerId, StringComparison.Ordinal))
        {
            throw new DomainException("A user cannot be their own partner.");
        }

        return new User
        {
            ExternalId = externalId,
            PartnerId = partnerId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Sets a new partner of the user.</summary>
    /// <param name="partnerUpline">All partners above the new partner; if the user is among them, the link would create a cycle.</param>
    /// <returns><c>false</c> when the user already has this partner.</returns>
    public bool AssignPartner(User user, string partnerId, IReadOnlyCollection<string> partnerUpline, DateTimeOffset now)
    {
        if (string.Equals(user.ExternalId, partnerId, StringComparison.Ordinal))
        {
            throw new DomainException("A user cannot be their own partner.");
        }

        if (string.Equals(user.PartnerId, partnerId, StringComparison.Ordinal))
        {
            return false;
        }

        if (partnerUpline.Contains(user.ExternalId, StringComparer.Ordinal))
        {
            throw new DomainException(
                $"User '{partnerId}' is in the downline of '{user.ExternalId}': the link would create a cycle.");
        }

        user.PartnerId = partnerId;
        user.UpdatedAt = now;
        return true;
    }

    /// <returns><c>false</c> when the user has no partner.</returns>
    public bool RemovePartner(User user, DateTimeOffset now)
    {
        if (user.PartnerId is null)
        {
            return false;
        }

        user.PartnerId = null;
        user.UpdatedAt = now;
        return true;
    }
}
