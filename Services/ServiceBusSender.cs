using Azure.Messaging.ServiceBus;
using System.Text.Json;
using ApiTmb.Models;

namespace ApiTmb.Services
{
    public class PedidoServiceBusSender
    {
        private readonly string _connectionString;
        private readonly string _queueName;
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;

        public PedidoServiceBusSender(IConfiguration configuration)
        {
            _connectionString = configuration["AzureServiceBus:ConnectionString"];
            _queueName = configuration["AzureServiceBus:QueueName"];

            _client = new ServiceBusClient(_connectionString);
            _sender = _client.CreateSender(_queueName);
        }

        public async Task EnviarPedidoAsync(Pedido pedido)
        {
            var mensagemJson = JsonSerializer.Serialize(pedido);
            var message = new ServiceBusMessage(mensagemJson);

            await _sender.SendMessageAsync(message);
        }
    }
}
