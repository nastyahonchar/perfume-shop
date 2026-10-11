# Nota: інтернет-магазин парфумів

Лабораторна робота 01: проєктування баз даних для мікросервісної архітектури.

## Предметна область
Nota це інтернет-магазин парфумерії. Покупці обирають аромат за брендом, сезоном і нотами піраміди (верхні, серце, база), оформлюють замовлення і користуються сезонними знижками, наприклад на осінні «гарбузові» аромати. Покупці також залишають відгуки й обговорюють аромати. Адміністратор керує каталогом і акціями.

## Три контексти (три незалежні бази)
| № | Контекст | База | Технологія | Сутності |
|---|----------|------|------------|----------|
| 1 | Замовлення | `PerfumeOrdersDb` (`db/p1`) | SQL Server | Customers, Orders, OrderDetails, OrderItems, OrderStatusHistory, OrderStatusTransitions, Promotions, Products (копія довідника та власний залишок Stock) |
| 2 | Каталог | `PerfumeCatalogDb` (`db/p2`) | SQL Server | Brands, Perfumes, PerfumeDetails, PerfumeImages, Notes, PerfumeNotes |
| 3 | Відгуки | `PerfumeReviewsDb` (`db/p3`) | MongoDB | reviews, discussions, discussionMessages, ratingSummaries |

Між базами немає жодного FOREIGN KEY. Вони посилаються одна на одну лише за ідентифікатором товару (`ProductId` / `productId`). Перевірка: запит `SELECT * FROM sys.foreign_keys` у кожній SQL-базі показує лише зв'язки всередині цієї бази.

Детальніше про межі контекстів, агрегати та проєктні рішення: [docs/domain-and-contexts.md](docs/domain-and-contexts.md).

## Зв'язки
| Тип | База 1 (Замовлення) | База 2 (Каталог) |
|-----|---------------------|------------------|
| 1:1 | Orders ↔ OrderDetails | Perfumes ↔ PerfumeDetails |
| 1:N | Customers → Orders | Brands → Perfumes |
| M:N | Orders ↔ Products через OrderItems | Perfumes ↔ Notes через PerfumeNotes (власні поля Layer, AddedAt) |

### ERD

БД Замовлень:

![ERD БД Замовлень](docs/erd-orders.png)

БД Каталогу:

![ERD БД Каталогу](docs/erd-catalog.png)

Таблиця `Promotions` не має зовнішніх ключів: акція діє на сезон, і процедура `usp_AddOrderItem` зіставляє її з товаром за значенням `Season`. Таблиця `OrderStatusTransitions` є довідником дозволених переходів статусів, який читає `usp_ChangeOrderStatus`.

Джерела для dbdiagram.io: [orders.dbml](docs/orders.dbml), [catalog.dbml](docs/catalog.dbml).

## Таблиця політики дублювання даних
| Поле | Де зберігається | Джерело (власник даних) | Політика узгодженості |
|------|-----------------|-------------------------|-----------------------|
| `OrderItems.ProductName`, `UnitPrice` | БД Замовлень | Perfumes (БД Каталогу) | Знімок на момент оформлення, не оновлюється |
| `OrderItems.Discount` | БД Замовлень | Promotions (БД Замовлень) | Знімок знижки на момент додавання позиції |
| `Products.Name`, `BrandName`, `Price`, `Season`, `IsActive` | БД Замовлень | Perfumes і Brands (БД Каталогу) | Копія довідника, оновлюється подіями з Каталогу |
| `Products.Stock` | БД Замовлень | БД Замовлень | Власне поле, не дублюється |
| `reviews.productName`, `brandName` | БД Відгуків | Perfumes і Brands (БД Каталогу) | Оновлюється подіями з Каталогу |
| `discussions.productName` | БД Відгуків | Perfumes (БД Каталогу) | Оновлюється подіями з Каталогу |
| `ratingSummaries.productName`, `brandName` | БД Відгуків | Perfumes і Brands (БД Каталогу) | Оновлюється подіями з Каталогу |
| `reviews.author.displayName` | БД Відгуків | Customers (БД Замовлень) | Знімок на момент написання |

Повна версія з поясненнями: [docs/duplication-policy.md](docs/duplication-policy.md).

## Розгортання з нуля
Потрібно: SQL Server 2017 або новіший, SQL Server Management Studio (SSMS), MongoDB 6+ і `mongosh`.

### SQL Server (SSMS)
Для кожного файлу: відкрити **New Query**, вставити вміст файлу, натиснути **Execute** (F5). Порядок такий:

1. `db/p1/schema.sql`: база `PerfumeOrdersDb`, таблиці, обмеження, індекси
2. `db/p1/procedures.sql`: 6 збережуваних процедур
3. `db/p1/seed.sql`: тестові дані
4. `db/p1/checks.sql`: перевірки (за бажанням)
5. `db/p2/schema.sql`: база `PerfumeCatalogDb`, таблиці, індекси, представлення
6. `db/p2/seed.sql`: довідники та тестові дані

Повторний запуск будь-якого скрипта безпечний: seed-скрипти не створюють дублікатів.

### MongoDB
У PowerShell із кореня репозиторію:

```powershell
mongosh "mongodb://localhost:27017/PerfumeReviewsDb" --file db/p3/collections.js
mongosh "mongodb://localhost:27017/PerfumeReviewsDb" --file db/p3/seed.js
mongosh "mongodb://localhost:27017/PerfumeReviewsDb" --file db/p3/queries.js
```
Перший скрипт створює колекції з валідацією та індексами, другий додає тестові документи, третій містить приклади запитів і агрегацій. Приклади документів у форматі JSON лежать у [docs/mongo-examples](docs/mongo-examples).

## Збережувані процедури (БД Замовлень)
| Процедура | Призначення |
|-----------|-------------|
| `usp_CreateOrder` | Створює замовлення, його деталі та запис в історії статусів (одна транзакція) |
| `usp_AddOrderItem` | Додає позицію, фіксує назву й ціну (знімок) і застосовує сезонну знижку |
| `usp_ConfirmOrder` | Перевіряє та списує залишок, ставить статус StockConfirmed |
| `usp_ChangeOrderStatus` | Змінює статус у явній транзакції лише за допустимим переходом |
| `usp_GetOrderById` | Повертає замовлення, позиції та історію статусів |
| `usp_GetCustomerOrders` | Повертає історію замовлень клієнта з фільтром за статусом і пагінацією |

Статуси: `Submitted → AwaitingValidation → StockConfirmed → Paid → Shipped`, скасування (`Cancelled`) можливе лише до оплати. Допустимі переходи зберігаються в таблиці `OrderStatusTransitions`. Недопустимий перехід (наприклад, відвантажити неоплачене замовлення) завершується керованою помилкою.

| Код | Значення |
|-----|----------|
| 50001 | Замовлення не знайдено |
| 50002 | Недопустимий перехід статусу |
| 50003 | Недостатньо залишку |
| 50004 | Товар не знайдено або неактивний |
| 50005 | Позиції можна змінювати лише у статусі Submitted |
| 50006 | Клієнта не знайдено |
| 50007 | Статус StockConfirmed ставиться лише через usp_ConfirmOrder |
| 50008 | Кількість має бути додатною |
| 50009 | Замовлення без позицій |

## Структура репозиторію
```
README.md
docs/          опис контекстів, ERD, політика дублювання, приклади документів MongoDB
db/
  p1/          schema.sql, procedures.sql, seed.sql, checks.sql   (БД Замовлень)
  p2/          schema.sql, seed.sql                               (БД Каталогу)
  p3/          collections.js, seed.js, queries.js                (MongoDB)
```