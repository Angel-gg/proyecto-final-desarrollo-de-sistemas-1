using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using System.Text.Json;

namespace EcommerceApp.Controllers
{
    /// <summary>
    /// Tienda web (E-Commerce): Catálogo + Carrito + Checkout + Factura + Mis Pedidos.
    /// </summary>
    public class TiendaController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager) : Controller
    {
        private const string CarritoKey = "carrito_techparts";

        // ─── Catálogo ─────────────────────────────────────────────────────────

        public async Task<IActionResult> Index(string? tipo, string? tab)
        {
            tab = tab ?? "componentes";
            ViewBag.Tab = tab;

            var componentesQuery = db.Componentes.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(tipo))
                componentesQuery = componentesQuery.Where(c => c.Tipo == tipo);
            var componentes = await componentesQuery.OrderBy(c => c.Tipo).ThenBy(c => c.Nombre).ToListAsync();

            var combos = await db.Combos
                .Include(c => c.ComboComponentes).ThenInclude(cc => cc.Componente)
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var tipos = await db.Componentes
                .Select(c => c.Tipo).Distinct().OrderBy(t => t).ToListAsync();

            ViewBag.Tipos      = tipos;
            ViewBag.TipoActual = tipo;
            ViewBag.Combos     = combos;
            ViewBag.CarritoCount = ObtenerCarrito().TotalItems;

            return View(componentes);
        }

        // ─── Carrito ──────────────────────────────────────────────────────────

        [Authorize]
        public IActionResult Carrito()
        {
            var carrito = ObtenerCarrito();
            return View(carrito);
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> AgregarAlCarrito(int id, string tipo = "componente")
        {
            var carrito = ObtenerCarrito();
            var existente = carrito.Items.FirstOrDefault(i => i.Tipo == tipo && i.Id == id);

            if (existente != null)
            {
                existente.Cantidad++;
            }
            else
            {
                if (tipo == "componente")
                {
                    var comp = await db.Componentes.FindAsync(id);
                    if (comp == null) return NotFound();
                    if (comp.Stock == 0)
                    {
                        TempData["Error"] = $"\"{comp.Nombre}\" está agotado.";
                        return RedirectToAction(nameof(Index));
                    }
                    carrito.Items.Add(new CarritoItem
                    {
                        Id = comp.Id, Nombre = comp.Nombre, Precio = comp.Precio,
                        Cantidad = 1, ImageUrl = comp.ImageUrl, Tipo = "componente"
                    });
                }
                else if (tipo == "combo")
                {
                    var combo = await db.Combos.FindAsync(id);
                    if (combo == null) return NotFound();
                    carrito.Items.Add(new CarritoItem
                    {
                        Id = combo.Id, Nombre = combo.Nombre, Precio = combo.PrecioVenta,
                        Cantidad = 1, ImageUrl = combo.ImageUrl, Tipo = "combo"
                    });
                }
            }

            GuardarCarrito(carrito);
            await RegistrarCarritoAbandonado(carrito);
            TempData["Exito"] = "Producto agregado al carrito ✓";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> QuitarDelCarrito(int id, string tipo)
        {
            var carrito = ObtenerCarrito();
            var item = carrito.Items.FirstOrDefault(i => i.Id == id && i.Tipo == tipo);
            if (item != null)
            {
                if (item.Cantidad > 1) item.Cantidad--;
                else carrito.Items.Remove(item);
            }
            GuardarCarrito(carrito);
            await RegistrarCarritoAbandonado(carrito);
            return RedirectToAction(nameof(Carrito));
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> EliminarDelCarrito(int id, string tipo)
        {
            var carrito = ObtenerCarrito();
            carrito.Items.RemoveAll(i => i.Id == id && i.Tipo == tipo);
            GuardarCarrito(carrito);
            await RegistrarCarritoAbandonado(carrito);
            return RedirectToAction(nameof(Carrito));
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> VaciarCarrito()
        {
            HttpContext.Session.Remove(CarritoKey);
            await RegistrarCarritoAbandonado(new CarritoViewModel());
            TempData["Exito"] = "Carrito vaciado.";
            return RedirectToAction(nameof(Carrito));
        }

        [HttpGet]
        public IActionResult CarritoCount()
        {
            var carrito = ObtenerCarrito();
            return Json(new { count = carrito.TotalItems });
        }

        // ─── Checkout ─────────────────────────────────────────────────────────

        [Authorize]
        public IActionResult Checkout()
        {
            var carrito = ObtenerCarrito();
            if (!carrito.Items.Any())
            {
                TempData["Error"] = "Tu carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }
            ViewBag.Carrito = carrito;
            return View();
        }

        [HttpPost, Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarPago(MetodoPago metodoPago, string? notas)
        {
            var carrito = ObtenerCarrito();
            if (!carrito.Items.Any())
            {
                TempData["Error"] = "Tu carrito está vacío.";
                return RedirectToAction(nameof(Carrito));
            }

            var userId = userManager.GetUserId(User)!;

            // Generar número único de pedido
            var fechaHoy   = DateTime.UtcNow;
            var inicioDia  = fechaHoy.Date.ToUniversalTime();
            var finDia     = inicioDia.AddDays(1);
            var countHoy   = await db.Pedidos.CountAsync(p => p.FechaPedido >= inicioDia && p.FechaPedido < finDia);
            var numPedido  = $"TPC-{fechaHoy:yyyyMMdd}-{(countHoy + 1):D4}";

            decimal subtotal = carrito.Total;
            decimal iva      = Math.Round(subtotal * 0.16m, 2);
            decimal total    = subtotal + iva;
            decimal comision = Math.Round(subtotal * 0.05m, 2);

            var pedido = new Pedido
            {
                NumeroPedido = numPedido,
                UserId       = userId,
                FechaPedido  = fechaHoy,
                Subtotal     = subtotal,
                Impuesto     = iva,
                Total        = total,
                Comision     = comision,
                MetodoPago   = metodoPago,
                Estado       = EstadoPedido.Completado,
                Notas        = notas,
                Items        = carrito.Items.Select(i => new PedidoItem
                {
                    ProductoNombre = i.Nombre,
                    Tipo           = i.Tipo,
                    ProductoId     = i.Id,
                    PrecioUnitario = i.Precio,
                    Cantidad       = i.Cantidad,
                    Subtotal       = i.Subtotal,
                    ImageUrl       = i.ImageUrl
                }).ToList()
            };

            db.Pedidos.Add(pedido);

            // Marcar carrito abandonado como convertido
            var uid = userId;
            var carritoAbandonado = await db.CarritosAbandonados
                .FirstOrDefaultAsync(c => c.UserId == uid && !c.Convertido);
            if (carritoAbandonado != null)
            {
                carritoAbandonado.Convertido = true;
                db.CarritosAbandonados.Update(carritoAbandonado);
            }

            await db.SaveChangesAsync();
            HttpContext.Session.Remove(CarritoKey);

            TempData["Exito"] = $"¡Compra completada! Pedido {numPedido}";
            return RedirectToAction(nameof(Factura), new { id = pedido.Id });
        }

        // ─── Factura / Comprobante ─────────────────────────────────────────────

        [Authorize]
        public async Task<IActionResult> Factura(int id)
        {
            var userId = userManager.GetUserId(User);
            bool isStaff = User.IsInRole("GerenteGeneral") || User.IsInRole("Admin") ||
                           User.IsInRole("GerenteSucursal") || User.IsInRole("GerenteRegional");

            var pedido = await db.Pedidos
                .Include(p => p.Items)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id && (p.UserId == userId || isStaff));

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        // ─── Mis Pedidos ──────────────────────────────────────────────────────

        [Authorize]
        public async Task<IActionResult> MisPedidos()
        {
            var userId = userManager.GetUserId(User);
            bool isStaff = User.IsInRole("GerenteGeneral") || User.IsInRole("Admin") ||
                           User.IsInRole("GerenteSucursal") || User.IsInRole("GerenteRegional") ||
                           User.IsInRole("Vendedor") || User.IsInRole("Comercial");

            IQueryable<Pedido> query = db.Pedidos.Include(p => p.Items).Include(p => p.Usuario);
            if (!isStaff)
                query = query.Where(p => p.UserId == userId);

            var pedidos = await query.OrderByDescending(p => p.FechaPedido).ToListAsync();
            ViewBag.IsStaff = isStaff;
            return View(pedidos);
        }

        [HttpPost, Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SolicitarDevolucion(int id)
        {
            var userId = userManager.GetUserId(User);
            bool isStaff = User.IsInRole("GerenteGeneral") || User.IsInRole("Admin");

            var pedido = await db.Pedidos
                .FirstOrDefaultAsync(p => p.Id == id && (p.UserId == userId || isStaff));

            if (pedido == null) return NotFound();
            if (pedido.Estado == EstadoPedido.Devuelto)
            {
                TempData["Error"] = "Este pedido ya fue devuelto.";
            }
            else
            {
                pedido.Estado = EstadoPedido.Devuelto;
                await db.SaveChangesAsync();
                TempData["Exito"] = $"Devolución registrada para el pedido {pedido.NumeroPedido}.";
            }
            return RedirectToAction(nameof(MisPedidos));
        }

        // ─── Helpers privados ─────────────────────────────────────────────────

        private CarritoViewModel ObtenerCarrito()
        {
            var json = HttpContext.Session.GetString(CarritoKey);
            if (string.IsNullOrEmpty(json)) return new CarritoViewModel();
            return JsonSerializer.Deserialize<CarritoViewModel>(json) ?? new CarritoViewModel();
        }

        private void GuardarCarrito(CarritoViewModel carrito)
        {
            var json = JsonSerializer.Serialize(carrito);
            HttpContext.Session.SetString(CarritoKey, json);
        }

        private async Task RegistrarCarritoAbandonado(CarritoViewModel carrito)
        {
            if (!User.Identity?.IsAuthenticated ?? true) return;

            var userId = userManager.GetUserId(User);
            if (userId == null) return;

            var existente = await db.CarritosAbandonados
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.Convertido);

            var itemsJson = JsonSerializer.Serialize(carrito.Items);

            if (existente == null)
            {
                if (carrito.Items.Any())
                {
                    db.CarritosAbandonados.Add(new CarritoAbandonado
                    {
                        UserId               = userId,
                        FechaCreacion        = DateTime.Now,
                        FechaUltimaActividad = DateTime.Now,
                        ItemsJson            = itemsJson,
                        TotalEstimado        = carrito.Total,
                        TotalItems           = carrito.TotalItems,
                        Convertido           = false
                    });
                }
            }
            else
            {
                existente.FechaUltimaActividad = DateTime.Now;
                existente.ItemsJson    = itemsJson;
                existente.TotalEstimado = carrito.Total;
                existente.TotalItems   = carrito.TotalItems;
                db.CarritosAbandonados.Update(existente);
            }

            try { await db.SaveChangesAsync(); }
            catch { /* no bloquear si falla el tracking */ }
        }
    }
}
