using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApp.Models
{
    /// <summary>
    /// Componente de hardware (CPU, GPU, RAM, etc.) — entidad principal del catálogo.
    /// </summary>
    public class Componente
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(800)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Tipo / Categoría")]
        public string Tipo { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Marca")]
        public string? Marca { get; set; }

        [Required, Range(0.01, 9999999.99)]
        [Display(Name = "Precio (MXN)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        [Required, Range(0, int.MaxValue)]
        [Display(Name = "Stock")]
        public int Stock { get; set; }

        [Display(Name = "URL de imagen")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Especificaciones técnicas")]
        public string? Especificaciones { get; set; }

        public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
        public DateTime? ActualizadoEn { get; set; }

        // Relación inversa con combos
        public ICollection<ComboComponente> ComboComponentes { get; set; } = new List<ComboComponente>();
    }
}
