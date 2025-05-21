using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiTmb.Services
{
    public class PedidoWorker : BackgroundService
    {
        private readonly ILogger<PedidoWorker> _logger;

        public PedidoWorker(ILogger<PedidoWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PedidoWorker iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Aqui você vai colocar a lógica para consumir mensagens do Service Bus
                // e atualizar o status dos pedidos

                _logger.LogInformation("PedidoWorker rodando em: {time}", DateTimeOffset.Now);

                // Exemplo: aguardar 5 segundos (simulando trabalho)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.LogInformation("PedidoWorker finalizado.");
        }
    }
}
