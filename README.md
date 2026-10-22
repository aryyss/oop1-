# Електронні таблиці (ЛР №1*, варіант 5)

## Що в архіві
- `SpreadsheetApp.Core` — уся логіка (розбір і обчислення виразів, таблиця, залежності, збереження JSON, ViewModel). Без залежності від MAUI.
- `SpreadsheetApp.Tests` — тести (xunit).
- `maui-files` — файли для проєкту .NET MAUI (екрани, діалоги, Google Drive).

## Крок 1. Перевірка ядра
1. Відкрийте `SpreadsheetApp.sln` у Visual Studio.
2. Якщо встановлений тільки .NET 10, у `SpreadsheetApp.Core.csproj` та `SpreadsheetApp.Tests.csproj` замініть `net8.0` на `net10.0`.
3. Test -> Run All Tests. Усі тести мають пройти.

## Крок 2. Проєкт MAUI
1. ПКМ по рішенню -> Add -> New Project -> ".NET MAUI App".
2. Назва строго `SpreadsheetApp`, розташування — папка, де лежить `SpreadsheetApp.sln`.
3. Додайте посилання: ПКМ по проєкту SpreadsheetApp -> Add -> Project Reference -> SpreadsheetApp.Core.
4. NuGet: встановіть пакет `Google.Apis.Drive.v3` у проєкт SpreadsheetApp.
5. З папки проєкту видаліть шаблонні `MainPage.xaml` і `MainPage.xaml.cs`.
6. Скопіюйте з `maui-files` у папку проєкту SpreadsheetApp (із заміною):
   `MauiProgram.cs`, `AppShell.xaml`, `AppShell.xaml.cs`, папки `Views` і `Services`.
   `App.xaml` та `App.xaml.cs` залишайте шаблонними.
7. Увімкніть режим розробника Windows (Параметри -> Для розробників).
8. Виберіть "Windows Machine" і запустіть (F5).

Попередження про застарілі `DisplayAlert`/`DisplayActionSheet` у .NET 10 можна ігнорувати.

## Крок 3. Google Drive
1. https://console.cloud.google.com -> створіть проєкт.
2. APIs & Services -> Library -> Google Drive API -> Enable.
3. OAuth consent screen: тип External, додайте свій Gmail у Test users.
4. Credentials -> Create credentials -> OAuth client ID -> Application type: Desktop app.
5. Завантажте JSON, перейменуйте на `credentials.json` і покладіть у `Документи\SpreadsheetApp\`.
6. Під час першого збереження на Google Drive відкриється браузер. Якщо Google пише, що застосунок не перевірений, натисніть Advanced -> Continue.

Файли JSON на комп'ютері зберігаються в `Документи\SpreadsheetApp\`.
