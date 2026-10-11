const oid = (prefix, n) => ObjectId(prefix + n.toString(16).padStart(16, "0"));
const R = n => oid("66f0c2a1", n);   // reviews
const D = n => oid("66f0c3a1", n);   // discussions
const M = n => oid("66f0c4a1", n);   // discussionMessages

function insertIfMissing(coll, docs) {
  let added = 0;
  docs.forEach(d => {
    if (db[coll].countDocuments({ _id: d._id }) === 0) { db[coll].insertOne(d); added++; }
  });
  print(coll + ": додано " + added + " з " + docs.length);
}

const u = (id, name) => ({ userId: id, displayName: name });

insertIfMissing("reviews", [
  // повний відгук: вкладені об'єкти й масиви, фото, посилання на обговорення
  { _id: R(1), productId: 1, productName: "Pumpkin Velours", brandName: "Maison Aurelle",
    author: u("u-1", "Олена"), rating: 5, title: "Осінь у флаконі",
    text: "Тепле гарбузове серце і кориця, тримається цілий день.",
    verifiedPurchase: true, photos: ["rev1-a.jpg", "rev1-b.jpg"],
    scentProfile: { longevityHours: 9, sillage: "moderate", seasonOfWear: ["Autumn", "Winter"],
                    notesRated: [ { note: "Pumpkin", score: 5 }, { note: "Vanilla", score: 4 }, { note: "Cinnamon", score: 5 } ] },
    comments: [
      { author: u("u-7", "Андрій"), text: "Згоден, мій улюблений на жовтень", createdAt: ISODate("2026-10-03T09:30:00Z") },
      { author: u("u-3", "Ірина"),  text: "А на літо не занадто солодкий?",   createdAt: ISODate("2026-10-04T12:10:00Z") } ],
    discussionId: D(1), createdAt: ISODate("2026-10-02T18:00:00Z") },

  // швидкий відгук: лише оцінка
  { _id: R(2), productId: 1, productName: "Pumpkin Velours", brandName: "Maison Aurelle",
    author: u("u-5", "Марко"), rating: 4, createdAt: ISODate("2026-10-05T08:15:00Z") },

  // відгук із фото
  { _id: R(3), productId: 5, productName: "Spiced Orchard", brandName: "Velvet Atelier",
    author: u("u-2", "Марина"), rating: 5, text: "Яблуко з корицею, як пиріг на кухні в неділю.",
    verifiedPurchase: true, photos: ["rev3-a.jpg"], createdAt: ISODate("2026-09-28T14:00:00Z") },

  // відгук з унікальним вкладеним полем wearInfo
  { _id: R(4), productId: 5, productName: "Spiced Orchard", brandName: "Velvet Atelier",
    author: u("u-9", "Софія"), rating: 3, text: "Після двох тижнів носіння не впевнена, що це мій аромат.",
    wearInfo: { daysTested: 14, occasion: "office" },
    comments: [ { author: u("u-2", "Марина"), text: "Дай йому годину, він розкривається", createdAt: ISODate("2026-10-01T10:00:00Z") } ],
    discussionId: D(2), createdAt: ISODate("2026-09-30T20:45:00Z") },

  { _id: R(5), productId: 2, productName: "Citrus Riviera", brandName: "Maison Aurelle",
    author: u("u-4", "Дмитро"), rating: 4, text: "Свіжий, але вечором уже майже не чути.",
    scentProfile: { longevityHours: 3, sillage: "intimate", seasonOfWear: ["Summer"],
                    notesRated: [ { note: "Bergamot", score: 5 }, { note: "Mint", score: 3 } ] },
    createdAt: ISODate("2026-07-14T11:20:00Z") },

  { _id: R(6), productId: 6, productName: "Black Iris", brandName: "Velvet Atelier",
    author: u("u-1", "Олена"), rating: 5, text: "Ірис і уд, розкішно.", verifiedPurchase: true,
    scentProfile: { longevityHours: 12, sillage: "strong", seasonOfWear: ["Autumn", "Winter", "Spring"] },
    createdAt: ISODate("2026-08-20T19:30:00Z") },

  { _id: R(7), productId: 7, productName: "Amber Harvest", brandName: "Casa Ambra",
    author: u("u-8", "Тарас"), rating: 2, text: "Занадто кавовий, очікував більше бурштину.",
    createdAt: ISODate("2026-10-06T16:00:00Z") },

  { _id: R(8), productId: 7, productName: "Amber Harvest", brandName: "Casa Ambra",
    author: u("u-6", "Каріна"), rating: 5, createdAt: ISODate("2026-10-07T07:50:00Z") }
]);

