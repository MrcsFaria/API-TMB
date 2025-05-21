using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiTmb.Data;
using ApiTmb.Models;
using ApiTmb.Services;

namespace ApiTmb.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PedidoServiceBusSender _serviceBusSender;

        public OrdersController(AppDbContext context, PedidoServiceBusSender serviceBusSender)
        {
            _context = context;
            _serviceBusSender = serviceBusSender;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Pedido>>> GetOrders()
        {
            return await _context.Pedidos.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Pedido>> GetOrder(Guid id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
                return NotFound();

            return pedido;
        }

        [HttpPost]
        public async Task<ActionResult<Pedido>> PostOrder(Pedido pedido)
        {
            if (string.IsNullOrEmpty(pedido.Cliente) || string.IsNullOrEmpty(pedido.Produto) || pedido.Valor <= 0)
            return BadRequest("Dados do pedido inválidos.");
            
            pedido.Id = Guid.NewGuid();
            pedido.DataCriacao = DateTime.UtcNow;

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            await _serviceBusSender.EnviarPedidoAsync(pedido);

            return CreatedAtAction(nameof(GetOrder), new { id = pedido.Id }, pedido);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(Guid id, Pedido pedido)
        {
            if (id != pedido.Id)
                return BadRequest();

            _context.Entry(pedido).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PedidoExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
                return NotFound();

            _context.Pedidos.Remove(pedido);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PedidoExists(Guid id)
        {
            return _context.Pedidos.Any(e => e.Id == id);
        }
    }
}
