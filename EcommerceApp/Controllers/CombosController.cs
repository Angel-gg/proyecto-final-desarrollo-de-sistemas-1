using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    /// <summary>
    /// CRUD de Combos — paquetes de componentes con precio especial.
    /// </summary>
    [Authorize(Roles = "GerenteGeneral,GerenteSucursal,Comercial,Admin")]
    public class CombosController(ApplicationDbContext db) : Controller
    {
        // GET /Combos
        public async Task<IActionResult> Index()
        {
            var combos = await db.Combos
                .Include(c => c.ComboComponentes)
                    .ThenInclude(cc => cc.Componente)
                .OrderBy(c => c.Nombre)
                .ToListAsync();
            return View(combos);
        }

        // GET /Combos/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var combo = await db.Combos
                .Include(c => c.ComboComponentes)
                    .ThenInclude(cc => cc.Componente)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (combo == null) return NotFound();
            return View(combo);
        }

        // GET /Combos/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Componentes = await db.Componentes
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToListAsync();
            return View();
        }

        // POST /Combos/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Combo combo, int[] ComponenteIds, int[] Cantidades)
        {
            // Limpiar validaciones de navegación (no vienen del form)
            ModelState.Remove("ComboComponentes");

            if (!ModelState.IsValid)
            {
                ViewBag.Componentes = await db.Componentes.OrderBy(c => c.Nombre).ToListAsync();
                return View(combo);
            }

            combo.CreadoEn = DateTime.UtcNow;
            db.Combos.Add(combo);
            await db.SaveChangesAsync();

            // Agregar los componentes seleccionados
            for (int i = 0; i < ComponenteIds.Length; i++)
            {
                db.ComboComponentes.Add(new ComboComponente
                {
                    ComboId       = combo.Id,
                    ComponenteId  = ComponenteIds[i],
                    Cantidad      = i < Cantidades.Length ? Cantidades[i] : 1
                });
            }
            await db.SaveChangesAsync();

            TempData["Exito"] = $"Combo \"{combo.Nombre}\" creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Combos/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var combo = await db.Combos
                .Include(c => c.ComboComponentes)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (combo == null) return NotFound();

            ViewBag.Componentes = await db.Componentes
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToListAsync();
            return View(combo);
        }

        // POST /Combos/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Combo combo, int[] ComponenteIds, int[] Cantidades)
        {
            if (id != combo.Id) return NotFound();
            ModelState.Remove("ComboComponentes");

            if (!ModelState.IsValid)
            {
                ViewBag.Componentes = await db.Componentes.OrderBy(c => c.Nombre).ToListAsync();
                return View(combo);
            }

            combo.ActualizadoEn = DateTime.UtcNow;
            db.Combos.Update(combo);

            // Reemplazar componentes
            var existentes = db.ComboComponentes.Where(cc => cc.ComboId == id);
            db.ComboComponentes.RemoveRange(existentes);

            for (int i = 0; i < ComponenteIds.Length; i++)
            {
                db.ComboComponentes.Add(new ComboComponente
                {
                    ComboId       = id,
                    ComponenteId  = ComponenteIds[i],
                    Cantidad      = i < Cantidades.Length ? Cantidades[i] : 1
                });
            }
            await db.SaveChangesAsync();

            TempData["Exito"] = $"Combo \"{combo.Nombre}\" actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Combos/Delete/5
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var combo = await db.Combos
                .Include(c => c.ComboComponentes).ThenInclude(cc => cc.Componente)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (combo == null) return NotFound();
            return View(combo);
        }

        // POST /Combos/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var combo = await db.Combos.FindAsync(id);
            if (combo != null)
            {
                db.Combos.Remove(combo);
                await db.SaveChangesAsync();
                TempData["Exito"] = "Combo eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
