using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Tabla de unión entre Combo y Componente.
    /// Indica cuántas unidades de cada componente incluye un combo.
    /// </summary>
    public class ComboComponente
    {
        public int ComboId { get; set; }
        public Combo Combo { get; set; } = null!;

        public int ComponenteId { get; set; }
        public Componente Componente { get; set; } = null!;

        [Range(1, 999)]
        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; } = 1;
    }
}
