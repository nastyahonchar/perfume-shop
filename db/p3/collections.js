const existing = db.getCollectionNames();

// reviews: вбудовані author, comments, scentProfile; посилання discussionId
if (!existing.includes("reviews")) {
  db.createCollection("reviews", {
    validator: {
      $jsonSchema: {
        bsonType: "object",
        required: ["productId", "productName", "author", "rating", "createdAt"],
        properties: {
          productId:   { bsonType: ["int", "long"] },
          productName: { bsonType: "string" },
          brandName:   { bsonType: "string" },
          author: {
            bsonType: "object",
            required: ["userId", "displayName"],
            properties: { userId: { bsonType: "string" }, displayName: { bsonType: "string" } }
          },
          rating: { bsonType: "int", minimum: 1, maximum: 5 },
          text:   { bsonType: "string", maxLength: 4000 },
          photos: { bsonType: "array", items: { bsonType: "string" } },
          verifiedPurchase: { bsonType: "bool" },
          scentProfile: {
            bsonType: "object",
            properties: {
              longevityHours: { bsonType: ["int", "double"], minimum: 0, maximum: 48 },
              sillage: { enum: ["intimate", "moderate", "strong", "enormous"] },
              seasonOfWear: { bsonType: "array", items: { enum: ["Spring", "Summer", "Autumn", "Winter"] } },
              notesRated: {
                bsonType: "array",
                items: {
                  bsonType: "object",
                  required: ["note", "score"],
                  properties: { note: { bsonType: "string" }, score: { bsonType: "int", minimum: 1, maximum: 5 } }
                }
              }
            }
          },
          comments: { bsonType: "array", items: { bsonType: "object", required: ["author", "text", "createdAt"] } },
          discussionId: { bsonType: "objectId" }
        }
      }
    }
  });
}

// discussions: тема і 3 останні повідомлення вбудовані, усі повідомлення в discussionMessages
if (!existing.includes("discussions")) {
  db.createCollection("discussions", {
    validator: {
      $jsonSchema: {
        bsonType: "object",
        required: ["productId", "productName", "title", "createdBy", "createdAt", "messageCount"],
        properties: {
          productId:    { bsonType: ["int", "long"] },
          title:        { bsonType: "string", minLength: 3, maxLength: 200 },
          messageCount: { bsonType: ["int", "long"], minimum: 0 },
          lastMessages: { bsonType: "array", maxItems: 3 }
        }
      }
    }
  });
}

// discussionMessages: повідомлення посилаються на тему через discussionId
if (!existing.includes("discussionMessages")) {
  db.createCollection("discussionMessages", {
    validator: {
      $jsonSchema: {
        bsonType: "object",
        required: ["discussionId", "author", "text", "createdAt"],
        properties: {
          discussionId: { bsonType: "objectId" },
          text: { bsonType: "string", minLength: 1, maxLength: 4000 }
        }
      }
    }
  });
}

// ratingSummaries: готові підсумки рейтингу по товару (_id = productId)
if (!existing.includes("ratingSummaries")) {
  db.createCollection("ratingSummaries");
}

// Індекси. Біля кожного вказано запит.
db.reviews.createIndex({ productId: 1, createdAt: -1 }, { name: "ix_reviews_product_created" });   // відгуки товару, нові першими
db.reviews.createIndex({ productId: 1, rating: -1 }, { name: "ix_reviews_product_rating" });       // найвищі й найнижчі оцінки товару
db.reviews.createIndex({ "author.userId": 1 }, { name: "ix_reviews_author" });                     // відгуки одного користувача
db.reviews.createIndex({ text: "text", title: "text" }, { name: "tx_reviews_text", default_language: "none" }); // пошук за текстом
db.discussions.createIndex({ productId: 1, lastMessageAt: -1 }, { name: "ix_disc_product_last" }); // теми товару за активністю
db.discussionMessages.createIndex({ discussionId: 1, createdAt: 1 }, { name: "ix_msg_disc_created" }); // повідомлення теми по порядку

print("collections ready");