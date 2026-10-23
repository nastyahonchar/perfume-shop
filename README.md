# Nota — інтернет-магазин парфумів

Навчальний проєкт з дисципліни «Технології .NET розробки Web-додатків». Проєкт розвивається в межах двох лабораторних робіт: проєктування баз даних для мікросервісної архітектури (Лаб. 01) та розробка тришарового Web API на ADO.NET і Dapper (Лаб. 02).

## Зміст

- [Опис предметної області](#опис-предметної-області)
- [Архітектура та контексти](#архітектура-та-контексти)
- [Зв'язки та ERD](#звязки-та-erd)
- [Політика дублювання даних](#політика-дублювання-даних)
- [Структура репозиторію](#структура-репозиторію)
- [Лабораторна робота № 01: бази даних](#лабораторна-робота--01-бази-даних)
- [Лабораторна робота № 02: Orders API](#лабораторна-робота--02-orders-api)
- [Запуск через .NET Aspire](#запуск-через-net-aspire)
- [Ендпоінти API](#ендпоінти-api)
- [Приклади запитів](#приклади-запитів)
- [Перевірка обробки помилок і транзакцій](#перевірка-обробки-помилок-і-транзакцій)
- [Git-процес](#git-процес)

## Опис предметної області

Nota — інтернет-магазин парфумерії. Покупці переглядають аромати, шукають парфуми за брендом, сезоном і нотами піраміди (верхні, серця та базові), оформлюють замовлення й користуються сезонними знижками. Користувачі також залишають відгуки та беруть участь в обговореннях ароматів, а адміністратор керує каталогом і акціями.

Система поділена на три незалежні контексти. Кожен контекст має власну базу даних і відповідає за свою частину предметної області. Між базами немає зовнішніх ключів: для взаємодії використовуються ідентифікатори та визначена політика дублювання даних.

## Архітектура та контексти

| № | Контекст | База даних | Технологія | Основні сутності |
|---|---|---|---|---|
| 1 | Замовлення (Orders) | `PerfumeOrdersDb` | SQL Server, ADO.NET, Dapper | `Customers`, `Orders`, `OrderDetails`, `OrderItems`, `OrderStatusHistory`, `OrderStatusTransitions`, `Promotions`, `Products` |
| 2 | Каталог (Catalog) | `PerfumeCatalogDb` | SQL Server, EF Core | `Brands`, `Perfumes`, `PerfumeDetails`, `PerfumeImages`, `Notes`, `PerfumeNotes` |
| 3 | Відгуки (Reviews) | `PerfumeReviewsDb` | MongoDB | `reviews`, `discussions`, `discussionMessages`, `ratingSummaries` |

У межах Лабораторної роботи № 02 реалізується контекст **Orders** як окрема вертикаль — від доступу до бази даних до HTTP API.

### Тришарова архітектура Orders

```text
HTTP-клієнт / Swagger
        |
        v
Orders.Api       HTTP-контракт, маршрутизація, Swagger, обробка помилок
        |
        v
Orders.Bll       бізнес-правила, сервіси, DTO, мапінг і валідація
        |
        v
Orders.Dal       репозиторії, SQL-запити, ADO.NET, Dapper, Unit of Work
        |
        v
SQL Server       PerfumeOrdersDb
```

`Orders.Domain` містить спільні доменні моделі, константи та доменні винятки, які використовують DAL і BLL.

Залежності спрямовані в один бік: `Api → Bll → Dal`. API звертається до бізнес-логіки через сервіси, а не викликає репозиторії безпосередньо. SQL-запити зберігаються в DAL; контролери не повинні містити SQL або бізнес-правила.

## Зв'язки та ERD

У SQL-базах зв'язки реалізовані в межах відповідної бази даних.

| Тип зв'язку | БД замовлень | БД каталогу |
|---|---|---|
| 1:1 | `Orders` ↔ `OrderDetails` | `Perfumes` ↔ `PerfumeDetails` |
| 1:N | `Customers` → `Orders` | `Brands` → `Perfumes` |
| M:N | `Orders` ↔ `Products` через `OrderItems` | `Perfumes` ↔ `Notes` через `PerfumeNotes` |

`PerfumeNotes` містить власні поля, зокрема `Layer` і `AddedAt`. Зовнішні ключі використовуються для зв'язків усередині кожної SQL-бази. Між різними базами даних `FOREIGN KEY` не створюються.

### ERD

- База замовлень: див. [опис контекстів і матеріали ERD](docs/domain-and-contexts.md) та вихідний файл [orders.dbml](docs/orders.dbml).
- База каталогу: див. [catalog.dbml](docs/catalog.dbml).

БД Замовлень:

![ERD БД Замовлень](docs/erd-orders.png)

БД Каталогу:

![ERD БД Каталогу](docs/erd-catalog.png)


Таблиця `Promotions` не має зовнішніх ключів на каталог: сезонна знижка зіставляється з товаром за значенням `Season`. `OrderStatusTransitions` містить дозволені переходи статусів, які використовує процедура `usp_ChangeOrderStatus`.

## Політика дублювання даних

Дублювання окремих полів потрібне для автономності сервісів і зменшення кількості синхронних звернень між ними. Для кожного поля визначено власника даних і спосіб узгодження.

| Поле | Де зберігається | Джерело (власник даних) | Політика узгодженості |
|---|---|---|---|
| `OrderItems.ProductName`, `UnitPrice` | БД замовлень | `Perfumes` у БД каталогу | Знімок назви та ціни на момент оформлення; надалі не оновлюється |
| `OrderItems.Discount` | БД замовлень | `Promotions` у БД замовлень | Знімок знижки на момент додавання позиції |
| `Products.Name`, `BrandName`, `Price`, `Season`, `IsActive` | Локальна копія довідника в БД замовлень | `Perfumes` і `Brands` у БД каталогу | Оновлюється подіями з каталогу |
| `Products.Stock` | БД замовлень | Контекст Orders | Власне поле Orders; залишок змінює сервіс замовлень |
| `reviews.productName`, `brandName` | MongoDB, база відгуків | `Perfumes` і `Brands` у БД каталогу | Оновлюється подіями з каталогу |
| `discussions.productName` | MongoDB, база відгуків | `Perfumes` у БД каталогу | Оновлюється подіями з каталогу |
| `ratingSummaries.productName`, `brandName` | MongoDB, база відгуків | `Perfumes` і `Brands` у БД каталогу | Оновлюється подіями з каталогу |
| `reviews.author.displayName` | MongoDB, база відгуків | Дані клієнта з контексту Orders | Знімок імені на момент написання відгуку |

Докладні пояснення наведено в [docs/duplication-policy.md](docs/duplication-policy.md).

## Структура репозиторію

```text
perfume-shop/
├── README.md
├── docs/
│   ├── domain-and-contexts.md
│   ├── duplication-policy.md
│   ├── orders.dbml
│   ├── catalog.dbml
│   ├── erd-orders.png
│   ├── erd-catalog.png
│   └── mongo-examples/
├── db/
│   ├── p1/
│   │   ├── schema.sql
│   │   ├── procedures.sql
│   │   ├── seed.sql
│   │   └── checks.sql
│   ├── p2/
│   │   ├── schema.sql
│   │   └── seed.sql
│   └── p3/
│       ├── collections.js
│       ├── seed.js
│       └── queries.js
├── Platform.slnx
├── Platform.AppHost/
└── Services/
    └── Orders/
        ├── Orders.Api/
        ├── Orders.Bll/
        ├── Orders.Dal/
        └── Orders.Domain/
```

`Platform.slnx`, `Platform.AppHost` і `Services/Orders` — рішення Лабораторної роботи № 02.

## Лабораторна робота № 01: бази даних

### Вимоги до середовища

- SQL Server 2017 або новіший.
- SQL Server Management Studio (SSMS).
- MongoDB 6+ і `mongosh` для бази відгуків.

### Розгортання SQL Server

Відкрийте кожен SQL-файл у SSMS та виконайте його командою **Execute (F5)**. Скрипти потрібно виконувати в такому порядку:

1. `db/p1/schema.sql` — створення `PerfumeOrdersDb`, таблиць, обмежень та індексів.
2. `db/p1/procedures.sql` — створення збережуваних процедур.
3. `db/p1/seed.sql` — додавання тестових даних.
4. `db/p1/checks.sql` — контрольні запити (необов'язково).
5. `db/p2/schema.sql` — створення `PerfumeCatalogDb`, таблиць, індексів і представлень.
6. `db/p2/seed.sql` — початкові довідники та тестові дані.

Seed-скрипти розраховані на повторний запуск без створення дублікатів.

### Розгортання MongoDB

Із кореня репозиторію виконайте:

```powershell
mongosh "mongodb://localhost:27017/PerfumeReviewsDb" --file db/p3/collections.js
mongosh "mongodb://localhost:27017/PerfumeReviewsDb" --file db/p3/seed.js
mongosh "mongodb://localhost:27017/PerfumeReviewsDb" --file db/p3/queries.js
```

`collections.js` створює колекції, правила валідації та індекси; `seed.js` додає тестові документи; `queries.js` містить приклади запитів та агрегацій. Приклади документів у форматі JSON розміщено в `docs/mongo-examples/`.

### Збережувані процедури БД замовлень

| Процедура | Призначення |
|---|---|
| `usp_CreateOrder` | Створення замовлення, деталей і запису в історії статусів у межах однієї транзакції |
| `usp_AddOrderItem` | Додавання позиції, збереження знімка назви й ціни та застосування сезонної знижки |
| `usp_ConfirmOrder` | Перевірка та списання залишку, перехід у `StockConfirmed` |
| `usp_ChangeOrderStatus` | Зміна статусу лише за дозволеним переходом |
| `usp_GetOrderById` | Отримання замовлення, позицій та історії статусів |
| `usp_GetCustomerOrders` | Історія замовлень клієнта з фільтром за статусом і пагінацією |

Основний життєвий цикл замовлення: `Submitted → AwaitingValidation → StockConfirmed → Paid → Shipped`. Скасування (`Cancelled`) дозволене лише до оплати. Недопустимий перехід має завершуватися контрольованою помилкою.

| Код SQL | Значення |
|---|---|
| `50001` | Замовлення не знайдено |
| `50002` | Недопустимий перехід статусу |
| `50003` | Недостатньо товару на складі |
| `50004` | Товар не знайдено або він неактивний |
| `50005` | Позиції можна змінювати лише у статусі `Submitted` |
| `50006` | Клієнта не знайдено |
| `50007` | Статус `StockConfirmed` можна встановити лише через `usp_ConfirmOrder` |
| `50008` | Кількість має бути додатною |
| `50009` | Замовлення не містить позицій |

## Лабораторна робота № 02: Orders API

### Призначення

Orders API надає HTTP-інтерфейс для роботи з клієнтами, товарами та замовленнями. Назва, ціна й сезон товару є копією з контексту «Каталог», тому через API змінюється лише залишок (`PUT /api/products/{id}/stock`). Бізнес-операції реалізовано в BLL, SQL-доступ винесено в DAL (чистий ADO.NET і Dapper).

### Основні компоненти

| Компонент | Відповідальність |
|---|---|
| `Orders.Api` | Контролери, маршрутизація, Swagger, конфігурація, централізована обробка помилок |
| `Orders.Bll` | Сервіси, бізнес-правила, DTO, AutoMapper і бізнес-валідація |
| `Orders.Dal` | Репозиторії, параметризовані SQL-запити, ADO.NET, Dapper і Unit of Work |
| `Orders.Domain` | Доменні моделі, константи й доменні винятки |
| `Platform.AppHost` | Запуск API та підключення до бази через .NET Aspire |

### Доступ до даних

- `IGenericRepository<T>` визначає спільні CRUD-операції для відповідних репозиторіїв.
- `CustomerRepository` демонструє чистий ADO.NET: параметризовані команди та ручне читання результатів.
- `ProductRepository` і `OrderRepository` використовують Dapper для типових операцій та спеціалізованих запитів.
- `UnitOfWork` координує репозиторії через спільне з'єднання й транзакцію.
- SQL-запити виконуються в DAL; значення передаються як параметри, а не конкатенуються з текстом SQL.
- Операції доступу до бази виконуються асинхронно.

### Технології

| Компонент | Версія |
|---|---|
| .NET | 9 |
| .NET Aspire (AppHost) | 9.5.2 |
| Dapper | 2.1.89 |
| Microsoft.Data.SqlClient | 7.1.1 |
| AutoMapper | 14.0.0 (ліцензійний ключ не потрібен) |
| Serilog.AspNetCore | 10.0.0 |
| Swashbuckle.AspNetCore | 9.0.6 |

### Транзакції та конкурентність

Транзакційна операція виконується так: Unit of Work відкриває транзакцію, репозиторії виконують зміни, після успіху викликається `Commit`, у разі помилки — `Rollback`. Транзакційні сценарії: створення замовлення (`POST /api/orders`: перевірка клієнта, перевірка товарів і додавання позицій у трьох репозиторіях), підтвердження замовлення і зміна статусу.

Рівень ізоляції — `ReadCommitted`: операція читає лише зафіксовані дані, а блокування короткі. Вищі рівні (`RepeatableRead`, `Serializable`) дають менше аномалій читання, але більше блокувань і ризик взаємних блокувань (deadlock). Таймаут команд — 30 с (значення `SqlCommand` за замовчуванням).

Підхід до конкурентності — песимістичний: збережувані процедури блокують рядок замовлення через `UPDLOCK`, тому два одночасні підтвердження одного замовлення виконуються по черзі. Колонка `RowVersion` з Лаб. 01 у цьому сервісі не перевіряється.

Процедури мають `SET XACT_ABORT ON`, тому при помилці (наприклад, нестача залишку) сервер сам відкочує всю транзакцію. `UnitOfWork.RollbackAsync` це враховує і не намагається відкотити транзакцію вдруге.

### Валідація та обробка помилок

Перший рівень валідації — DataAnnotations на DTO (обов'язкові поля, діапазони, формат email): некоректний запит відхиляється з `400` ще до виклику сервісу. Другий рівень — BLL: правила, яким потрібен стан бази (клієнт існує, товар існує й активний, статус дозволений, email не дублюється).

Помилки збережуваних процедур перетворюються на доменні винятки в DAL (`SqlErrorTranslator`), а глобальний обробник `DomainExceptionHandler` повертає їх у форматі `ProblemDetails`:

| Виняток | HTTP-статус | Приклад |
|---|---|---|
| `ValidationException` та помилки DataAnnotations | `400 Bad Request` | порожній email, невідомий статус |
| `NotFoundException` | `404 Not Found` | замовлення чи клієнта з таким Id немає |
| `BusinessConflictException` | `409 Conflict` | недопустимий перехід статусу, нестача залишку, дубль email |
| інші винятки | `500 Internal Server Error` | клієнту повертається загальний опис, деталі пишуться в лог |

## Запуск через .NET Aspire

### Передумови

- .NET 9 SDK (перевірка: `dotnet --list-sdks`).
- Visual Studio 2022 (17.14 або новіша) із навантаженням **ASP.NET and web development** або .NET CLI.
- SQL Server LocalDB або інший локальний SQL Server.
- Docker Desktop — лише для варіанта з SQL Server у контейнері.

### Два режими бази даних

Режим вибирає параметр `UseSqlContainer` у `Platform.AppHost/appsettings.json`.

| Режим | `UseSqlContainer` | База |
|---|---|---|
| Існуюча база (за замовчуванням) | `false` | Підключення до `PerfumeOrdersDb` за рядком `ConnectionStrings:OrdersDb` із `Platform.AppHost/appsettings.json` (за замовчуванням LocalDB) |
| SQL Server у контейнері | `true` | Aspire створює контейнер із томом даних і порожню базу `PerfumeOrdersDb` |

**Підготовка бази.** В обох режимах до бази потрібно застосувати `db/p1/schema.sql`, `db/p1/procedures.sql` і `db/p1/seed.sql` (у SSMS або через `sqlcmd`). У режимі контейнера адресу й порт SQL Server видно в Aspire Dashboard, порожній контейнер таблиць не містить. У режимі існуючої бази достатньо один раз виконати ці скрипти на своєму SQL Server.

Увімкнути контейнер без редагування файлу можна змінною оточення:

```powershell
$env:UseSqlContainer = "true"
```

### Запуск усієї системи

З кореня репозиторію:

```powershell
dotnet run --project Platform.AppHost
```

Aspire Dashboard відкривається за адресою з консолі (за замовчуванням `https://localhost:17062`). У ньому API `orders-api` і ресурс бази мають статус `Running`. Адресу API беріть з колонки **Endpoints**, Swagger доступний за шляхом `/swagger`.

### Рядок підключення

Рядок підключення зберігається в `appsettings.json` і перевизначається змінною оточення (подвійне підкреслення замість двокрапки):

```powershell
$env:ConnectionStrings__OrdersDb = "Server=localhost;Database=PerfumeOrdersDb;Trusted_Connection=True;TrustServerCertificate=True"
```

Паролі й інші секрети в репозиторій не додаються.

### Запуск API без Aspire

```powershell
dotnet run --project Services/Orders/Orders.Api --launch-profile https
```

API стартує на `https://localhost:7130`, Swagger — `https://localhost:7130/swagger`. Рядок підключення береться з `Services/Orders/Orders.Api/appsettings.json` (за замовчуванням LocalDB) або зі змінної оточення `ConnectionStrings__OrdersDb`.

## Ендпоінти API


| Метод | Шлях | Призначення | Типові відповіді |
|---|---|---|---|
| `GET` | `/api/customers` | Отримати список клієнтів | `200` |
| `GET` | `/api/customers/{id}` | Отримати клієнта за ID | `200`, `404` |
| `POST` | `/api/customers` | Створити клієнта | `201`, `400`, `409` |
| `PUT` | `/api/customers/{id}` | Оновити клієнта | `200`, `400`, `404`, `409` |
| `DELETE` | `/api/customers/{id}` | Видалити клієнта (м'яке видалення) | `204`, `404` |
| `GET` | `/api/products` | Отримати товари; можливий фільтр `season` | `200`, `400` |
| `GET` | `/api/products/{id}` | Отримати товар за ID | `200`, `404` |
| `PUT` | `/api/products/{id}/stock` | Змінити залишок | `200`, `400`, `404` |
| `GET` | `/api/orders/{id}` | Отримати замовлення з позиціями | `200`, `404` |
| `GET` | `/api/customers/{customerId}/orders` | Отримати замовлення клієнта; можливі `status`, `page`, `pageSize` | `200`, `400`, `404` |
| `POST` | `/api/orders` | Створити замовлення | `201`, `400`, `404`, `409` |
| `PUT` | `/api/orders/{id}` | Оновити адресу та коментар замовлення | `200`, `400`, `404`, `409` |
| `POST` | `/api/orders/{id}/confirm` | Підтвердити наявність і виконати списання залишку | `204`, `404`, `409` |
| `PUT` | `/api/orders/{id}/status` | Змінити статус із перевіркою переходу | `204`, `400`, `404`, `409` |
| `DELETE` | `/api/orders/{id}` | Видалити замовлення за дозволених статусів | `204`, `404`, `409` |

Створення ресурсу повертає `201 Created` із заголовком `Location`. Операції без тіла відповіді (`confirm`, зміна статусу, видалення) повертають `204 No Content`. Помилки валідації тіла запиту (`POST` і `PUT`) повертають `400`.

## Приклади запитів

`BASE` — адреса API: з Aspire Dashboard (колонка **Endpoints**) або `https://localhost:7130` при запуску API напряму.

```powershell
$BASE = "https://localhost:7130"

# список клієнтів
Invoke-RestMethod -Method Get -Uri "$BASE/api/customers"

# створити клієнта
$body = @{
    email    = "test@example.com"
    fullName = "Test User"
    phone    = "+380501112233"
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "$BASE/api/customers" -ContentType "application/json" -Body $body

# товари зимового сезону
Invoke-RestMethod -Method Get -Uri "$BASE/api/products?season=Winter"

# замовлення клієнта 1 зі статусом Paid, перша сторінка
Invoke-RestMethod -Method Get -Uri "$BASE/api/customers/1/orders?status=Paid&page=1&pageSize=10"

# одне замовлення з позиціями
Invoke-RestMethod -Method Get -Uri "$BASE/api/orders/1"
```

Створення замовлення (`POST /api/orders`). Позиція фіксує назву, ціну й сезонну знижку на момент замовлення:

```json
{
  "customerId": 1,
  "shippingAddress": "Kyiv, Khreshchatyk 1",
  "comment": "Подарункова упаковка",
  "items": [
    { "productId": 1, "units": 2 },
    { "productId": 2, "units": 1 }
  ]
}
```

Підтвердження замовлення і зміна статусу:

```powershell
Invoke-RestMethod -Method Post -Uri "$BASE/api/orders/3/confirm"

$status = @{ status = "Paid" } | ConvertTo-Json
Invoke-RestMethod -Method Put -Uri "$BASE/api/orders/3/status" -ContentType "application/json" -Body $status
```

Приклад відповіді `409 Conflict` на повторне підтвердження замовлення (`POST /api/orders/2/confirm`):

```json
{
  "status": 409,
  "title": "Business rule conflict",
  "detail": "Stock can be confirmed only for a new order",
  "instance": "/api/orders/2/confirm",
  "traceId": "0HN..."
}
```

## Перевірка обробки помилок і транзакцій

Сценарії зручно виконувати у Swagger (`/swagger`), бо Swagger показує код і тіло відповіді. Для свіжої бази після `seed.sql`: замовлення 1 має статус `Paid`, 2 — `StockConfirmed`, 3 — `Submitted`.

| Сценарій | Запит | Очікуваний результат |
|---|---|---|
| Спецсимволи в параметрі | `POST /api/customers` з `fullName` = `Conor O'Brien` | `201`, помилки SQL немає: значення передається параметром |
| Валідація DTO | `POST /api/customers` з `email` = `not-an-email` | `400` у форматі ProblemDetails з переліком полів |
| Неіснуючий Id | `GET /api/orders/9999` | `404`, а не `500` |
| Повторне підтвердження | `POST /api/orders/2/confirm` | `409`, ProblemDetails |
| Недопустимий перехід | `PUT /api/orders/3/status` зі статусом `Shipped` | `409`: неоплачене замовлення відвантажити не можна |
| Відкат при створенні замовлення | `POST /api/orders` для клієнта 1 з двома позиціями: `productId` 1 і `productId` 99999 | `404` (другого товару немає); у `GET /api/customers/1/orders` кількість замовлень не змінилась, бо створене замовлення і перша позиція відкотились |
| Відкат при нестачі залишку | `PUT /api/products/7/stock` зі `stock` = 1, потім `POST /api/orders/3/confirm` | `409`; у `GET /api/products/3` залишок не змінився; після перевірки поверніть залишок товару 7 до 14 |

## Git-процес

Для розробки використовується така модель гілок:

- `main` — стабільна версія проєкту;
- `develop` — інтеграційна гілка;
- `feature/lab-02-orders-service` — зміни для Лабораторної роботи № 02.