// обговорення: лічильник і 3 останні повідомлення вбудовані, усі повідомлення в discussionMessages
insertIfMissing("discussions", [
  { _id: D(1), productId: 1, productName: "Pumpkin Velours", title: "З чим носити гарбузовий аромат?",
    createdBy: u("u-1", "Олена"), createdAt: ISODate("2026-10-02T18:05:00Z"), isPinned: true, tags: ["autumn", "gourmand"],
    messageCount: 4, lastMessageAt: ISODate("2026-10-06T21:00:00Z"),
    lastMessages: [
      { author: "Ірина",  text: "Мені пасує з теплим светром", createdAt: ISODate("2026-10-05T10:00:00Z") },
      { author: "Андрій", text: "Для офісу занадто солодкий?", createdAt: ISODate("2026-10-05T12:00:00Z") },
      { author: "Олена",  text: "Спробуй один бризок, він м'який", createdAt: ISODate("2026-10-06T21:00:00Z") } ] },
    { _id: D(2), productId: 5, productName: "Spiced Orchard", title: "Чи підходить для офісу?",
    createdBy: u("u-9", "Софія"), createdAt: ISODate("2026-09-30T20:50:00Z"), tags: ["office"],
    messageCount: 1, lastMessageAt: ISODate("2026-10-01T10:00:00Z"),
    lastMessages: [ { author: "Марина", text: "Для офісу беріть один бризок", createdAt: ISODate("2026-10-01T10:00:00Z") } ] }
]);

insertIfMissing("discussionMessages", [
  { _id: M(1), discussionId: D(1), author: u("u-1", "Олена"),  text: "Цікаво, з чим ви його носите?",   createdAt: ISODate("2026-10-02T18:05:00Z") },
  { _id: M(2), discussionId: D(1), author: u("u-3", "Ірина"),  text: "Мені пасує з теплим светром",     createdAt: ISODate("2026-10-05T10:00:00Z") },
  { _id: M(3), discussionId: D(1), author: u("u-7", "Андрій"), text: "Для офісу занадто солодкий?",     createdAt: ISODate("2026-10-05T12:00:00Z") },
  { _id: M(4), discussionId: D(1), author: u("u-1", "Олена"),  text: "Спробуй один бризок, він м'який", createdAt: ISODate("2026-10-06T21:00:00Z") },
    { _id: M(5), discussionId: D(2), author: u("u-2", "Марина"), text: "Для офісу беріть один бризок", createdAt: ISODate("2026-10-01T10:00:00Z") }
]);

// ratingSummaries перераховується з відгуків, тому повторний запуск дає той самий результат
db.reviews.aggregate([
  { $group: { _id: { p: "$productId", r: "$rating" }, n: { $sum: 1 },
              productName: { $first: "$productName" }, brandName: { $first: "$brandName" } } },
  { $group: { _id: "$_id.p", productName: { $first: "$productName" }, brandName: { $first: "$brandName" },
              count: { $sum: "$n" }, ratingSum: { $sum: { $multiply: ["$_id.r", "$n"] } },
              dist: { $push: { k: { $toString: "$_id.r" }, v: "$n" } } } },
  { $project: { productName: 1, brandName: 1, count: 1,
                average: { $round: [{ $divide: ["$ratingSum", "$count"] }, 2] },
                distribution: { $arrayToObject: "$dist" }, updatedAt: "$$NOW" } },
  { $merge: { into: "ratingSummaries", on: "_id", whenMatched: "replace", whenNotMatched: "insert" } }
]);

print("seed done");