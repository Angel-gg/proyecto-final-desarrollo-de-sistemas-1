using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

namespace EcommerceApp.Controllers
{
    [Authorize(Roles = "GerenteGeneral,GerenteRegional,GerenteSucursal,Admin")]
    public class SucursalesController(ApplicationDbContext db) : Controller
    {
        // GET /Sucursales
        public async Task<IActionResult> Index()
        {
            var sucursales = await db.Sucursales.OrderBy(s => s.Nombre).ToListAsync();
            return View(sucursales);
        }

        // GET /Sucursales/Create
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public IActionResult Create() => View(new Sucursal());

        // POST /Sucursales/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> Create(Sucursal model)
        {
            if (!ModelState.IsValid) return View(model);
            model.CreadaEn = DateTime.Now;
            db.Sucursales.Add(model);
            await db.SaveChangesAsync();
            TempData["Exito"] = $"Sucursal \"{model.Nombre}\" creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Sucursales/Edit/5
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var s = await db.Sucursales.FindAsync(id);
            if (s == null) return NotFound();
            return View(s);
        }

        // POST /Sucursales/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> Edit(int id, Sucursal model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            db.Sucursales.Update(model);
            await db.SaveChangesAsync();
            TempData["Exito"] = "Sucursal actualizada.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Sucursales/ToggleActiva/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> ToggleActiva(int id)
        {
            var s = await db.Sucursales.FindAsync(id);
            if (s == null) return NotFound();
            s.Activa = !s.Activa;
            await db.SaveChangesAsync();
            TempData["Exito"] = $"Sucursal {(s.Activa ? "activada" : "desactivada")}.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Sucursales/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var s = await db.Sucursales.FindAsync(id);
            if (s == null) return NotFound();
            db.Sucursales.Remove(s);
            await db.SaveChangesAsync();
            TempData["Exito"] = "Sucursal eliminada.";
            return RedirectToAction(nameof(Index));
        }
    }
}
