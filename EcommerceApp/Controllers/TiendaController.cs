using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using System.Text.Json;

namespace EcommerceApp.Controllers
{
    /// <summary>
    /// Tienda web (E-Commerce) para clientes.
    /// Catálogo de productos + Carrito de compras (Session-based).
    /// </summary>
    public class TiendaController(ApplicationDbContext db) : Controller
    {
        private const string CarritoKey = "carrito_techparts";

        // ─── Catálogo ─────────────────────────────────────────────────────────

        // GET /Tienda  — accesible sin login
        public async Task<IActionResult> Index(string? tipo, string? tab)
        {
            tab = tab ?? "componentes";
            ViewBag.Tab = tab;

            // Componentes
            var componentesQuery = db.Componentes.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(tipo))
                componentesQuery = componentesQuery.Where(c => c.Tipo == tipo);
            var componentes = await componentesQuery.OrderBy(c => c.Tipo).ThenBy(c => c.Nombre).ToListAsync();

            // Combos
            var combos = await db.Combos
                .Include(c => c.ComboComponentes).ThenInclude(cc => cc.Componente)
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            // Tipos/categorías disponibles
            var tipos = await db.Componentes
                .Select(c => c.Tipo).Distinct().OrderBy(t => t).ToListAsync();

            ViewBag.Tipos = tipos;
            ViewBag.TipoActual = tipo;
            ViewBag.Combos = combos;
            ViewBag.CarritoCount = ObtenerCarrito().TotalItems;

            return View(componentes);
        }

        // ─── Carrito ──────────────────────────────────────────────────────────

        // GET /Tienda/Carrito
        [Authorize]
        public IActionResult Carrito()
        {
            var carrito = ObtenerCarrito();
            return View(carrito);
        }

        // POST /Tienda/AgregarAlCarrito
        [HttpPost, Authorize]
        public IActionResult AgregarAlCarrito(int id, string tipo = "componente")
        {
            var carrito = ObtenerCarrito();

            // Buscar si ya existe el ítem
            var itemKey = $"{tipo}_{id}";
            var existente = carrito.Items.FirstOrDefault(i => i.Tipo == tipo && i.Id == id);

            if (existente != null)
            {
                existente.Cantidad++;
            }
            else
            {
                if (tipo == "componente")
                {
                    var comp = db.Componentes.Find(id);
                    if (comp == null) return NotFound();
                    if (comp.Stock == 0)
                    {
                        TempData["Error"] = $"\"{comp.Nombre}\" está agotado.";
                        return RedirectToAction(nameof(Index));
                    }
                    carrito.Items.Add(new CarritoItem
                    {
                        Id       = comp.Id,
                        Nombre   = comp.Nombre,
                        Precio   = comp.Precio,
                        Cantidad = 1,
                        ImageUrl = comp.ImageUrl,
                        Tipo     = "componente"
                    });
                }
                else if (tipo == "combo")
                {
                    var combo = db.Combos.Find(id);
                    if (combo == null) return NotFound();
                    carrito.Items.Add(new CarritoItem
                    {
                        Id       = combo.Id,
                        Nombre   = combo.Nombre,
                        Precio   = combo.PrecioVenta,
                        Cantidad = 1,
                        ImageUrl = combo.ImageUrl,
                        Tipo     = "combo"
                    });
                }
            }

            GuardarCarrito(carrito);
            TempData["Exito"] = "Producto agregado al carrito ✓";
            return RedirectToAction(nameof(Index));
        }

        // POST /Tienda/QuitarDelCarrito
        [HttpPost, Authorize]
        public IActionResult QuitarDelCarrito(int id, string tipo)
        {
            var carrito = ObtenerCarrito();
            var item = carrito.Items.FirstOrDefault(i => i.Id == id && i.Tipo == tipo);
            if (item != null)
            {
                if (item.Cantidad > 1)
                    item.Cantidad--;
                else
                    carrito.Items.Remove(item);
            }
            GuardarCarrito(carrito);
            return RedirectToAction(nameof(Carrito));
        }

        // POST /Tienda/EliminarDelCarrito
        [HttpPost, Authorize]
        public IActionResult EliminarDelCarrito(int id, string tipo)
        {
            var carrito = ObtenerCarrito();
            carrito.Items.RemoveAll(i => i.Id == id && i.Tipo == tipo);
            GuardarCarrito(carrito);
            return RedirectToAction(nameof(Carrito));
        }

        // POST /Tienda/VaciarCarrito
        [HttpPost, Authorize]
        public IActionResult VaciarCarrito()
        {
            HttpContext.Session.Remove(CarritoKey);
            TempData["Exito"] = "Carrito vaciado.";
            return RedirectToAction(nameof(Carrito));
        }

        // ─── Helpers privados ─────────────────────────────────────────────────

        private CarritoViewModel ObtenerCarrito()
        {
            var json = HttpContext.Session.GetString(CarritoKey);
            if (string.IsNullOrEmpty(json))
                return new CarritoViewModel();
            return JsonSerializer.Deserialize<CarritoViewModel>(json) ?? new CarritoViewModel();
        }

        private void GuardarCarrito(CarritoViewModel carrito)
        {
            var json = JsonSerializer.Serialize(carrito);
            HttpContext.Session.SetString(CarritoKey, json);
        }
    }
}
