using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;

namespace EcommerceApp.Controllers
{
    /// <summary>
    /// Generación de reportes PDF y Excel para personal de TechParts ERP.
    /// </summary>
    [Authorize(Roles = "GerenteGeneral,GerenteRegional,GerenteSucursal,Vendedor,Comercial,Admin")]
    public class ReportesController(ApplicationDbContext db) : Controller
    {
        // ─── Panel de Reportes ─────────────────────────────────────────
        public IActionResult Index() => View();

        // ════════════════════════════════════════════════════════════════
        //  COMPONENTES
        // ════════════════════════════════════════════════════════════════

        // GET /Reportes/ComponentesPdf
        public async Task<IActionResult> ComponentesPdf()
        {
            var componentes = await db.Componentes
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToListAsync();

            var pdf = GenerarPdfComponentes(componentes);
            return File(pdf, "application/pdf", $"Componentes_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        // GET /Reportes/ComponentesExcel
        public async Task<IActionResult> ComponentesExcel()
        {
            var componentes = await db.Componentes
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Componentes");

            // Encabezados
            string[] headers = ["ID", "Nombre", "Tipo", "Marca", "Precio", "Stock", "Especificaciones", "Creado En"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6c63ff");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Datos
            for (int r = 0; r < componentes.Count; r++)
            {
                var c = componentes[r];
                int row = r + 2;
                ws.Cell(row, 1).Value = c.Id;
                ws.Cell(row, 2).Value = c.Nombre;
                ws.Cell(row, 3).Value = c.Tipo;
                ws.Cell(row, 4).Value = c.Marca ?? "";
                ws.Cell(row, 5).Value = (double)c.Precio;
                ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 6).Value = c.Stock;
                ws.Cell(row, 7).Value = c.Especificaciones ?? "";
                ws.Cell(row, 8).Value = c.CreadoEn.ToString("dd/MM/yyyy");

                // Zebra striping
                if (r % 2 == 0)
                {
                    var range = ws.Range(row, 1, row, headers.Length);
                    range.Style.Fill.BackgroundColor = XLColor.FromHtml("#f0eeff");
                }
            }

            ws.Columns().AdjustToContents();
            ws.Row(1).Height = 20;

            // Totales al final
            int totalRow = componentes.Count + 3;
            ws.Cell(totalRow, 1).Value = "TOTALES";
            ws.Cell(totalRow, 1).Style.Font.Bold = true;
            ws.Cell(totalRow, 5).FormulaA1 = $"=SUM(E2:E{componentes.Count + 1})";
            ws.Cell(totalRow, 5).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(totalRow, 5).Style.Font.Bold = true;
            ws.Cell(totalRow, 6).FormulaA1 = $"=SUM(F2:F{componentes.Count + 1})";
            ws.Cell(totalRow, 6).Style.Font.Bold = true;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Componentes_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ════════════════════════════════════════════════════════════════
        //  COMBOS
        // ════════════════════════════════════════════════════════════════

        // GET /Reportes/CombosPdf
        public async Task<IActionResult> CombosPdf()
        {
            var combos = await db.Combos
                .Include(c => c.ComboComponentes).ThenInclude(cc => cc.Componente)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var pdf = GenerarPdfCombos(combos);
            return File(pdf, "application/pdf", $"Combos_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        // GET /Reportes/CombosExcel
        public async Task<IActionResult> CombosExcel()
        {
            var combos = await db.Combos
                .Include(c => c.ComboComponentes).ThenInclude(cc => cc.Componente)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Combos");

            string[] headers = ["ID", "Nombre", "Descripción", "Precio Venta", "Stock", "Activo", "Componentes"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#00d9b5");
                cell.Style.Font.FontColor = XLColor.White;
            }

            for (int r = 0; r < combos.Count; r++)
            {
                var c = combos[r];
                int row = r + 2;
                ws.Cell(row, 1).Value = c.Id;
                ws.Cell(row, 2).Value = c.Nombre;
                ws.Cell(row, 3).Value = c.Descripcion ?? "";
                ws.Cell(row, 4).Value = (double)c.PrecioVenta;
                ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 5).Value = c.Stock;
                ws.Cell(row, 6).Value = c.Activo ? "Sí" : "No";
                ws.Cell(row, 7).Value = string.Join(", ", c.ComboComponentes
                    .Select(cc => $"{cc.Componente?.Nombre} x{cc.Cantidad}"));

                if (r % 2 == 0)
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#e6faf8");
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Combos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ════════════════════════════════════════════════════════════════
        //  INVENTARIO CONSOLIDADO
        // ════════════════════════════════════════════════════════════════

        // GET /Reportes/InventarioPdf
        public async Task<IActionResult> InventarioPdf()
        {
            var componentes = await db.Componentes.OrderBy(c => c.Tipo).ThenBy(c => c.Nombre).ToListAsync();
            var pdf = GenerarPdfInventario(componentes);
            return File(pdf, "application/pdf", $"Inventario_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        // GET /Reportes/InventarioExcel
        public async Task<IActionResult> InventarioExcel()
        {
            var componentes = await db.Componentes.OrderBy(c => c.Tipo).ThenBy(c => c.Nombre).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Inventario");

            // Resumen por categoría
            var grupos = componentes.GroupBy(c => c.Tipo).ToList();

            ws.Cell(1, 1).Value = "REPORTE DE INVENTARIO — TechParts ERP";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0d0f1a");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Range(1, 1, 1, 6).Merge();

            ws.Cell(2, 1).Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Range(2, 1, 2, 6).Merge();

            string[] headers = ["Categoría", "Producto", "Marca", "Stock", "Precio Unit.", "Valor Total"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6c63ff");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int currentRow = 5;
            foreach (var grupo in grupos)
            {
                foreach (var comp in grupo)
                {
                    ws.Cell(currentRow, 1).Value = comp.Tipo;
                    ws.Cell(currentRow, 2).Value = comp.Nombre;
                    ws.Cell(currentRow, 3).Value = comp.Marca ?? "";
                    ws.Cell(currentRow, 4).Value = comp.Stock;
                    ws.Cell(currentRow, 5).Value = (double)comp.Precio;
                    ws.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(currentRow, 6).Value = (double)(comp.Stock * comp.Precio);
                    ws.Cell(currentRow, 6).Style.NumberFormat.Format = "$#,##0.00";

                    // Stock crítico (< 5) en rojo
                    if (comp.Stock < 5)
                        ws.Cell(currentRow, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#fee2e2");

                    currentRow++;
                }
            }

            // Fila de totales
            ws.Cell(currentRow + 1, 1).Value = "TOTALES";
            ws.Cell(currentRow + 1, 1).Style.Font.Bold = true;
            ws.Range(currentRow + 1, 1, currentRow + 1, 3).Merge();
            ws.Cell(currentRow + 1, 4).FormulaA1 = $"=SUM(D5:D{currentRow})";
            ws.Cell(currentRow + 1, 4).Style.Font.Bold = true;
            ws.Cell(currentRow + 1, 6).FormulaA1 = $"=SUM(F5:F{currentRow})";
            ws.Cell(currentRow + 1, 6).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(currentRow + 1, 6).Style.Font.Bold = true;
            ws.Range(currentRow + 1, 1, currentRow + 1, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#f0eeff");

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Inventario_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ════════════════════════════════════════════════════════════════
        //  USUARIOS Y ROLES (Solo Gerente General / Admin)
        // ════════════════════════════════════════════════════════════════

        [Authorize(Roles = "GerenteGeneral,Admin")]
        public async Task<IActionResult> UsuariosExcel()
        {
            var usuarios = await db.Users.OrderBy(u => u.NombreCompleto).ToListAsync();
            var userRoles = await db.UserRoles.ToListAsync();
            var roles = await db.Roles.ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Usuarios");

            string[] headers = ["Nombre Completo", "Email", "Roles", "Sucursal ID", "Región ID", "Código Cliente", "Creado En"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6c63ff");
                cell.Style.Font.FontColor = XLColor.White;
            }

            for (int r = 0; r < usuarios.Count; r++)
            {
                var u = usuarios[r];
                int row = r + 2;

                // Obtener roles del usuario
                var userRoleIds = userRoles.Where(ur => ur.UserId == u.Id).Select(ur => ur.RoleId);
                var userRoleNames = roles.Where(ro => userRoleIds.Contains(ro.Id)).Select(ro => ro.Name);

                ws.Cell(row, 1).Value = u.NombreCompleto ?? u.FullName ?? "";
                ws.Cell(row, 2).Value = u.Email ?? "";
                ws.Cell(row, 3).Value = string.Join(", ", userRoleNames);
                ws.Cell(row, 4).Value = u.SucursalId?.ToString() ?? "—";
                ws.Cell(row, 5).Value = u.RegionId?.ToString() ?? "—";
                ws.Cell(row, 6).Value = u.CodigoCliente ?? "—";
                ws.Cell(row, 7).Value = u.CreatedAt.ToString("dd/MM/yyyy");

                if (r % 2 == 0)
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#f5f3ff");
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Usuarios_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ════════════════════════════════════════════════════════════════
        //  PDF Generators (QuestPDF)
        // ════════════════════════════════════════════════════════════════

        private static byte[] GenerarPdfComponentes(List<Componente> items)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().Text("TechParts ERP").FontSize(18).Bold().FontColor(Color.FromHex("6c63ff"));
                                inner.Item().Text("Reporte de Componentes").FontSize(12).FontColor(Color.FromHex("888888"));
                            });
                            row.ConstantItem(150).AlignRight().Column(inner =>
                            {
                                inner.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Color.FromHex("aaaaaa"));
                                inner.Item().Text($"Total: {items.Count} componentes").FontSize(9).Bold();
                            });
                        });
                        col.Item().PaddingTop(5).BorderBottom(2).BorderColor(Color.FromHex("6c63ff"));
                        col.Item().Height(8);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(30);   // ID
                            cols.RelativeColumn(3);    // Nombre
                            cols.RelativeColumn(2);    // Tipo
                            cols.RelativeColumn(2);    // Marca
                            cols.ConstantColumn(70);   // Precio
                            cols.ConstantColumn(45);   // Stock
                        });

                        // Header row
                        static IContainer HeaderCell(IContainer container) =>
                            container.Background(Color.FromHex("6c63ff")).Padding(6).DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(9));

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("ID");
                            h.Cell().Element(HeaderCell).Text("Nombre");
                            h.Cell().Element(HeaderCell).Text("Tipo / Categoría");
                            h.Cell().Element(HeaderCell).Text("Marca");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Precio");
                            h.Cell().Element(HeaderCell).AlignCenter().Text("Stock");
                        });

                        // Data rows
                        bool alternate = false;
                        foreach (var c in items)
                        {
                            var bg = alternate ? Color.FromHex("f0eeff") : Colors.White;
                            var stockColor = c.Stock < 5 ? Color.FromHex("ef4444") : Color.FromHex("059669");

                            table.Cell().Background(bg).Padding(5).Text(c.Id.ToString());
                            table.Cell().Background(bg).Padding(5).Text(c.Nombre).SemiBold();
                            table.Cell().Background(bg).Padding(5).Text(c.Tipo);
                            table.Cell().Background(bg).Padding(5).Text(c.Marca ?? "—");
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"${c.Precio:N2}").SemiBold().FontColor(Color.FromHex("059669"));
                            table.Cell().Background(bg).Padding(5).AlignCenter().Text(c.Stock.ToString()).Bold().FontColor(stockColor);

                            alternate = !alternate;
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text($"TechParts ERP — Reporte de Componentes — {DateTime.Now:yyyy}").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        row.ConstantItem(60).AlignRight().Text(x =>
                        {
                            x.Span("Página ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                            x.CurrentPageNumber().FontSize(7);
                            x.Span(" / ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                            x.TotalPages().FontSize(7);
                        });
                    });
                });
            }).GeneratePdf();
        }

