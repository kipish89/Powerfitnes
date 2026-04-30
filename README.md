# PowerFitness

PowerFitness - это клиент-серверное приложение для фитнес-зала с покупкой/продлением абонементов, управлением профилем, тренировочными программами и интеграцией с Telegram-ботом.

## Что входит в проект

- `PowerFitness.App` - клиент на .NET MAUI (Windows + Android)
- `PowerFitness.Api` - сервер на ASP.NET Core Web API
- `PowerFitness.Api.Tests` - тесты API (xUnit + Moq)
- `telegram_bridge` - Telegram-бот (Python) для подтверждения и оплаты
- `docs/ANDROID_EXPORT.md` - инструкция по Android-экспорту
- `docs/RUNNING_AND_TESTING.md` - инструкция по запуску и проверке

## Технологии

- .NET 8 (MAUI, ASP.NET Core, EF Core)
- SQLite (через Entity Framework Core)
- JWT авторизация
- Swagger/OpenAPI
- Telegram Bot API (Python)

## Архитектура и роли компонентов

### Клиент (`PowerFitness.App`)

Отвечает за UI, авторизацию, работу профиля, магазина и тренировок.

Ключевые страницы:

- `Components/Pages/Home.razor` - вход/регистрация, Telegram-подтверждение, настройка API-адреса
- `Components/Pages/Profile.razor` - профиль, редактирование, загрузка аватара
- `Components/Pages/Store.razor` - абонементы, покупка через Telegram, управление каталогом (для тренера)
- `Components/Pages/Workouts.razor` - тренировки/тренеры, CRUD программ (для тренера)

Ключевые сервисы:

- `Services/FitnessApiClient.cs` - все вызовы API
- `Services/AppState.cs` - локальное состояние (token, userId, phone, ticketId)
- `Services/SessionSyncService.cs` - восстановление сессии и синхронизация после Telegram
- `Services/ApiEndpointResolver.cs` - выбор адреса сервера для Windows/Android

### Сервер (`PowerFitness.Api`)

Отвечает за бизнес-логику, БД, авторизацию, файлы, роли и платежный workflow.

Ключевые контроллеры:

- `Controllers/AuthController.cs` - register/login/me + Telegram start/status/exchange/confirm
- `Controllers/DashboardController.cs` - данные профиля и дашборда
- `Controllers/PurchasesController.cs` - создание покупки и deep link в Telegram
- `Controllers/FilesController.cs` - upload/download аватаров
- `Controllers/UsersController.cs` - CRUD пользователей
- `Controllers/MembershipPlansController.cs` - CRUD абонементов
- `Controllers/WorkoutProgramsController.cs` - CRUD программ
- `Controllers/TrainersController.cs` - CRUD тренеров

Ключевые сервисы и данные:

- `Data/PowerFitnessDbContext.cs` - схема БД и связи
- `Services/AuthService.cs` - парольная авторизация + выдача JWT
- `Services/JwtTokenService.cs` - генерация токена
- `Services/EfFitnessRepository.cs` - сценарии Telegram, покупки, тренерские программы
- `Services/LocalFileStorageService.cs` - локальное хранение файлов
- `Services/AppDbSeeder.cs` - инициализация тестовых данных

### Telegram-бот (`telegram_bridge`)

Отвечает за подтверждение данных пользователя и запуск оплаты через Telegram.

- `bot.py` - обработка команд/контактов/платежей
- `powerfitness_bridge.py` - вызовы серверного API

## Как работает система

### Авторизация

Поддерживаются два сценария:

1. Обычная авторизация:
- регистрация (`phone + password + firstName + lastName`)
- вход (`phone + password`)
- сервер возвращает JWT
- клиент хранит токен и использует его в защищенных запросах

2. Telegram как дополнительный вход:
- клиент отправляет номер в `api/auth/start`
- сервер создает ticket и deep link
- пользователь подтверждает данные в Telegram-боте
- клиент получает статус ticket и выполняет `api/auth/telegram/exchange/{ticketId}`
- сервер выдает обычный JWT, как при парольном входе

### Покупка абонемента

1. Пользователь выбирает абонемент в магазине
2. Клиент создает intent через `api/purchases`
3. Сервер возвращает deep link в Telegram
4. Бот завершает платежный сценарий
5. Сервер фиксирует оплату и обновляет состояние пользователя/подписки

### Роли и доступ

- Обычный пользователь не может выполнять тренерский CRUD
- Тренерские действия ограничены и в UI, и на сервере (проверки роли)

## Что реализовано

### Обязательные пункты

- WebAPI с CRUD
- Entity Framework Core
- JWT авторизация (register/login/me) + хеширование паролей
- REST-подход и корректные HTTP-методы
- Swagger
- Файлы (загрузка/скачивание + валидация)
- Тесты (xUnit + Moq)
- Клиентское приложение (MAUI, Windows/Android)
- Авторизация на клиенте (хранение и использование JWT)
- Интеграция API-методов в клиенте
- Асинхронные вызовы API
- Обработка ошибок в UI (включая 401/404/сеть)
- Работа с файлами на клиенте (аватар)

### Опциональные пункты

Реализовано:

- Repository pattern
- Базовая адаптивность интерфейса

Не реализовано:

- SignalR
- Кэширование Redis/IMemoryCache
- Пагинация API/UI
- Microservices
- Identity Server

## Быстрый запуск

### 1) Запуск API + бота одной командой

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\User\Documents\powerfitness_app\Start-PowerFitness.ps1"
```

### 2) Проверка API

- `http://localhost:5004`
- `http://localhost:5004/swagger`

### 3) Запуск клиента на Windows

Открыть `PowerFitness.slnx` в Visual Studio и запустить `PowerFitness.App` на `Windows Machine`.

## Android-сборка

Используйте:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\User\Documents\powerfitness_app\Export-Android.ps1" -PackageFormat apk -Configuration Release
```

Актуальный release APK обычно формируется в:

- `PowerFitness.App\bin\Release\net10.0-android\android-arm64\`

## Тесты

Запуск тестов API:

```powershell
dotnet test "C:\Users\User\Documents\powerfitness_app\PowerFitness.Api.Tests\PowerFitness.Api.Tests.csproj"
```

## Примечания

- Для Android и внешних устройств API-адрес настраивается на главной странице приложения.
- Telegram-вход является дополнительным и не заменяет парольную авторизацию.

