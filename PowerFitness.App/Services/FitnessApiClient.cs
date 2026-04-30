using System.Net.Http.Headers;
using System.Net.Http.Json;
using PowerFitness.App.Models;

namespace PowerFitness.App.Services;

public sealed class FitnessApiClient(HttpClient httpClient, AppState appState)
{
    public async Task<ApiCallResult<AuthResponseVm>> RegisterAsync(RegisterRequestVm request)
    {
        request.PhoneNumber = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiEndpointResolver.BuildUri("api/auth/register"), request);
            if (!response.IsSuccessStatusCode)
            {
                return new ApiCallResult<AuthResponseVm>
                {
                    Message = response.StatusCode == System.Net.HttpStatusCode.BadRequest
                        ? "Регистрация отклонена сервером."
                        : "Не удалось выполнить регистрацию."
                };
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponseVm>();
            if (result is not null)
            {
                SaveAuthState(result);
            }

            return new ApiCallResult<AuthResponseVm>
            {
                Success = result is not null,
                Data = result,
                Message = result is null ? "Пустой ответ от сервера." : string.Empty
            };
        }
        catch
        {
            return new ApiCallResult<AuthResponseVm> { Message = "Сетевая ошибка при регистрации." };
        }
    }

    public async Task<ApiCallResult<AuthResponseVm>> LoginAsync(LoginRequestVm request)
    {
        request.PhoneNumber = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiEndpointResolver.BuildUri("api/auth/login"), request);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiCallResult<AuthResponseVm>
                {
                    IsUnauthorized = true,
                    Message = "Неверный номер или пароль."
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ApiCallResult<AuthResponseVm> { Message = "Не удалось выполнить вход." };
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponseVm>();
            if (result is not null)
            {
                SaveAuthState(result);
            }

            return new ApiCallResult<AuthResponseVm>
            {
                Success = result is not null,
                Data = result,
                Message = result is null ? "Пустой ответ от сервера." : string.Empty
            };
        }
        catch
        {
            return new ApiCallResult<AuthResponseVm> { Message = "Сетевая ошибка при входе." };
        }
    }

    public async Task<ApiCallResult<UserProfileVm>> GetCurrentUserAsync()
    {
        ApplyAuthorizationHeader();
        try
        {
            var response = await httpClient.GetAsync(ApiEndpointResolver.BuildUri("api/auth/me"));
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiCallResult<UserProfileVm> { IsUnauthorized = true, Message = "Токен недействителен." };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiCallResult<UserProfileVm> { IsNotFound = true, Message = "Пользователь не найден." };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ApiCallResult<UserProfileVm> { Message = "Не удалось загрузить текущего пользователя." };
            }

            return new ApiCallResult<UserProfileVm>
            {
                Success = true,
                Data = await response.Content.ReadFromJsonAsync<UserProfileVm>()
            };
        }
        catch
        {
            return new ApiCallResult<UserProfileVm> { Message = "Сетевая ошибка при запросе профиля." };
        }
    }

    public async Task<RegistrationStartResult> StartRegistrationAsync(string phoneNumber)
    {
        phoneNumber = PhoneNumberNormalizer.Normalize(phoneNumber);

        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiEndpointResolver.BuildUri("api/auth/start"), new { phoneNumber });
            if (!response.IsSuccessStatusCode)
            {
                return new RegistrationStartResult
                {
                    Status = "API недоступен. Проверь запуск PowerFitness.Api."
                };
            }

            var content = await response.Content.ReadFromJsonAsync<RegistrationTicketResponse>();
            return new RegistrationStartResult
            {
                TicketId = content?.TicketId ?? Guid.Empty,
                Status = "Запрос отправлен. Открой Telegram и подтверди данные.",
                DeepLink = content?.DeepLink ?? string.Empty
            };
        }
        catch
        {
            return new RegistrationStartResult
            {
                Status = "Не удалось связаться с API. Проверь адрес сервера и запуск PowerFitness.Api.",
                DeepLink = "https://t.me/iktrainingbot"
            };
        }
    }

    public async Task<RegistrationStatusResult?> GetRegistrationStatusAsync(Guid ticketId)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<RegistrationStatusResult>(
                ApiEndpointResolver.BuildUri($"api/auth/status/{ticketId}"));
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiCallResult<AuthResponseVm>> ExchangeTelegramTicketAsync(Guid ticketId)
    {
        try
        {
            var response = await httpClient.PostAsync(ApiEndpointResolver.BuildUri($"api/auth/telegram/exchange/{ticketId}"), null);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiCallResult<AuthResponseVm> { IsUnauthorized = true, Message = "Telegram-подтверждение ещё не завершено." };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ApiCallResult<AuthResponseVm> { Message = "Не удалось выполнить вход через Telegram." };
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponseVm>();
            if (result is not null)
            {
                SaveAuthState(result);
            }

            return new ApiCallResult<AuthResponseVm>
            {
                Success = result is not null,
                Data = result,
                Message = result is null ? "Пустой ответ от сервера." : string.Empty
            };
        }
        catch
        {
            return new ApiCallResult<AuthResponseVm> { Message = "Сетевая ошибка при входе через Telegram." };
        }
    }

    public async Task<DashboardVm?> GetDashboardAsync(Guid userId)
    {
        ApplyAuthorizationHeader();
        try
        {
            return await httpClient.GetFromJsonAsync<DashboardVm>(
                ApiEndpointResolver.BuildUri($"api/dashboard/{userId}"));
        }
        catch
        {
            return null;
        }
    }

    public void Logout()
    {
        appState.SetAccessToken(string.Empty);
        appState.ClearUser();
        httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<UserProfileVm?> GetUserByPhoneAsync(string phoneNumber)
    {
        phoneNumber = PhoneNumberNormalizer.Normalize(phoneNumber);
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        try
        {
            var response = await httpClient.GetAsync(
                ApiEndpointResolver.BuildUri($"api/auth/user-by-phone?phoneNumber={Uri.EscapeDataString(phoneNumber)}"));
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<UserProfileVm>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<MembershipPlanVm>> GetMembershipsAsync()
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<IReadOnlyList<MembershipPlanVm>>(
                ApiEndpointResolver.BuildUri("api/memberships"));
            if (result is not null)
            {
                return result;
            }
        }
        catch
        {
        }

        return GetFallbackCatalog().MembershipPlans;
    }

    public async Task<IReadOnlyList<WorkoutProgramVm>> GetWorkoutProgramsAsync()
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<IReadOnlyList<WorkoutProgramVm>>(
                ApiEndpointResolver.BuildUri("api/workouts"));
            if (result is not null)
            {
                return result;
            }
        }
        catch
        {
        }

        return GetFallbackCatalog().WorkoutPrograms;
    }

    public async Task<IReadOnlyList<TrainerProfileVm>> GetTrainersAsync()
    {
        try
        {
            var result = await httpClient.GetFromJsonAsync<IReadOnlyList<TrainerProfileVm>>(
                ApiEndpointResolver.BuildUri("api/trainers"));
            if (result is not null)
            {
                return result;
            }
        }
        catch
        {
        }

        return GetFallbackCatalog().Trainers;
    }

    public async Task<WorkoutProgramVm?> SaveTrainerProgramAsync(TrainerProgramUpsertRequestVm request)
    {
        ApplyAuthorizationHeader();
        try
        {
            var response = await httpClient.PostAsJsonAsync(ApiEndpointResolver.BuildUri("api/trainer/programs"), request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<WorkoutProgramVm>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<ApiCallResult<UserProfileVm>> UpdateUserAsync(UserProfileVm user)
    {
        ApplyAuthorizationHeader();
        try
        {
            var response = await httpClient.PutAsJsonAsync(ApiEndpointResolver.BuildUri($"api/users/{user.Id}"), user);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiCallResult<UserProfileVm> { IsUnauthorized = true, Message = "Нужен вход в систему." };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiCallResult<UserProfileVm> { IsNotFound = true, Message = "Пользователь не найден." };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ApiCallResult<UserProfileVm> { Message = "Не удалось сохранить профиль." };
            }

            return new ApiCallResult<UserProfileVm>
            {
                Success = true,
                Data = await response.Content.ReadFromJsonAsync<UserProfileVm>()
            };
        }
        catch
        {
            return new ApiCallResult<UserProfileVm> { Message = "Сетевая ошибка при сохранении профиля." };
        }
    }

    public async Task<ApiCallResult<MembershipPlanVm>> SaveMembershipAsync(MembershipPlanVm plan)
    {
        ApplyAuthorizationHeader();
        try
        {
            HttpResponseMessage response;
            if (plan.Id == Guid.Empty)
            {
                plan.Id = Guid.NewGuid();
                response = await httpClient.PostAsJsonAsync(ApiEndpointResolver.BuildUri("api/memberships"), plan);
            }
            else
            {
                response = await httpClient.PutAsJsonAsync(ApiEndpointResolver.BuildUri($"api/memberships/{plan.Id}"), plan);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiCallResult<MembershipPlanVm> { IsUnauthorized = true, Message = "Нужен вход в систему." };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiCallResult<MembershipPlanVm> { IsNotFound = true, Message = "Абонемент не найден." };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ApiCallResult<MembershipPlanVm> { Message = "Не удалось сохранить абонемент." };
            }

            return new ApiCallResult<MembershipPlanVm>
            {
                Success = true,
                Data = await response.Content.ReadFromJsonAsync<MembershipPlanVm>()
            };
        }
        catch
        {
            return new ApiCallResult<MembershipPlanVm> { Message = "Сетевая ошибка при сохранении абонемента." };
        }
    }

    public async Task<ApiCallResult<bool>> DeleteMembershipAsync(Guid id)
    {
        ApplyAuthorizationHeader();
        try
        {
            var response = await httpClient.DeleteAsync(ApiEndpointResolver.BuildUri($"api/memberships/{id}"));
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiCallResult<bool> { IsUnauthorized = true, Message = "Нужен вход в систему." };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiCallResult<bool> { IsNotFound = true, Message = "Абонемент не найден." };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ApiCallResult<bool> { Message = "Не удалось удалить абонемент." };
            }

            return new ApiCallResult<bool> { Success = true, Data = true };
        }
        catch
        {
            return new ApiCallResult<bool> { Message = "Сетевая ошибка при удалении абонемента." };
        }
    }

    public async Task<ApiCallResult<WorkoutProgramVm>> SaveWorkoutAsync(WorkoutProgramVm workout)
    {
        ApplyAuthorizationHeader();
        try
        {
            HttpResponseMessage response;
            if (workout.Id == Guid.Empty)
            {
                workout.Id = Guid.NewGuid();
                if (workout.CreatedAtUtc == default)
                {
                    workout.CreatedAtUtc = DateTime.UtcNow;
                }

                workout.UpdatedAtUtc = DateTime.UtcNow;
                response = await httpClient.PostAsJsonAsync(ApiEndpointResolver.BuildUri("api/workouts"), workout);
            }
            else
            {
                workout.UpdatedAtUtc = DateTime.UtcNow;
                response = await httpClient.PutAsJsonAsync(ApiEndpointResolver.BuildUri($"api/workouts/{workout.Id}"), workout);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiCallResult<WorkoutProgramVm> { IsUnauthorized = true, Message = "Нужен вход в систему." };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiCallResult<WorkoutProgramVm> { IsNotFound = true, Message = "Программа не найдена." };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ApiCallResult<WorkoutProgramVm> { Message = "Не удалось сохранить программу." };
            }

            return new ApiCallResult<WorkoutProgramVm>
            {
                Success = true,
                Data = await response.Content.ReadFromJsonAsync<WorkoutProgramVm>()
            };
        }
        catch
        {
            return new ApiCallResult<WorkoutProgramVm> { Message = "Сетевая ошибка при сохранении программы." };
        }
    }

    public async Task<ApiCallResult<bool>> DeleteWorkoutAsync(Guid id)
    {
        ApplyAuthorizationHeader();
        try
        {
            var response = await httpClient.DeleteAsync(ApiEndpointResolver.BuildUri($"api/workouts/{id}"));
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiCallResult<bool> { IsUnauthorized = true, Message = "Нужен вход в систему." };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiCallResult<bool> { IsNotFound = true, Message = "Программа не найдена." };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ApiCallResult<bool> { Message = "Не удалось удалить программу." };
            }

            return new ApiCallResult<bool> { Success = true, Data = true };
        }
        catch
        {
            return new ApiCallResult<bool> { Message = "Сетевая ошибка при удалении программы." };
        }
    }

    public async Task<IReadOnlyList<UserProfileVm>> GetUsersAsync()
    {
        ApplyAuthorizationHeader();
        try
        {
            var result = await httpClient.GetFromJsonAsync<IReadOnlyList<UserProfileVm>>(ApiEndpointResolver.BuildUri("api/users"));
            return result ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ApiCallResult<bool>> DeleteUserAsync(Guid id)
    {
        ApplyAuthorizationHeader();
        try
        {
            var response = await httpClient.DeleteAsync(ApiEndpointResolver.BuildUri($"api/users/{id}"));
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiCallResult<bool> { IsUnauthorized = true, Message = "Нужен вход в систему." };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiCallResult<bool> { IsNotFound = true, Message = "Пользователь не найден." };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ApiCallResult<bool> { Message = "Не удалось удалить пользователя." };
            }

            return new ApiCallResult<bool> { Success = true, Data = true };
        }
        catch
        {
            return new ApiCallResult<bool> { Message = "Сетевая ошибка при удалении пользователя." };
        }
    }

    public async Task<ApiCallResult<TrainerProfileVm>> SaveTrainerAsync(TrainerProfileVm trainer)
    {
        ApplyAuthorizationHeader();
        try
        {
            HttpResponseMessage response;
            if (trainer.Id == Guid.Empty)
            {
                trainer.Id = Guid.NewGuid();
                response = await httpClient.PostAsJsonAsync(ApiEndpointResolver.BuildUri("api/trainers"), trainer);
            }
            else
            {
                response = await httpClient.PutAsJsonAsync(ApiEndpointResolver.BuildUri($"api/trainers/{trainer.Id}"), trainer);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiCallResult<TrainerProfileVm> { IsUnauthorized = true, Message = "Нужен вход в систему." };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiCallResult<TrainerProfileVm> { IsNotFound = true, Message = "Тренер не найден." };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ApiCallResult<TrainerProfileVm> { Message = "Не удалось сохранить тренера." };
            }

            return new ApiCallResult<TrainerProfileVm>
            {
                Success = true,
                Data = await response.Content.ReadFromJsonAsync<TrainerProfileVm>()
            };
        }
        catch
        {
            return new ApiCallResult<TrainerProfileVm> { Message = "Сетевая ошибка при сохранении тренера." };
        }
    }

    public async Task<ApiCallResult<bool>> DeleteTrainerAsync(Guid id)
    {
        ApplyAuthorizationHeader();
        try
        {
            var response = await httpClient.DeleteAsync(ApiEndpointResolver.BuildUri($"api/trainers/{id}"));
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiCallResult<bool> { IsUnauthorized = true, Message = "Нужен вход в систему." };
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new ApiCallResult<bool> { IsNotFound = true, Message = "Тренер не найден." };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new ApiCallResult<bool> { Message = "Не удалось удалить тренера." };
            }

            return new ApiCallResult<bool> { Success = true, Data = true };
        }
        catch
        {
            return new ApiCallResult<bool> { Message = "Сетевая ошибка при удалении тренера." };
        }
    }

    public async Task<PurchaseStartResult> CreatePurchaseAsync(Guid userId, string productCode, string productType)
    {
        ApplyAuthorizationHeader();
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                ApiEndpointResolver.BuildUri("api/purchases"),
                new { userId, productCode, productType });

            if (!response.IsSuccessStatusCode)
            {
                var apiError = await response.Content.ReadFromJsonAsync<ApiMessageResponse>();
                return new PurchaseStartResult
                {
                    Status = apiError?.Message ?? "Не удалось создать счёт."
                };
            }

            var content = await response.Content.ReadFromJsonAsync<PurchaseResponse>();
            return new PurchaseStartResult
            {
                Status = "Счёт создан. Открываем Telegram для оплаты.",
                DeepLink = content?.DeepLink ?? string.Empty
            };
        }
        catch
        {
            return new PurchaseStartResult
            {
                Status = "Не удалось связаться с API для оплаты."
            };
        }
    }

    public async Task<FileUploadResultVm?> UploadAvatarAsync(Guid userId, Stream stream, string fileName, string contentType)
    {
        ApplyAuthorizationHeader();
        try
        {
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            var response = await httpClient.PostAsync(
                ApiEndpointResolver.BuildUri($"api/files/avatar/{userId}"),
                content);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<FileUploadResultVm>();
        }
        catch
        {
            return null;
        }
    }

    private void SaveAuthState(AuthResponseVm result)
    {
        appState.SetAccessToken(result.AccessToken);
        appState.SetUser(result.User.Id, result.User.PhoneNumber);
    }

    private void ApplyAuthorizationHeader()
    {
        if (string.IsNullOrWhiteSpace(appState.AccessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", appState.AccessToken);
    }

    private static DashboardVm GetFallbackCatalog() =>
        new()
        {
            MembershipPlans =
            [
                new() { Code = "gym-3m", Title = "Абонемент на 3 месяца", DurationMonths = 3, Price = 4500, Description = "Свободное посещение зала." },
                new() { Code = "gym-6m", Title = "Абонемент на 6 месяцев", DurationMonths = 6, Price = 7800, Description = "Баланс цены и срока." },
                new() { Code = "gym-12m", Title = "Абонемент на 12 месяцев", DurationMonths = 12, Price = 12900, Description = "Максимальная выгода." },
                new() { Code = "pro-1m", Title = "PowerFitness Pro", DurationMonths = 1, Price = 990, Description = "Программы Pro и расширенная статистика." }
            ],
            Trainers =
            [
                new() { FirstName = "Артем", LastName = "Соколов", Specialization = "Силовой тренинг", Bio = "Техника базовых упражнений." },
                new() { FirstName = "Елена", LastName = "Миронова", Specialization = "Функциональный тренинг", Bio = "Сушка и выносливость." }
            ],
            WorkoutPrograms =
            [
                new() { Title = "Старт в зале", Difficulty = "Новичок", DurationMinutes = 60, Description = "Вход в силовой тренинг.", TrainerName = "Артем Соколов" },
                new() { Title = "Upper/Lower Pro", Difficulty = "Продвинутый", DurationMinutes = 75, Description = "Силовой сплит с прогрессией.", TrainerName = "Артем Соколов", ProOnly = true },
                new() { Title = "Сушка 30 дней", Difficulty = "Средний", DurationMinutes = 45, Description = "Функциональный цикл.", TrainerName = "Елена Миронова", ProOnly = true }
            ]
        };

    private sealed class RegistrationTicketResponse
    {
        public Guid TicketId { get; set; }
        public string DeepLink { get; set; } = string.Empty;
    }

    private sealed class PurchaseResponse
    {
        public string DeepLink { get; set; } = string.Empty;
    }

    private sealed class ApiMessageResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
