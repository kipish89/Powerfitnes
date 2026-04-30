# Запуск и проверка PowerFitness

## Проверка на компьютере

Самый удобный способ сначала проверить проект на Windows, а уже потом переходить к Android.

### 1. Запустить API

Откройте терминал в папке:

`C:\Users\User\Documents\New project\PowerFitness.Api`

Команда:

```powershell
dotnet run --launch-profile http
```

После запуска API будет доступен по адресу:

`http://localhost:5004`

### 1a. Либо запустить API и Telegram-бота вместе

Из корня проекта:

```powershell
powershell -ExecutionPolicy Bypass -File "C:\Users\User\Documents\New project\Start-PowerFitness.ps1"
```

Скрипт:

- запускает `PowerFitness.Api`;
- запускает Telegram-бота `@iktrainingbot`;
- подставляет токен бота и `PROVIDER_TOKEN`;
- связывает бота с локальным API `http://localhost:5004`.

Проверка в браузере:

[http://localhost:5004](http://localhost:5004)

Если всё хорошо, вы увидите JSON со статусом `PowerFitness API`.

### 2. Запустить приложение на компьютере

Лучше всего через Visual Studio:

1. Откройте [PowerFitness.slnx](C:\Users\User\Documents\New project\PowerFitness.slnx)
2. Выберите проект `PowerFitness.App`
3. Вверху выберите цель `Windows Machine`
4. Нажмите `Start`

### 3. Что проверить в приложении

- открывается главная страница;
- ввод номера телефона работает;
- кнопка `Отправить в Telegram` показывает статус;
- страницы `Профиль`, `Магазин`, `Тренировки` открываются через нижнее меню;
- на странице магазина кнопка покупки создаёт демо-запрос на оплату;
- данные на страницах подгружаются из API или из демо-режима.

## Проверка на Android

### 1. Что уже подготовлено

Android запуск уже поддерживается проектом:

- в [PowerFitness.App.csproj](C:\Users\User\Documents\New project\PowerFitness.App\PowerFitness.App.csproj) есть target `net10.0-android`;
- приложение теперь использует адрес `http://10.0.2.2:5004/` на Android-эмуляторе;
- в Android manifest включён `usesCleartextTraffic="true"`.

### 2. Как запустить на Android-эмуляторе

1. Сначала запустите API командой:

```powershell
dotnet run --launch-profile http
```

2. Откройте решение в Visual Studio
3. Выберите проект `PowerFitness.App`
4. Выберите Android Emulator, например `Pixel`
5. Нажмите `Start`

`10.0.2.2` внутри эмулятора указывает на ваш компьютер, поэтому приложение сможет достучаться до локального API.

## Важное ограничение

Для реального Android-телефона `10.0.2.2` не подойдёт.

Для настоящего телефона нужно:

- либо запускать API на IP вашего компьютера в локальной сети, например `http://192.168.0.15:5004`;
- либо публиковать API на сервере/VPS;
- либо использовать туннель типа `ngrok` для тестов.

Если захочешь, следующим сообщением я могу сразу подготовить проект и для запуска с реального Android-телефона, а не только с эмулятора. 
