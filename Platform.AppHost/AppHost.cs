using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var useSqlContainer = builder.Configuration.GetValue<bool>("UseSqlContainer");

IResourceBuilder<IResourceWithConnectionString> ordersDb;

if (useSqlContainer)
{
    ordersDb = builder
        .AddSqlServer("sql")
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent)
        .AddDatabase("OrdersDb", "PerfumeOrdersDb");
}
else
{
    ordersDb = builder.AddConnectionString("OrdersDb");
}

builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDb)
    .WaitFor(ordersDb);

builder.Build().Run();