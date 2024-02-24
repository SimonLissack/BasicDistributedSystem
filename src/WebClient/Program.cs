using Infrastructure.Messaging.RabbitMq;
using Infrastructure.Telemetry;
using OpenTelemetry.Trace;
using WebClient;
using WebClient.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging
    .ClearProviders()
    .AddOpenTelemetryLogging();

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true)
    .AddEnvironmentVariables();

builder.Services
    .AddOptions<RabbitMqConfiguration>()
    .Bind(builder.Configuration.GetSection(RabbitMqConfiguration.SectionName));

builder.Services
    .AddOptions<WebClientConfiguration>()
    .Bind(builder.Configuration.GetSection(WebClientConfiguration.SectionName));

builder.Services
    .InstallRabbitMqInfrastructure()
    .AddSingleton<IPingRepository, InMemoryPingRepository>()
    .AddTransient<IWorkRequestPublisherService, WorkRequestPublisherService>()
    .AddHostedService<WorkResponseConsumerHostedService>();

builder.Services.AddOpenTelemetryStack(
    builder.Environment.EnvironmentName,
    b => b
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
