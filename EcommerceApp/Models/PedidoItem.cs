using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Ítem individual dentro de un pedido. Snapshot del precio al momento de compra.
    /// </summary>
    public class PedidoItem
    {
        [Key]
        public int Id { get; set; }

        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        [Required, MaxLength(200)]
        public string ProductoNombre { get; set; } = string.Empty;

        /// <summary>"componente" | "combo"</summary>
        [MaxLength(20)]
        public string Tipo { get; set; } = "componente";

        public int ProductoId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }

        public int Cantidad { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        public string? ImageUrl { get; set; }
    }
}
