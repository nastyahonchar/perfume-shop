# Предметна область та межі контекстів

## Опис домену
**Nota** — інтернет-магазин парфумерії. Покупці обирають аромат за брендом, сезоном і **нотами піраміди** (верхні, ноти серця, база), оформлюють і оплачують замовлення, користуються сезонними знижками та діляться відгуками й обговорюють аромати. Адміністратори керують каталогом і акціями.

## Три обмежені контексти (Bounded Contexts)
| № | Контекст | Сервіс / БД | Технологія | Сутності, якими він володіє |
|---|----------|-------------|------------|-----------------------------|
| 1 | **Замовлення** | `PerfumeOrdersDb` | SQL Server + ADO.NET/Dapper | Customers, Orders, OrderDetails, OrderItems, OrderStatusHistory, OrderStatusTransitions, Promotions, Products *(локальна копія довідника + власні залишки Stock)* |
| 2 | **Каталог** | `PerfumeCatalogDb` | SQL Server + EF Core | Brands, Perfumes, PerfumeDetails, PerfumeImages, Notes, PerfumeNotes |
| 3 | **Відгуки та обговорення** | `PerfumeReviewsDb` | MongoDB | reviews, discussions, discussionMessages, ratingSummaries |

Кожна сутність належить рівно одному контексту. Між базами **немає жодного FOREIGN KEY** — лише логічні посилання за ідентифікатором (`ProductId`, `productId`).

## Агрегати та життєвий цикл
| Контекст | Агрегат (корінь + частини) | Правило |
|----------|----------------------------|---------|
| Замовлення | **Order** = Orders + OrderDetails (1:1) + OrderItems + OrderStatusHistory | Позиції не існують без замовлення (`ON DELETE CASCADE`), змінюються лише через процедури; статус змінюється лише за дозволеними переходами |
| Замовлення | **Customer** | Не видаляється фізично, якщо має замовлення (`NO ACTION`) — лише `IsDeleted` |
| Замовлення | **Promotion** | Самостійний агрегат; діє на сезон у межах дат |
| Каталог | **Perfume** = Perfumes + PerfumeDetails (1:1) + PerfumeImages (1:N) + PerfumeNotes (M:N) | Деталі, фото й піраміда нот видаляються разом із парфумом (cascade); Brand і Note — довідники, окремі агрегати |
| Відгуки | **Review** = review + вбудовані author/comments/scentProfile | Коментарі й профіль аромату завжди читаються разом із відгуком → embed |
| Відгуки | **Discussion** = discussion + lastMessages (embed) → discussionMessages (reference) | Повідомлень можуть бути тисячі → окрема колекція |

## Статуси замовлення
`Submitted → AwaitingValidation → StockConfirmed → Paid → Shipped`; `Cancelled` — лише до оплати (з Submitted, AwaitingValidation, StockConfirmed).
Допустимі переходи зберігаються в таблиці `OrderStatusTransitions`; `StockConfirmed` виставляє лише `usp_ConfirmOrder` (разом зі списанням залишку).

## Проєктні рішення
- **Первинні ключі.** Orders/Customers/History — `BIGINT IDENTITY` (запис з одного джерела, послідовна вставка, компактні індекси). Perfumes/Brands/Notes — `INT IDENTITY` (малі довідники; `Id` товару копіюється в інші бази й у події, тому має бути коротким і незмінним). У Mongo — `ObjectId` (генерується на клієнті без координації).
- **Права власності на дані.** Ціна, назва, бренд, сезон товару — власність **Каталогу**. Залишок на складі (`Stock`) — власність **Замовлень**, у Каталозі його немає: так уникаємо dual write. Знижки (`Promotions`) — власність **Замовлень**.
- **Сезонна знижка.** Каталог знає лише `Season` парфуму. Замовлення зберігає копію `Products.Season` і при додаванні позиції шукає чинну `Promotion` для цього сезону; розмір знижки фіксується в `OrderItems.Discount`.
- **Нотна піраміда.** `PerfumeNotes` — M:N з власними полями `Layer` (Top/Heart/Base) і `AddedAt`; зворотний індекс `(NoteId, PerfumeId)` прискорює фільтрацію «парфуми з нотою X».
- **Аудит і soft delete.** `CreatedAt/By`, `UpdatedAt/By`, `IsDeleted` — на Orders, Customers, Perfumes, Promotions (довідники без аудиту, бо змінюються рідко).
- **Конкурентність.** `ROWVERSION` на Orders, Customers, Products, Perfumes: два адміни, що одночасно правлять один запис, не зітруть зміни один одного; для замовлень додатково `UPDLOCK` у процедурах.
- **Каскади.** `CASCADE` — для частин агрегату (OrderItems, OrderDetails, History, PerfumeDetails, PerfumeImages, PerfumeNotes). `NO ACTION` — для Orders→Customers, Perfumes→Brands, PerfumeNotes→Notes.
- **Безпека.** Окремий логін застосунку з мінімальними правами на свою БД (`security.sql`).