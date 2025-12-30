using OracleSQLCore.Interface;
using OracleSQLCore.Repositories;
using OracleSQLCore.Services;
using OracleSQLCore.Services.Imp;
using MongoDB.Driver;
using MongoDBCore.Entities.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Polly.Extensions.Http;
using Polly;
using MassTransit;
using MongoDBCore.Repositories.Consumer;
using Polly.Retry;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options => // C?u hình Swagger phân bi?t tên file trùng nhau b?ng namespace
{
    // Dòng này c?c k? quan tr?ng ?? s?a l?i b?n ?ang g?p
    // Nó s? dùng Full Name (Namespace + ClassName) ?? làm ID trong Swagger
    options.CustomSchemaIds(type => type.FullName);
});

// ===== ConnectionStrings =====
// 1. L?y connection string t? c?u hình (appsettings ho?c Docker Env)

#region ConnectionString
var oracleConnectionString =
    builder.Configuration.GetConnectionString("OracleDbConnection");

if (string.IsNullOrWhiteSpace(oracleConnectionString))
    throw new InvalidOperationException("OracleDbConnection is missing");

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDBSettings"));

builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<MongoDbSettings>>().Value);

var mongoConnectionString =
    builder.Configuration["MongoDBSettings:ConnectionString"];

if (string.IsNullOrWhiteSpace(mongoConnectionString))
{
    throw new InvalidOperationException("MongoDbConnection is missing");
}
#endregion

#region Oracle DI, Service + Repo
// ===== Oracle DI =====
//2. ??ng ký Repository vào h? th?ng DI
// ??ng ký cho Oracle
builder.Services.AddScoped<OracleSQLCore.Interface.ICustomerRepository>(
    _ => new OracleSQLCore.Repositories.CustomerRepository(oracleConnectionString)
);
builder.Services.AddScoped<ICustomerService, CustomerService>();



builder.Services.AddScoped<OracleSQLCore.Interface.IInsuranceTypeRepository>(
    _ => new OracleSQLCore.Repositories.InsuranceTypeRepository(oracleConnectionString)
);

builder.Services.AddScoped<OracleSQLCore.Interface.IPolicyRepository>(
    _ => new OracleSQLCore.Repositories.PolicyRepository(oracleConnectionString)
);



builder.Services.AddScoped<IInsuranceTypeService, InsuranceTypeService>();
builder.Services.AddScoped<IAgentRepository>(sp => // S? d?ng Factory ?? truy?n string vào Constructor, g?i là Factory Registration
    new AgentRepository(oracleConnectionString!));
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
#endregion
// End
// ??ng ký cho MongoDB
#region MongoDb, Repo
builder.Services.AddScoped<MongoDBCore.Interfaces.IPolicyRepository, MongoDBCore.Repositories.PolicyRepository>();
builder.Services.AddScoped<MongoDBCore.Interfaces.ICustomerRepository, MongoDBCore.Repositories.CustomerRepository>();
builder.Services.AddScoped<MongoDBCore.Interfaces.IAgentRepository, MongoDBCore.Repositories.AgentRepository>();
builder.Services.AddScoped<MongoDBCore.Services.ICustomerService, MongoDBCore.Services.Imp.CustomerService>();
builder.Services.AddScoped<MongoDBCore.Interfaces.IInsuranceRepository, MongoDBCore.Repositories.InsuranceTypeRepository>();
builder.Services.AddSingleton<IMongoClient>(
    _ => new MongoClient(mongoConnectionString)
);
#endregion
// ===== HealthChecks =====
builder.Services.AddHealthChecks()
    .AddOracle(oracleConnectionString, name: "oracle-db")
    .AddMongoDb(
        sp => sp.GetRequiredService<IMongoClient>(),
        name: "mongodb"
    );

// ??ng ký các d?ch v? hi?n có c?a b?n
builder.Services.AddControllers();

// ??ng ký HttpClient ?? các service có th? g?i nhau
builder.Services.AddHttpClient();



#region Polly
// C?u hình chính sách: Th? l?i 3 l?n, m?i l?n cách nhau 2 giây n?u g?p l?i m?ng ho?c l?i 5xx
//g?n Polly vào HttpClient, hàm t?o Create controller Oracle
// ?o?n codde này có nhi?m v? “M?i khi tôi g?i CreateClient("MongoSyncClient") thì m?i HTTP request g?i ?i s? t? ??ng có retry + circuit breaker.”

