namespace EcommerceApp.Models
{
    /// <summary>
    /// Representa un proveedor de inventario/componentes de TechParts ERP.
    /// </summary>
    public class Proveedor
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Contacto { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Ciudad { get; set; }
        public string? CategoriaEspecialidad { get; set; } // ej: "GPU", "RAM", "Periféricos"
        public string? SitioWeb { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime CreadoEn { get; set; } = DateTime.Now;
    }
}
