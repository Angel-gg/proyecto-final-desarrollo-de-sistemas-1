using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    /// <summary>
    /// CRUD de Componentes de hardware — acceso restringido a roles de gestión.
    /// </summary>
    [Authorize(Roles = "GerenteGeneral,GerenteSucursal,Vendedor,Comercial,Admin")]
    public class ComponentesController(ApplicationDbContext db) : Controller
    {
        // GET /Componentes
        public async Task<IActionResult> Index(string? tipo, string? busqueda)
        {
            var query = db.Componentes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(tipo))
                query = query.Where(c => c.Tipo == tipo);

            if (!string.IsNullOrWhiteSpace(busqueda))
                query = query.Where(c => c.Nombre.Contains(busqueda) || (c.Marca != null && c.Marca.Contains(busqueda)));

            var componentes = await query.OrderBy(c => c.Tipo).ThenBy(c => c.Nombre).ToListAsync();
            var tipos = await db.Componentes.Select(c => c.Tipo).Distinct().OrderBy(t => t).ToListAsync();

            ViewBag.Tipos = tipos;
            ViewBag.TipoActual = tipo;
            ViewBag.Busqueda = busqueda;

            return View(componentes);
        }

        // GET /Componentes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var comp = await db.Componentes.FindAsync(id);
            if (comp == null) return NotFound();
            return View(comp);
        }

        // GET /Componentes/Create
        [Authorize(Roles = "GerenteGeneral,GerenteSucursal,Comercial,Admin")]
        public IActionResult Create() => View();

        // POST /Componentes/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,GerenteSucursal,Comercial,Admin")]
        public async Task<IActionResult> Create(Componente componente)
        {
            if (!ModelState.IsValid) return View(componente);
            componente.CreadoEn = DateTime.UtcNow;
            db.Componentes.Add(componente);
            await db.SaveChangesAsync();
            TempData["Exito"] = $"Componente \"{componente.Nombre}\" creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Componentes/Edit/5
        [Authorize(Roles = "GerenteGeneral,GerenteSucursal,Comercial,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var comp = await db.Componentes.FindAsync(id);
            if (comp == null) return NotFound();
            return View(comp);
        }

        // POST /Componentes/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,GerenteSucursal,Comercial,Admin")]
        public async Task<IActionResult> Edit(int id, Componente componente)
        {
            if (id != componente.Id) return NotFound();
            if (!ModelState.IsValid) return View(componente);

            componente.ActualizadoEn = DateTime.UtcNow;
            db.Componentes.Update(componente);
            await db.SaveChangesAsync();
            TempData["Exito"] = $"Componente \"{componente.Nombre}\" actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Componentes/Delete/5
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var comp = await db.Componentes.FindAsync(id);
            if (comp == null) return NotFound();
            return View(comp);
        }

        // POST /Componentes/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comp = await db.Componentes.FindAsync(id);
            if (comp != null)
            {
                db.Componentes.Remove(comp);
                await db.SaveChangesAsync();
                TempData["Exito"] = $"Componente eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
