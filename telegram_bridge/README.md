# Telegram bot for PowerFitness

Эта папка теперь содержит не только bridge-функции, но и готовый каркас Telegram-бота.

Файлы:

- `bot.py`:
  основной Telegram-бот.
- `powerfitness_bridge.py`:
  вызовы API PowerFitness.
- `requirements.txt`:
  Python-зависимости.

## Что умеет бот

- принимает deep link вида `https://t.me/<bot>?start=register_<ticketId>`;
- просит пользователя отправить контакт;
- подтверждает регистрацию через `POST /api/auth/telegram/confirm`;
- показывает каталог продуктов;
- запускает покупку абонемента или Pro;
- в демо-режиме без платёжного провайдера сразу подтверждает платёж через API;
- в боевом режиме умеет отправлять Telegram invoice.

## Переменные окружения

Можно задать вручную:

```powershell
$env:TELEGRAM_BOT_TOKEN="your-bot-token"
$env:TELEGRAM_BOT_USERNAME="your_powerfitness_bot"
$env:POWERFITNESS_API_URL="http://localhost:5004"
```

Для реальных Telegram Payments дополнительно:

```powershell
$env:TELEGRAM_PROVIDER_TOKEN="your-provider-token"
```

Если API работает на локальном HTTPS с самоподписанным сертификатом:

```powershell
$env:POWERFITNESS_ALLOW_SELF_SIGNED="1"
```

В этом проекте уже прописаны ваши текущие данные по умолчанию:

- бот: `@iktrainingbot`
- `TELEGRAM_BOT_TOKEN`
- `TELEGRAM_PROVIDER_TOKEN`

Если потом замените токены, можно просто переопределить их через переменные окружения.

## Установка

```powershell
cd "C:\Users\User\Documents\New project\telegram_bridge"
py -m pip install -r requirements.txt
```

## Запуск

Сначала подними API:

```powershell
cd "C:\Users\User\Documents\New project\PowerFitness.Api"
dotnet run --launch-profile http
```

Потом бота:

```powershell
cd "C:\Users\User\Documents\New project\telegram_bridge"
py bot.py
```

Или одной командой из корня проекта:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\User\Documents\New project\Start-PowerFitness.ps1"
```

## Как проверить сценарий

1. Открой приложение PowerFitness.
2. Введи номер телефона.
3. Нажми `Отправить в Telegram`.
4. Перейди по deep link в бота.
5. Отправь контакт.
6. Бот подтвердит регистрацию через API.
7. Выбери продукт.
8. Бот отправит настоящий Telegram invoice через `PROVIDER_TOKEN`.
