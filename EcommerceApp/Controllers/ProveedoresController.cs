using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "Comercial,GerenteGeneral,GerenteRegional,Admin")]
    public class ProveedoresController(ApplicationDbContext db) : Controller
    {
        // GET /Proveedores
        public async Task<IActionResult> Index(string? categoria)
        {
            var query = db.Proveedores.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(categoria))
                query = query.Where(p => p.CategoriaEspecialidad == categoria);

            var categorias = await db.Proveedores
                .Where(p => p.CategoriaEspecialidad != null)
                .Select(p => p.CategoriaEspecialidad!)
                .Distinct().OrderBy(c => c).ToListAsync();

            ViewBag.Categorias = categorias;
            ViewBag.CategoriaActual = categoria;

            var proveedores = await query.OrderBy(p => p.Nombre).ToListAsync();
            return View(proveedores);
        }

        // GET /Proveedores/Create
        [Authorize(Roles = "Comercial,GerenteGeneral,Admin")]
        public IActionResult Create() => View(new Proveedor());

        // POST /Proveedores/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Comercial,GerenteGeneral,Admin")]
        public async Task<IActionResult> Create(Proveedor model)
        {
            if (!ModelState.IsValid) return View(model);
            model.CreadoEn = DateTime.Now;
            db.Proveedores.Add(model);
            await db.SaveChangesAsync();
            TempData["Exito"] = $"Proveedor \"{model.Nombre}\" registrado.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Proveedores/Edit/5
        [Authorize(Roles = "Comercial,GerenteGeneral,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var p = await db.Proveedores.FindAsync(id);
            if (p == null) return NotFound();
            return View(p);
        }

        // POST /Proveedores/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Comercial,GerenteGeneral,Admin")]
        public async Task<IActionResult> Edit(int id, Proveedor model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            db.Proveedores.Update(model);
            await db.SaveChangesAsync();
            TempData["Exito"] = "Proveedor actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Proveedores/ToggleActivo/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Comercial,GerenteGeneral,Admin")]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            var p = await db.Proveedores.FindAsync(id);
            if (p == null) return NotFound();
            p.Activo = !p.Activo;
            await db.SaveChangesAsync();
            TempData["Exito"] = $"Proveedor {(p.Activo ? "activado" : "desactivado")}.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Proveedores/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await db.Proveedores.FindAsync(id);
            if (p == null) return NotFound();
            db.Proveedores.Remove(p);
            await db.SaveChangesAsync();
            TempData["Exito"] = "Proveedor eliminado.";
            return RedirectToAction(nameof(Index));
        }
    }
}
