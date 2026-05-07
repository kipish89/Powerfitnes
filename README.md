# PowerFitness

PowerFitness - клиент-серверное приложение для фитнес-зала с покупкой и продлением абонементов, управлением профилем, тренировочными программами и интеграцией с Telegram-ботом.

## Состав проекта

- `PowerFitness.App` - клиент на .NET MAUI (Windows + Android)
- `PowerFitness.Api` - сервер на ASP.NET Core Web API
- `PowerFitness.Api.Tests` - тесты API (xUnit + Moq)
- `telegram_bridge` - Telegram-бот (Python) для подтверждения данных и оплаты
- `docs/ANDROID_EXPORT.md` - заметки по Android-экспорту
- `docs/RUNNING_AND_TESTING.md` - заметки по локальной проверке

## Технологический стек

- .NET 10 (MAUI, ASP.NET Core, EF Core)
- SQLite (через Entity Framework Core)
- JWT авторизация
- Swagger/OpenAPI
- Telegram Bot API (Python)

## Архитектура

### Клиент (`PowerFitness.App`)

Клиент отвечает за интерфейс, авторизацию, профиль, магазин и тренировки.

Ключевые страницы:

- `Components/Pages/Home.razor` - вход/регистрация, Telegram-подтверждение, настройка адреса API
- `Components/Pages/Profile.razor` - профиль, редактирование, загрузка аватара
- `Components/Pages/Store.razor` - каталог абонементов, покупка через Telegram, управление каталогом (для тренера)
- `Components/Pages/Workouts.razor` - программы тренировок и тренеры, CRUD программ (для тренера)

Ключевые сервисы:

- `Services/FitnessApiClient.cs` - клиентские вызовы API
- `Services/AppState.cs` - локальное состояние (token, userId, phone, ticketId)
- `Services/SessionSyncService.cs` - восстановление сессии и синхронизация после Telegram
- `Services/ApiEndpointResolver.cs` - выбор адреса API для разных платформ

### Сервер (`PowerFitness.Api`)

Сервер отвечает за бизнес-логику, базу данных, авторизацию, файлы и роли.

Ключевые контроллеры:

- `Controllers/AuthController.cs` - register/login/me + Telegram start/status/exchange/confirm
- `Controllers/DashboardController.cs` - данные профиля и дашборда
- `Controllers/PurchasesController.cs` - создание покупки и deep link в Telegram
- `Controllers/FilesController.cs` - загрузка/скачивание аватаров
- `Controllers/UsersController.cs` - CRUD пользователей
- `Controllers/MembershipPlansController.cs` - CRUD абонементов
- `Controllers/WorkoutProgramsController.cs` - CRUD тренировочных программ
- `Controllers/TrainersController.cs` - CRUD тренеров

Ключевые сервисы:

- `Data/PowerFitnessDbContext.cs` - схема БД и связи
- `Services/AuthService.cs` - парольная авторизация и выдача JWT
- `Services/JwtTokenService.cs` - генерация токенов
- `Services/EfFitnessRepository.cs` - логика Telegram, платежей и программ
- `Services/LocalFileStorageService.cs` - локальное хранилище файлов
- `Services/AppDbSeeder.cs` - начальное заполнение тестовыми данными

### Telegram-бот (`telegram_bridge`)

- `bot.py` - обработка команд, контактов, платежей
- `powerfitness_bridge.py` - вызовы серверного API

## Как работает система

### Авторизация

Поддерживаются два сценария:

1. Парольный вход:
- регистрация (`phone + password + firstName + lastName`)
- вход (`phone + password`)
- сервер выдает JWT
- клиент хранит token и использует его в защищенных запросах

2. Telegram как дополнительный вход:
- клиент отправляет номер в `api/auth/start`
- сервер создает ticket и deep link
- пользователь подтверждает данные в Telegram
- клиент обменивает ticket через `api/auth/telegram/exchange/{ticketId}`
- сервер выдает обычный JWT

### Покупка абонемента

1. Пользователь выбирает абонемент в магазине
2. Клиент создает intent через `api/purchases`
3. Сервер возвращает deep link в Telegram
4. Бот завершает платежный сценарий
5. Сервер фиксирует оплату и обновляет данные пользователя

### Доступ и роли

- обычный пользователь не имеет доступа к тренерскому CRUD
- тренерские действия ограничены и в UI, и на сервере

## Что реализовано

### Обязательные пункты

- WebAPI с CRUD
- Entity Framework Core
- JWT авторизация (register/login/me) + хеширование паролей
- REST-подход и корректные HTTP-методы
- Swagger
- Работа с файлами (загрузка/скачивание + валидация)
- Тесты (xUnit + Moq)
- Клиентское приложение (MAUI)
- Авторизация на клиенте (хранение и использование JWT)
- Интеграция API-методов в клиенте
- Асинхронные вызовы API
- Обработка ошибок в UI (401/404/сетевые ошибки)
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

## Использование языков в проекте

- C# - основная логика клиента, сервера, авторизации, БД и тестов
- HTML (Razor) - разметка UI страниц
- Python - Telegram-бот и bridge к API
- CSS - стили и адаптивность интерфейса
- PowerShell - автоматизация экспорта/локальных операций
- AIDL - артефакты Android toolchain
- Roff - служебные текстовые артефакты зависимостей/инструментов

## Примечания

- Telegram-вход является дополнительным и не заменяет парольный вход.
- Для production рекомендуется хранить токены и секреты во внешнем секрет-хранилище или переменных окружения.
