namespace EcommerceApp.Models
{
    /// <summary>
    /// Representa una sucursal física de TechParts ERP.
    /// </summary>
    public class Sucursal
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Ciudad { get; set; }
        public string? Estado { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Responsable { get; set; }
        public bool Activa { get; set; } = true;
        public DateTime CreadaEn { get; set; } = DateTime.Now;
    }
}
