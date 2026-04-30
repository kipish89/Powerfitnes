using PowerFitness.App.Models;

namespace PowerFitness.App.Services;

public sealed class SessionSyncService(FitnessApiClient apiClient, AppState appState)
{
    public async Task<UserProfileVm?> TryRestoreCurrentUserAsync()
    {
        if (!string.IsNullOrWhiteSpace(appState.AccessToken))
        {
            var currentUserResult = await apiClient.GetCurrentUserAsync();
            if (currentUserResult.Success && currentUserResult.Data is not null)
            {
                appState.SetUser(currentUserResult.Data.Id, currentUserResult.Data.PhoneNumber);
                return currentUserResult.Data;
            }

            if (currentUserResult.IsUnauthorized)
            {
                appState.SetAccessToken(string.Empty);
            }
        }

        if (appState.CurrentUserId.HasValue)
        {
            var dashboard = await apiClient.GetDashboardAsync(appState.CurrentUserId.Value);
            if (dashboard?.User is not null)
            {
                if (!string.IsNullOrWhiteSpace(appState.CurrentUserPhone) &&
                    !string.Equals(
                        PhoneNumberNormalizer.Normalize(dashboard.User.PhoneNumber),
                        PhoneNumberNormalizer.Normalize(appState.CurrentUserPhone),
                        StringComparison.Ordinal))
                {
                    appState.SetUser(null, appState.CurrentUserPhone);
                }
                else
                {
                    appState.SetUser(dashboard.User.Id, dashboard.User.PhoneNumber);
                    return dashboard.User;
                }
            }
        }

        if (!appState.PendingTicketId.HasValue)
        {
            return null;
        }

        var status = await apiClient.GetRegistrationStatusAsync(appState.PendingTicketId.Value);
        if (status is null)
        {
            return null;
        }

        if (string.Equals(status.Status, "confirmed", StringComparison.OrdinalIgnoreCase) &&
            status.UserId.HasValue)
        {
            var authResult = await apiClient.ExchangeTelegramTicketAsync(status.TicketId);
            if (authResult.Success && authResult.Data is not null)
            {
                appState.SetPendingTicket(null);
                return authResult.Data.User;
            }

            var confirmedDashboard = await apiClient.GetDashboardAsync(status.UserId.Value);
            if (confirmedDashboard?.User is not null)
            {
                appState.SetUser(confirmedDashboard.User.Id, confirmedDashboard.User.PhoneNumber);
                return confirmedDashboard.User;
            }

            appState.SetUser(status.UserId.Value, appState.CurrentUserPhone);
        }

        if (status.ExpiresAtUtc <= DateTime.UtcNow)
        {
            appState.SetPendingTicket(null);
        }

        return null;
    }
}
