using ApiTmb.Data;
using Microsoft.EntityFrameworkCore;
using ApiTmb.Services;
using ApiTmb.Hubs;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
var serviceBusConn = builder.Configuration["AzureServiceBus:ConnectionString"];
var queueName = builder.Configuration["AzureServiceBus:QueueName"];

// Adiciona o DbContext ao container de DI
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Adiciona CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Porta do seu React
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Outros serviços, controllers etc
builder.Services.AddControllers();

builder.Services.AddSingleton(sp =>
{
    var connectionString = builder.Configuration.GetValue<string>("AzureServiceBus:ConnectionString");
    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException("AzureServiceBus:ConnectionString não está configurada.");

    return new Azure.Messaging.ServiceBus.ServiceBusClient(connectionString);
});


// Se tiver um serviço que envia pedido via ServiceBus
builder.Services.AddSingleton<PedidoServiceBusSender>();

// Registra o worker que vai consumir mensagens do Service Bus
builder.Services.AddHostedService<PedidoWorker.Worker>();

builder.Services.AddSignalR();

var app = builder.Build();

// Usa CORS (coloque antes de MapControllers)
app.UseCors("PermitirFrontend");

app.MapHub<PedidoHub>("/pedidoHub");

app.MapControllers();

app.Run();