        private static byte[] GenerarPdfCombos(List<Combo> combos)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().Text("TechParts ERP").FontSize(18).Bold().FontColor(Color.FromHex("00d9b5"));
                                inner.Item().Text("Reporte de Combos").FontSize(12).FontColor(Color.FromHex("888888"));
                            });
                            row.ConstantItem(150).AlignRight().Column(inner =>
                            {
                                inner.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Color.FromHex("aaaaaa"));
                                inner.Item().Text($"Total: {combos.Count} combos").FontSize(9).Bold();
                            });
                        });
                        col.Item().PaddingTop(5).BorderBottom(2).BorderColor(Color.FromHex("00d9b5"));
                        col.Item().Height(8);
                    });

                    page.Content().Column(col =>
                    {
                        foreach (var combo in combos)
                        {
                            col.Item().Border(1).BorderColor(Color.FromHex("ddddee")).Padding(10).Column(inner =>
                            {
                                inner.Item().Row(row =>
                                {
                                    row.RelativeItem().Text(combo.Nombre).FontSize(12).Bold().FontColor(Color.FromHex("6c63ff"));
                                    row.ConstantItem(100).AlignRight().Text($"${combo.PrecioVenta:N2}").FontSize(12).Bold().FontColor(Color.FromHex("059669"));
                                });
                                if (!string.IsNullOrEmpty(combo.Descripcion))
                                    inner.Item().PaddingTop(4).Text(combo.Descripcion).FontSize(8).FontColor(Color.FromHex("666666"));

                                inner.Item().PaddingTop(6).Text("Componentes incluidos:").FontSize(8).Bold();
                                foreach (var cc in combo.ComboComponentes)
                                {
                                    inner.Item().PaddingLeft(10).Text($"• {cc.Componente?.Nombre} — x{cc.Cantidad}").FontSize(8);
                                }

                                inner.Item().PaddingTop(4).Row(row =>
                                {
                                    row.RelativeItem().Text($"Stock disponible: {combo.Stock}").FontSize(8).FontColor(combo.Stock < 5 ? Color.FromHex("ef4444") : Color.FromHex("059669"));
                                    row.ConstantItem(80).AlignRight().Text(combo.Activo ? "✓ Activo" : "✗ Inactivo").FontSize(8).FontColor(combo.Activo ? Color.FromHex("059669") : Color.FromHex("ef4444"));
                                });
                            });
                            col.Item().Height(8);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("TechParts ERP — Combos — Página ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        x.CurrentPageNumber().FontSize(7);
                        x.Span(" / ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        x.TotalPages().FontSize(7);
                    });
                });
            }).GeneratePdf();
        }

        private static byte[] GenerarPdfInventario(List<Componente> items)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var valorTotal = items.Sum(c => c.Precio * c.Stock);
            var stockCritico = items.Where(c => c.Stock < 5).ToList();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().Text("TechParts ERP").FontSize(18).Bold().FontColor(Color.FromHex("6c63ff"));
                                inner.Item().Text("Reporte de Inventario Consolidado").FontSize(12).FontColor(Color.FromHex("888888"));
                            });
                            row.ConstantItem(200).AlignRight().Column(inner =>
                            {
                                inner.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Color.FromHex("aaaaaa"));
                                inner.Item().Text($"Valor total del inventario: ${valorTotal:N2}").FontSize(10).Bold().FontColor(Color.FromHex("059669"));
                                inner.Item().Text($"Items con stock crítico (< 5): {stockCritico.Count}").FontSize(9).FontColor(Color.FromHex("ef4444"));
                            });
                        });
                        col.Item().PaddingTop(5).BorderBottom(2).BorderColor(Color.FromHex("6c63ff"));
                        col.Item().Height(10);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);    // Categoría
                            cols.RelativeColumn(3);    // Nombre
                            cols.RelativeColumn(2);    // Marca
                            cols.ConstantColumn(50);   // Stock
                            cols.ConstantColumn(80);   // Precio
                            cols.ConstantColumn(90);   // Valor Total
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.Background(Color.FromHex("0d0f1a")).Padding(6).DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(9));

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Categoría");
                            h.Cell().Element(HeaderCell).Text("Nombre");
                            h.Cell().Element(HeaderCell).Text("Marca");
                            h.Cell().Element(HeaderCell).AlignCenter().Text("Stock");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Precio Unit.");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Valor Total");
                        });

                        bool alt = false;
                        foreach (var c in items)
                        {
                            var bg = c.Stock < 5 ? Color.FromHex("fff1f2") : (alt ? Color.FromHex("f0eeff") : Colors.White);
                            var stockColor = c.Stock < 5 ? Color.FromHex("ef4444") : Color.FromHex("374151");

                            table.Cell().Background(bg).Padding(5).Text(c.Tipo).FontSize(8);
                            table.Cell().Background(bg).Padding(5).Text(c.Nombre).SemiBold();
                            table.Cell().Background(bg).Padding(5).Text(c.Marca ?? "—");
                            table.Cell().Background(bg).Padding(5).AlignCenter().Text(c.Stock.ToString()).Bold().FontColor(stockColor);
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"${c.Precio:N2}");
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"${c.Precio * c.Stock:N2}").SemiBold();

                            alt = !alt;
                        }

                        // Fila total
                        table.Cell().ColumnSpan(5).Background(Color.FromHex("6c63ff")).Padding(6).AlignRight()
                            .Text("VALOR TOTAL DEL INVENTARIO:").FontColor(Colors.White).Bold();
                        table.Cell().Background(Color.FromHex("6c63ff")).Padding(6).AlignRight()
                            .Text($"${valorTotal:N2}").FontColor(Colors.White).Bold().FontSize(11);
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("TechParts ERP — Inventario — Página ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        x.CurrentPageNumber().FontSize(7);
                        x.Span(" / ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        x.TotalPages().FontSize(7);
                    });
                });
            }).GeneratePdf();
        }
    }
}
