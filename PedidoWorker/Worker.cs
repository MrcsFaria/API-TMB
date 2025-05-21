using Azure.Messaging.ServiceBus;
using ApiTmb.Data;    // para acessar AppDbContext
using ApiTmb.Models;  // para acessar Pedido e StatusPedido
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using ApiTmb.Hubs;

namespace PedidoWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ServiceBusProcessor _processor;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<PedidoHub> _hubContext;


        public Worker(ServiceBusClient client, IServiceProvider serviceProvider, ILogger<Worker> logger, IHubContext<PedidoHub> hubContext)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;

            _processor = client.CreateProcessor("pedidosqueue", new ServiceBusProcessorOptions());
            _processor.ProcessMessageAsync += ProcessMessageHandler;
            _processor.ProcessErrorAsync += ErrorHandler;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await _processor.StartProcessingAsync(cancellationToken);
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }

        private async Task ProcessMessageHandler(ProcessMessageEventArgs args)
        {
            var jsonString = args.Message.Body.ToString();

            _logger.LogInformation($"Processando pedido {jsonString}");

            // Desserializa o JSON para objeto Pedido
            Pedido pedidoMensagem;
            try
            {
                pedidoMensagem = JsonSerializer.Deserialize<Pedido>(jsonString);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao desserializar mensagem: {ex.Message}");
                await args.DeadLetterMessageAsync(args.Message, "ErroDeDeserializacao", ex.Message);
                return;
            }

            // Criar escopo para injetar o DbContext
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Procurar o pedido no banco pelo Id (que é Guid)
                var pedido = await context.Pedidos.FindAsync(pedidoMensagem.Id);
                if (pedido != null)
                {
                    pedido.Status = "Processando";
                    await context.SaveChangesAsync();

                    // 🔔 Notificar os clientes conectados
                    await _hubContext.Clients.All.SendAsync("PedidoAtualizado", new
                    {
                        pedido.Id,
                        pedido.Cliente,
                        pedido.Produto,
                        pedido.Valor,
                        pedido.Status,
                        pedido.DataCriacao
                    });

                    await Task.Delay(5000);

                    pedido.Status = "Finalizado";
                    await context.SaveChangesAsync();

                    // 🔔 Notificar novamente
                    await _hubContext.Clients.All.SendAsync("PedidoAtualizado", new
                    {
                        pedido.Id,
                        pedido.Cliente,
                        pedido.Produto,
                        pedido.Valor,
                        pedido.Status,
                        pedido.DataCriacao
                    });
                }
                else
                {
                    _logger.LogWarning($"Pedido com Id {pedidoMensagem.Id} não encontrado no banco.");
                }
            }

            await args.CompleteMessageAsync(args.Message);
        }
        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, args.Exception.Message);
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Pode ficar vazio porque o processamento está no evento do ServiceBus
            await Task.CompletedTask;
        }
    }
}
