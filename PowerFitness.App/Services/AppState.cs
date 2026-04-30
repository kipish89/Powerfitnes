namespace PowerFitness.App.Services;

public sealed class AppState
{
    private const string PhoneKey = "current_user_phone";
    private const string UserIdKey = "current_user_id";
    private const string PendingTicketKey = "pending_registration_ticket_id";
    private const string AccessTokenKey = "access_token";

    public string CurrentUserPhone { get; private set; }
    public Guid? CurrentUserId { get; private set; }
    public Guid? PendingTicketId { get; private set; }
    public string AccessToken { get; private set; }

    public AppState()
    {
        CurrentUserPhone = Preferences.Default.Get(PhoneKey, string.Empty);
        var rawUserId = Preferences.Default.Get(UserIdKey, string.Empty);
        var rawPendingTicketId = Preferences.Default.Get(PendingTicketKey, string.Empty);
        AccessToken = Preferences.Default.Get(AccessTokenKey, string.Empty);
        CurrentUserId = Guid.TryParse(rawUserId, out var userId) ? userId : null;
        PendingTicketId = Guid.TryParse(rawPendingTicketId, out var pendingTicketId) ? pendingTicketId : null;
    }

    public void SetPhone(string phone)
    {
        var normalizedPhone = PhoneNumberNormalizer.Normalize(phone);
        if (!string.Equals(CurrentUserPhone, normalizedPhone, StringComparison.Ordinal))
        {
            CurrentUserId = null;
            Preferences.Default.Remove(UserIdKey);
            PendingTicketId = null;
            Preferences.Default.Remove(PendingTicketKey);
        }

        CurrentUserPhone = normalizedPhone;
        Preferences.Default.Set(PhoneKey, CurrentUserPhone);
    }

    public void SetUser(Guid? userId, string? phone = null)
    {
        CurrentUserId = userId;
        if (userId.HasValue)
        {
            Preferences.Default.Set(UserIdKey, userId.Value.ToString());
            PendingTicketId = null;
            Preferences.Default.Remove(PendingTicketKey);
        }
        else
        {
            Preferences.Default.Remove(UserIdKey);
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            CurrentUserPhone = PhoneNumberNormalizer.Normalize(phone);
            Preferences.Default.Set(PhoneKey, CurrentUserPhone);
        }
    }

    public void SetAccessToken(string token)
    {
        AccessToken = token ?? string.Empty;
        if (string.IsNullOrWhiteSpace(AccessToken))
        {
            Preferences.Default.Remove(AccessTokenKey);
            return;
        }

        Preferences.Default.Set(AccessTokenKey, AccessToken);
    }

    public void Logout()
    {
        CurrentUserId = null;
        PendingTicketId = null;
        AccessToken = string.Empty;
        Preferences.Default.Remove(UserIdKey);
        Preferences.Default.Remove(PendingTicketKey);
        Preferences.Default.Remove(AccessTokenKey);
    }

    public void SetPendingTicket(Guid? ticketId)
    {
        PendingTicketId = ticketId;
        if (ticketId.HasValue)
        {
            Preferences.Default.Set(PendingTicketKey, ticketId.Value.ToString());
        }
        else
        {
            Preferences.Default.Remove(PendingTicketKey);
        }
    }

    public void ClearUser()
    {
        CurrentUserId = null;
        CurrentUserPhone = string.Empty;
        PendingTicketId = null;
        AccessToken = string.Empty;
        Preferences.Default.Remove(UserIdKey);
        Preferences.Default.Remove(PhoneKey);
        Preferences.Default.Remove(PendingTicketKey);
        Preferences.Default.Remove(AccessTokenKey);
    }
}
