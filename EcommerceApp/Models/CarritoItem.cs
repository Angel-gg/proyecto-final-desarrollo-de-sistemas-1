namespace EcommerceApp.Models
{
    /// <summary>
    /// ViewModel para un ítem del carrito de compras (almacenado en Session como JSON).
    /// Puede representar un Componente individual o un Combo.
    /// </summary>
    public class CarritoItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Cantidad { get; set; } = 1;
        public string? ImageUrl { get; set; }
        public string Tipo { get; set; } = "componente"; // "componente" | "combo"

        public decimal Subtotal => Precio * Cantidad;
    }

    /// <summary>ViewModel del carrito completo.</summary>
    public class CarritoViewModel
    {
        public List<CarritoItem> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Subtotal);
        public int TotalItems => Items.Sum(i => i.Cantidad);
    }
}
