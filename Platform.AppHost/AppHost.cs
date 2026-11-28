var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sql = builder.AddSqlServer("sql", sqlPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var ordersDb = sql.AddDatabase("OrdersDb", "PerfumeOrdersDb");
var catalogDb = sql.AddDatabase("CatalogDb", "PerfumeCatalogDb");
var identityDb = sql.AddDatabase("IdentityDb", "PerfumeIdentityDb");

var identityServerDb = builder.AddSqlServer("identityserver-sql", sqlPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("IdentityServerDb");

var mongo = builder.AddMongoDB("mongo")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var reviewsDb = mongo.AddDatabase("ReviewsDb", "PerfumeReviewsDb");

var redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var rabbit = builder.AddRabbitMQ("messaging")
    .WithDataVolume()
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

// Траєкторія А: Keycloak запускається контейнером з фіксованою версією образу (26.0), його дані зберігаються
// в окремому контейнері PostgreSQL (keycloak-postgres) з томом. Підключення до бази задається змінними KC_DB*,
// контейнери бачать один одного за іменем ресурсу. Адмін-обліковка admin/admin123 лише для розробки.
var pgPassword = builder.AddParameter("pg-password", secret: true);

var keycloakPostgres = builder.AddPostgres("keycloak-postgres", password: pgPassword)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var keycloakDb = keycloakPostgres.AddDatabase("keycloakdb");

var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.0")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin123")
    .WithEnvironment("KC_DB", "postgres")
    .WithEnvironment("KC_DB_URL", "jdbc:postgresql://keycloak-postgres:5432/keycloakdb")
    .WithEnvironment("KC_DB_USERNAME", "postgres")
    .WithEnvironment("KC_DB_PASSWORD", pgPassword)
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithArgs("start-dev")
    .WithLifetime(ContainerLifetime.Persistent)
    .WaitFor(keycloakDb);

var orders = builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDb).WaitFor(ordersDb)
    .WithReference(redis).WaitFor(redis)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithJwtSettings(builder.Configuration)
    .WithIdentityServerSettings(builder.Configuration, "orders_api")
    .WithKeycloakSettings(builder.Configuration, "orders-api")
    .WithHttpHealthCheck("/health/ready");

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb).WaitFor(catalogDb)
    .WithReference(redis).WaitFor(redis)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithJwtSettings(builder.Configuration)
    .WithIdentityServerSettings(builder.Configuration, "products_api")
    .WithKeycloakSettings(builder.Configuration, "products-api")
    .WithHttpHealthCheck("/health/ready");

var reviews = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb).WaitFor(reviewsDb)
    .WithReference(redis).WaitFor(redis)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithJwtSettings(builder.Configuration)
    .WithHttpHealthCheck("/health/ready");

var auth = builder.AddProject<Projects.Auth_Api>("auth-api")
    .WithReference(identityDb).WaitFor(identityDb)
    .WithJwtSettings(builder.Configuration)
    .WithHttpHealthCheck("/health/ready");

var identityServer = builder.AddProject<Projects.Platform_IdentityServer>("identityserver")
    .WithReference(identityServerDb).WaitFor(identityServerDb)
    .WithHttpEndpoint(port: 5100, name: "public")
    .WithHttpHealthCheck("/health/ready");

var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(orders).WaitFor(orders)
    .WithReference(catalog).WaitFor(catalog)
    .WithReference(reviews).WaitFor(reviews)
    .WithReference(redis).WaitFor(redis)
    .WithAggregatorClient(builder.Configuration)
    .WithHttpHealthCheck("/health/ready");

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(orders)
    .WithReference(catalog)
    .WithReference(reviews)
    .WithReference(auth).WaitFor(auth)
    .WithReference(aggregator).WaitFor(aggregator)
    .WithJwtSettings(builder.Configuration)
    .WithIdentityServerSettings(builder.Configuration, validateAudience: false)
    .WithKeycloakSettings(builder.Configuration, validateAudience: false)
    .WithHttpHealthCheck("/health/ready", endpointName: "public");

builder.Build().Run();