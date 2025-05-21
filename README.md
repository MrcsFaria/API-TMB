# API de Pedidos com .NET 7, EF Core, PostgreSQL, Azure Service Bus e SignalR
## Visão Geral
Essa API gerencia pedidos (Pedido) armazenados em banco PostgreSQL, utiliza Azure Service Bus para enfileiramento de mensagens, e notifica clientes conectados via SignalR sobre alterações de status do pedido.

Tecnologias usadas:

.NET 7 ou superior

Entity Framework Core com PostgreSQL (Npgsql)

Azure Service Bus (Azure.Messaging.ServiceBus)

SignalR para WebSocket em tempo real

DotNetEnv para variáveis de ambiente .env

CORS configurado para front React em localhost:5173

## Requisitos
.NET 7 SDK (ou superior)

PostgreSQL instalado e rodando

Conta no Azure com Service Bus criado (com fila configurada)

Editor de código (VSCode, Visual Studio, etc)

Node.js e React app para front-end opcional

(Opcional) DotNet CLI para rodar o projeto

## Estrutura do Projeto
ApiTmb.Data: Contexto EF Core AppDbContext e configurações do modelo

ApiTmb.Models: Modelo Pedido

ApiTmb.Controllers: Controller RESTful OrdersController

ApiTmb.Services: Serviço de envio para Azure Service Bus PedidoServiceBusSender e worker PedidoWorker

PedidoWorker: Worker background que consome mensagens da fila e atualiza status dos pedidos

ApiTmb.Hubs: SignalR Hub PedidoHub para comunicação em tempo real

## Configuração do Ambiente
Clonando o repositório


```env
git clone https://github.com/MrcsFaria/API-TMB
cd API-TMB
```


## Banco de Dados PostgreSQL
Crie um banco PostgreSQL (exemplo: api_tmb)

Configure o usuário e senha

## Azure Service Bus
Crie um namespace no Azure Service Bus

Crie uma fila (exemplo: pedidosqueue)

Pegue a connection string da namespace e o nome da fila

## Arquivo .env (usado pelo DotNetEnv)
Crie na raiz do projeto um arquivo .env com as variáveis:

```env
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=api_tmb;Username=seu_usuario;Password=sua_senha"
AzureServiceBus__ConnectionString="Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=..."
AzureServiceBus__QueueName="pedidosqueue"
```
## appsettings.json
Certifique-se que o arquivo appsettings.json está assim (pode deixar strings vazias pois usa .env):
```bash
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "AzureServiceBus": {
    "ConnectionString": "",
    "QueueName": ""
  },
  "AllowedHosts": "*"
}
```

## Executando a Aplicação
Restaure pacotes e compile

Na raiz do projeto:
```bash
dotnet restore
dotnet build
```

Crie e aplique as migrations (caso use migrations)

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Execute a aplicação
```bash
dotnet run
```

A API estará disponível em https://localhost:5001 ou http://localhost:5000 (verifique saída no console).

## Testando Endpoints
GET /api/orders — lista todos os pedidos

GET /api/orders/{id} — consulta pedido pelo Id (GUID)

POST /api/orders — cria um pedido (JSON: { "cliente": "...", "produto": "...", "valor": 123.45 })

PUT /api/orders/{id} — atualiza pedido

DELETE /api/orders/{id} — deleta pedido

## Funcionamento Interno
Quando um pedido é criado, ele é salvo no banco com status inicial.

A mensagem do pedido é enviada para Azure Service Bus.

O PedidoWorker consome mensagens da fila, atualiza o status do pedido para Processando, depois Finalizado, e usa SignalR para notificar clientes conectados.

O SignalR Hub fica em /pedidoHub.

## CORS e Frontend
API libera CORS para origem http://localhost:5173 (onde o React está rodando)

Frontend pode conectar SignalR ao hub /pedidoHub para receber atualizações

## Observações
Ajuste a connection string PostgreSQL conforme seu ambiente.

Configure sua fila Azure Service Bus e preencha corretamente .env.

Para produção, revise configurações de CORS e segurança.

O worker de mensagens usa injeção de escopo para DbContext e SignalR.

SignalR usa Clients.All.SendAsync para notificação global.
