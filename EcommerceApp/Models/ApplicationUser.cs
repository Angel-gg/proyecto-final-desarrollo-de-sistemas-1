using Microsoft.AspNetCore.Identity;

namespace EcommerceApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        // --- Datos personales ---
        public string? NombreCompleto { get; set; }

        // Mantener FullName para compatibilidad con datos existentes
        public string? FullName { get; set; }

        // --- Jerarquía organizacional (nullable según el rol) ---
        // Solo para GerenteRegional: indica qué región supervisa
        public int? RegionId { get; set; }

        // Solo para GerenteSucursal y Vendedor: sucursal a la que pertenecen
        public int? SucursalId { get; set; }

        // Código único alfanumérico para autocompletar en el POS (solo Clientes)
        public string? CodigoCliente { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

