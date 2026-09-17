using Microsoft.AspNetCore.Mvc;

namespace EcommerceApp.Controllers
{
    /// <summary>
    /// Tienda web (E-Commerce) para clientes.
    /// Catálogo de productos filtrado por sucursal seleccionada.
    /// </summary>
    public class TiendaController : Controller
    {
        // Página principal de la tienda — accesible sin login
        public IActionResult Index()
        {
            return View();
        }
    }
}
