using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    public enum EstadoPedido { Completado, Devuelto, Pendiente }
    public enum MetodoPago  { Efectivo, Tarjeta, Transferencia }

    /// <summary>
    /// Representa un pedido/compra realizado por un cliente en la tienda.
    /// </summary>
    public class Pedido
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Número legible: TPC-YYYYMMDD-NNNN</summary>
        [Required, MaxLength(30)]
        public string NumeroPedido { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? Usuario { get; set; }

        public DateTime FechaPedido { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        /// <summary>IVA 16%</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Impuesto { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public MetodoPago MetodoPago { get; set; } = MetodoPago.Tarjeta;
        public EstadoPedido Estado { get; set; } = EstadoPedido.Completado;

        public string? Notas { get; set; }

        /// <summary>Comisión del vendedor (5% del subtotal)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Comision { get; set; }

        public ICollection<PedidoItem> Items { get; set; } = new List<PedidoItem>();
    }
}
