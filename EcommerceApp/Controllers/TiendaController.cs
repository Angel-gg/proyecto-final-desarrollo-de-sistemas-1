using Microsoft.AspNetCore.Mvc;
using EcommerceApp.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Controllers
{
    /// <summary>
    /// Tienda web (E-Commerce) para clientes.
    /// Catálogo de productos filtrado por sucursal seleccionada.
    /// </summary>
    public class TiendaController(ApplicationDbContext db) : Controller
    {
        // Página principal de la tienda — accesible sin login
        public async Task<IActionResult> Index(string? categoria)
        {
            var productos = await db.Products
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var categorias = productos.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();

            if (!string.IsNullOrEmpty(categoria))
                productos = productos.Where(p => p.Category == categoria).ToList();

            ViewBag.Categorias = categorias;
            ViewBag.CategoriaActual = categoria;
            return View(productos);
        }
    }
}
