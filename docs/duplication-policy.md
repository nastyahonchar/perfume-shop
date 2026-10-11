# Політика дублювання даних і узгодженості

| Поле | Де зберігається | Джерело (власник даних) | Політика узгодженості |
|------|-----------------|-------------------------|-----------------------|
| `OrderItems.ProductName`, `OrderItems.UnitPrice` | БД Замовлень | Perfumes (БД Каталогу) | Знімок на момент оформлення, не оновлюється |
| `OrderItems.Discount` | БД Замовлень | Promotions (БД Замовлень) | Знімок знижки на момент додавання позиції, не оновлюється |
| `Products.Name`, `Products.BrandName`, `Products.Price`, `Products.Season`, `Products.IsActive` | БД Замовлень | Perfumes і Brands (БД Каталогу) | Локальна копія довідника, оновлюється подіями з Каталогу |
| `Products.Stock` | БД Замовлень | БД Замовлень | Власне поле, не дублюється. Каталог залишку не знає |
| `Promotions.Season` | БД Замовлень | Список сезонів у Каталозі (`Perfumes.Season`) | Спільний набір значень: Spring, Summer, Autumn, Winter, AllYear |
| `reviews.productName`, `reviews.brandName` | БД Відгуків (MongoDB) | Perfumes і Brands (БД Каталогу) | Оновлюється подіями з Каталогу |
| `discussions.productName` | БД Відгуків | Perfumes (БД Каталогу) | Оновлюється подіями з Каталогу |
| `ratingSummaries.productName`, `ratingSummaries.brandName` | БД Відгуків | Perfumes і Brands (БД Каталогу) | Оновлюється подіями з Каталогу |
| `ratingSummaries.count`, `average`, `distribution` | БД Відгуків | reviews (БД Відгуків) | Похідні дані одного контексту, перераховуються агрегацією |
| `reviews.author.displayName` | БД Відгуків | Customers.FullName (БД Замовлень) | Знімок на момент написання, не оновлюється |

## Принципи
- Між базами немає жодного FOREIGN KEY. Посилання `ProductId` у БД Замовлень і `productId` у MongoDB є звичайними значеннями без обмеження.
- Кожне продубльоване поле має одного власника. Копії, які оновлюються, змінює лише власник даних (Каталог), тому два сервіси не пишуть у те саме поле.
- Знімки (`OrderItems`, `author.displayName`) змінювати не можна: вони фіксують стан на момент події.