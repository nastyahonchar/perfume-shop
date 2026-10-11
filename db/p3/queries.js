// Приклади запитів і агрегацій.

// 1. Документи мають різні набори полів
db.reviews.find({}, { rating: 1, text: 1, photos: 1, scentProfile: 1, wearInfo: 1, comments: 1 }).limit(8);

// 2. Відгуки товару, нові першими
db.reviews.find({ productId: 1 }).sort({ createdAt: -1 });

// 3. Лише відгуки з фото або з оцінками нот
db.reviews.find({ photos: { $exists: true } });
db.reviews.find({ "scentProfile.notesRated.note": "Pumpkin" });

// 4. Середній рейтинг і кількість відгуків по товарах
db.reviews.aggregate([
  { $group: { _id: "$productId", name: { $first: "$productName" }, avg: { $avg: "$rating" }, count: { $sum: 1 } } },
  { $sort: { avg: -1 } }
]);

// 5. Які ноти отримують найвищі оцінки
db.reviews.aggregate([
  { $unwind: "$scentProfile.notesRated" },
  { $group: { _id: "$scentProfile.notesRated.note", avgScore: { $avg: "$scentProfile.notesRated.score" }, votes: { $sum: 1 } } },
  { $sort: { avgScore: -1, votes: -1 } }
]);

// 6. Відгук разом з обговоренням
db.reviews.aggregate([
  { $match: { discussionId: { $exists: true } } },
  { $lookup: { from: "discussions", localField: "discussionId", foreignField: "_id", as: "discussion" } },
  { $unwind: "$discussion" },
  { $project: { productName: 1, rating: 1, "discussion.title": 1, "discussion.messageCount": 1 } }
]);

// 7. Усі повідомлення однієї теми по порядку
db.discussionMessages.find({ discussionId: ObjectId("66f0c3a10000000000000001") }).sort({ createdAt: 1 });

// 8. Готові підсумки рейтингу
db.ratingSummaries.find().sort({ average: -1 });

// 9. Оновлення продубльованої назви товару
db.reviews.updateMany({ productId: 1 }, { $set: { productName: "Pumpkin Velours" } });
db.discussions.updateMany({ productId: 1 }, { $set: { productName: "Pumpkin Velours" } });
db.ratingSummaries.updateOne({ _id: 1 }, { $set: { productName: "Pumpkin Velours" } });

// 10. Валідація відхиляє некоректний документ
try {
  db.reviews.insertOne({ productId: 1, productName: "X", author: { userId: "u-x", displayName: "X" }, rating: 7, createdAt: new Date() });
} catch (e) {
  print("Відхилено: " + e.codeName);
}