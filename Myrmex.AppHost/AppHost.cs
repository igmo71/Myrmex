var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var myrmexDatabase = builder.AddConnectionString("MyrmexDatabase");

var apiService = builder.AddProject<Projects.Myrmex_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(myrmexDatabase)
    .WithEnvironment("Myrmex__Integrations__OneC__Username",
        builder.Configuration["Myrmex:Integrations:OneC:Username"])
    .WithEnvironment("Myrmex__Integrations__OneC__Password",
        builder.Configuration["Myrmex:Integrations:OneC:Password"]);

builder.AddProject<Projects.Myrmex_WebApp>("webapp")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(myrmexDatabase)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
