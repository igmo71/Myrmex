var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var sqlServer = builder.AddSqlServer("sql");
var myrmexDatabase = sqlServer.AddDatabase("MyrmexDatabase");

var apiService = builder.AddProject<Projects.Myrmex_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(myrmexDatabase)
    .WaitFor(myrmexDatabase);

builder.AddProject<Projects.Myrmex_WebApp>("webapp")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(myrmexDatabase)
    .WaitFor(myrmexDatabase)
    .WithReference(apiService)
    .WaitFor(apiService)
    ;

builder.Build().Run();
