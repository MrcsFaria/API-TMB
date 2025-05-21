namespace ApiTmb.Models
{
    public class Pedido
    {
        public Guid Id { get; set; } = Guid.NewGuid();  // GUID gerado automaticamente
        public string Cliente { get; set; }
        public string Produto { get; set; }
        public decimal Valor { get; set; }
        public string Status { get; set; } // Configurei para o pedido entrar no banco de dados com o status = Pendente
        public DateTime DataCriacao { get; set; }
    }
}