#region Cách 1 áp d?ng v?i Service A ??ng b? v?i Service B bình th??ng áp d?ng c? 2 g?n vào HttpClient và sau ?ó g?i b?ng PostJson
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError() // b?t l?i t?m th?i include L?i m?ng (HttpRequestException), HTTP 5xx, HTTP 408 (Timeout)
    .WaitAndRetryAsync(
        retryCount: 3, // n?u g?p l?i th? l?i 3 l?n
        sleepDurationProvider: _ => TimeSpan.FromSeconds(2)); // ch? 2 giây, giúp service ?ích có th?i gian h?i ph?c, tránh th?i gian request

//polly kieu 1
var circuitBreaker = HttpPolicyExtensions // c?ng b?t cùng các lo?i l?i nh? retry
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5, // n?u có 5 yêu c?u liên ti?p (sau khi retry xong) v?n lõi -> Circuit s? OPEN => ?? 1 request = ?ã retry xong m?i tính
        durationOfBreak: TimeSpan.FromSeconds(30)); // trong 30 giây m?i request không g?i HTTP, Không retry, Fail Ngay

builder.Services.AddHttpClient("MongoSyncClient") // // g?n polly vào client -> CreateClient("MongoSyncClient") ? t? ??ng có retry
    .AddPolicyHandler(retryPolicy) // m?i request g?i ?i -> retry theo retryPolicy
    .AddPolicyHandler(circuitBreaker); // N?u l?i nhi?u -> circuit breaker qu?n lý
// Chú ý th? t? trong Pollyu r?t quan tr?ng. Retry tr??c -> Circuit breaker sau -> ?ây là th? t? ?úng

#endregion


#region Cách 2 . Áp d?ng v?i RabbitMQ ch? Retry
//poly kieu 2 - dùng trong consumer và controlelr
builder.Services.AddSingleton<AsyncRetryPolicy>(sp => // n?u có th? thì s? làm ki?u interface IASyncPolicy, còn ?ây là cách c? th?
{
    return Policy
        .Handle<Exception>() // X? lý khi có b?t k? l?i nào x?y ra
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Th? l?i sau 2s, 4s, 8s
            (exception, timeSpan, retryCount, context) =>
            {
                // B?n có th? log l?i ? ?ây n?u c?n
                Console.WriteLine($"Retry {retryCount} due to: {exception.Message}");
            });
});

#endregion

#endregion

#region RabbitMQ
//RabbitMQ
//Bus (MassTransit bus) là n?i các consumer ??ng ký ?? nh?n message.
//Ch? project nào có consumer m?i c?n c?u hình consumer + bus ?? nh?n message.
builder.Services.AddMassTransit(x =>
{
    // --- ??NG KÝ CÁC CONSUMER ---
    x.AddConsumer<PolicyChangedConsumer>();
    x.AddConsumer<CustomerCreatedConsumer>();
    x.AddConsumer<PolicyCreatedConsumer>();
    x.AddConsumer<InsuranceTypeCreateConsumer>(); // Thêm dòng này cho Insurance
    x.AddConsumer<AgentCreatedConsumer>(); // Thêm dòng này cho Insurance
    x.UsingRabbitMq((context, cfg) =>
    {
        // THÊM DÒNG NÀY: Áp d?ng cho m?i Endpoint bên d??i
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // --- ENDPOINT 1: Cho Customer ---
        cfg.ReceiveEndpoint("customer-sync-queue", e =>
        {
            e.ConfigureConsumer<CustomerCreatedConsumer>(context);
        });

        // --- ENDPOINT 2: Cho Insurance Type ---
        cfg.ReceiveEndpoint("insurance-type-sync-queue", e => // Tên queue nên ??t riêng bi?t
        {
            e.ConfigureConsumer<InsuranceTypeCreateConsumer>(context);
        });
        // --- ENDPOINT 3: Cho Agents ---
        cfg.ReceiveEndpoint("agent-sync-queue", e =>
        {
            e.ConfigureConsumer<AgentCreatedConsumer>(context);
        });

        // --- ENDPOINT 4: Cho Policy ---
        cfg.ReceiveEndpoint("policy-sync-queue", e =>
        {
            e.ConfigureConsumer<PolicyCreatedConsumer>(context);
        });

        cfg.ReceiveEndpoint("policy-changed-sync-queue", e =>
        {
            e.ConfigureConsumer<PolicyChangedConsumer>(context);
        });
    });
});
#endregion




var app = builder.Build();




if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
