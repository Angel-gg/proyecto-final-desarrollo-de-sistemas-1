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
        // ════════════════════════════════════════════════════════════════
        //  FACTURA PDF (por ID de pedido)
        // ════════════════════════════════════════════════════════════════

        public async Task<IActionResult> FacturaPdf(int id)
        {
            var userId  = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            bool staff  = User.IsInRole("GerenteGeneral") || User.IsInRole("Admin") ||
                          User.IsInRole("GerenteSucursal") || User.IsInRole("GerenteRegional");

            var pedido = await db.Pedidos
                .Include(p => p.Items)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id && (p.UserId == userId || staff));

            if (pedido == null) return NotFound();

            var pdf = GenerarPdfFactura(pedido);
            return File(pdf, "application/pdf", $"Factura_{pedido.NumeroPedido}.pdf");
        }

        // ════════════════════════════════════════════════════════════════
        //  VENTAS POR PERÍODO
        // ════════════════════════════════════════════════════════════════

        public async Task<IActionResult> VentasPeriodoPdf(DateTime? desde, DateTime? hasta)
        {
            desde ??= DateTime.Now.AddDays(-30);
            hasta ??= DateTime.Now;
            var pedidos = await db.Pedidos
                .Include(p => p.Items).Include(p => p.Usuario)
                .Where(p => p.FechaPedido.Date >= desde.Value.Date &&
                            p.FechaPedido.Date <= hasta.Value.Date)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();
            var pdf = GenerarPdfVentasPeriodo(pedidos, desde.Value, hasta.Value);
            return File(pdf, "application/pdf", $"Ventas_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.pdf");
        }

        public async Task<IActionResult> VentasPeriodoExcel(DateTime? desde, DateTime? hasta)
        {
            desde ??= DateTime.Now.AddDays(-30);
            hasta ??= DateTime.Now;
            var pedidos = await db.Pedidos
                .Include(p => p.Items).Include(p => p.Usuario)
                .Where(p => p.FechaPedido.Date >= desde.Value.Date &&
                            p.FechaPedido.Date <= hasta.Value.Date)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Ventas por Período");

            ws.Cell(1, 1).Value = "REPORTE DE VENTAS POR PERÍODO — TechParts ERP";
            ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#6c63ff");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Range(1, 1, 1, 8).Merge();

            ws.Cell(2, 1).Value = $"Período: {desde:dd/MM/yyyy} — {hasta:dd/MM/yyyy}  |  Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Range(2, 1, 2, 8).Merge();

            string[] hdrs = ["Nº Pedido", "Fecha", "Hora", "Cliente", "Método Pago", "Subtotal", "IVA", "Total"];
            for (int i = 0; i < hdrs.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = hdrs[i]; cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6c63ff");
                cell.Style.Font.FontColor = XLColor.White;
            }

            for (int r = 0; r < pedidos.Count; r++)
            {
                var p = pedidos[r]; int row = r + 5;
                ws.Cell(row, 1).Value = p.NumeroPedido;
                ws.Cell(row, 2).Value = p.FechaPedido.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = p.FechaPedido.ToString("HH:mm:ss");
                ws.Cell(row, 4).Value = p.Usuario?.NombreCompleto ?? p.Usuario?.Email ?? "—";
                ws.Cell(row, 5).Value = p.MetodoPago.ToString();
                ws.Cell(row, 6).Value = (double)p.Subtotal; ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 7).Value = (double)p.Impuesto; ws.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 8).Value = (double)p.Total;    ws.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";
                if (r % 2 == 0) ws.Range(row, 1, row, hdrs.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#f0eeff");
                if (p.Estado == EstadoPedido.Devuelto) ws.Range(row, 1, row, hdrs.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#fee2e2");
            }

            int totRow = pedidos.Count + 6;
            ws.Cell(totRow, 1).Value = "TOTALES"; ws.Cell(totRow, 1).Style.Font.Bold = true;
            ws.Cell(totRow, 6).Value = (double)pedidos.Sum(p => p.Subtotal); ws.Cell(totRow, 6).Style.NumberFormat.Format = "$#,##0.00"; ws.Cell(totRow, 6).Style.Font.Bold = true;
            ws.Cell(totRow, 7).Value = (double)pedidos.Sum(p => p.Impuesto); ws.Cell(totRow, 7).Style.NumberFormat.Format = "$#,##0.00"; ws.Cell(totRow, 7).Style.Font.Bold = true;
            ws.Cell(totRow, 8).Value = (double)pedidos.Sum(p => p.Total);    ws.Cell(totRow, 8).Style.NumberFormat.Format = "$#,##0.00"; ws.Cell(totRow, 8).Style.Font.Bold = true;
            ws.Range(totRow, 1, totRow, hdrs.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#e0deff");
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream(); wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Ventas_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.xlsx");
        }

        // ════════════════════════════════════════════════════════════════
        //  PRODUCTOS MÁS VENDIDOS
        // ════════════════════════════════════════════════════════════════

        public async Task<IActionResult> MasVendidosPdf()
        {
            var items = await db.PedidoItems
                .GroupBy(i => new { i.ProductoNombre, i.Tipo })
                .Select(g => new { Nombre = g.Key.ProductoNombre, Tipo = g.Key.Tipo,
                    TotalVendido = g.Sum(x => x.Cantidad),
                    TotalIngresos = g.Sum(x => x.Subtotal) })
                .OrderByDescending(x => x.TotalVendido)
                .Take(20)
                .ToListAsync();
            var pdf = GenerarPdfMasVendidos(items.Select(x => (x.Nombre, x.Tipo, x.TotalVendido, x.TotalIngresos)).ToList());
            return File(pdf, "application/pdf", $"MasVendidos_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        public async Task<IActionResult> MasVendidosExcel()
        {
            var items = await db.PedidoItems
                .GroupBy(i => new { i.ProductoNombre, i.Tipo })
                .Select(g => new { Nombre = g.Key.ProductoNombre, Tipo = g.Key.Tipo,
                    TotalVendido = g.Sum(x => x.Cantidad),
                    TotalIngresos = g.Sum(x => x.Subtotal) })
                .OrderByDescending(x => x.TotalVendido)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Más Vendidos");

            ws.Cell(1, 1).Value = $"PRODUCTOS MÁS VENDIDOS — TechParts ERP — {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 13;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#6c63ff");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Range(1, 1, 1, 5).Merge();

            string[] hdrs = ["#", "Producto", "Tipo", "Unidades Vendidas", "Total Ingresos"];
            for (int i = 0; i < hdrs.Length; i++)
            {
                ws.Cell(3, i + 1).Value = hdrs[i];
                ws.Cell(3, i + 1).Style.Font.Bold = true;
                ws.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#6c63ff");
                ws.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
            }

            for (int r = 0; r < items.Count; r++)
            {
                var x = items[r]; int row = r + 4;
                ws.Cell(row, 1).Value = r + 1;
                ws.Cell(row, 2).Value = x.Nombre;
                ws.Cell(row, 3).Value = x.Tipo;
                ws.Cell(row, 4).Value = x.TotalVendido;
                ws.Cell(row, 5).Value = (double)x.TotalIngresos; ws.Cell(row, 5).Style.NumberFormat.Format = "$#,##0.00";
                if (r % 2 == 0) ws.Range(row, 1, row, hdrs.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#f0eeff");
            }
            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream(); wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"MasVendidos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ════════════════════════════════════════════════════════════════
        //  INVENTARIO BAJO (Stock crítico)
        // ════════════════════════════════════════════════════════════════

        public async Task<IActionResult> StockBajoPdf()
        {
            var items = await db.Componentes
                .Where(c => c.Stock <= 10)
                .OrderBy(c => c.Stock)
                .ToListAsync();
            var pdf = GenerarPdfStockBajo(items);
            return File(pdf, "application/pdf", $"StockBajo_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        public async Task<IActionResult> StockBajoExcel()
        {
            var items = await db.Componentes.Where(c => c.Stock <= 10).OrderBy(c => c.Stock).ToListAsync();
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Stock Bajo");

            ws.Cell(1, 1).Value = $"INVENTARIOS CON STOCK BAJO — TechParts ERP — {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#ef4444");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Range(1, 1, 1, 6).Merge();

            string[] hdrs = ["Producto", "Tipo", "Marca", "Stock", "Stock Mínimo", "Precio"];
            for (int i = 0; i < hdrs.Length; i++) {
                ws.Cell(3, i + 1).Value = hdrs[i]; ws.Cell(3, i + 1).Style.Font.Bold = true;
                ws.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#dc2626");
                ws.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
            }
            for (int r = 0; r < items.Count; r++) {
                var c = items[r]; int row = r + 4;
                ws.Cell(row, 1).Value = c.Nombre; ws.Cell(row, 2).Value = c.Tipo;
                ws.Cell(row, 3).Value = c.Marca ?? ""; ws.Cell(row, 4).Value = c.Stock;
                ws.Cell(row, 5).Value = 5; // umbral mínimo sugerido
                ws.Cell(row, 6).Value = (double)c.Precio; ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                var bg = c.Stock == 0 ? XLColor.FromHtml("#fee2e2") : XLColor.FromHtml("#fff7ed");
                ws.Range(row, 1, row, hdrs.Length).Style.Fill.BackgroundColor = bg;
            }
            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream(); wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"StockBajo_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ════════════════════════════════════════════════════════════════
        //  CARRITOS ABANDONADOS
        // ════════════════════════════════════════════════════════════════

        public async Task<IActionResult> CarritosAbandonadosPdf()
        {
            var registros = await db.CarritosAbandonados
                .Include(c => c.Usuario)
                .Where(c => !c.Convertido)
                .OrderByDescending(c => c.FechaUltimaActividad)
                .ToListAsync();
            var pdf = GenerarPdfCarritosAbandonados(registros);
            return File(pdf, "application/pdf", $"CarritosAbandonados_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        public async Task<IActionResult> CarritosAbandonadosExcel()
        {
            var registros = await db.CarritosAbandonados
                .Include(c => c.Usuario)
                .Where(c => !c.Convertido)
                .OrderByDescending(c => c.FechaUltimaActividad)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Carritos Abandonados");

            ws.Cell(1, 1).Value = $"CARRITOS ABANDONADOS — TechParts ERP — {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#f59e0b");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Range(1, 1, 1, 6).Merge();

            string[] hdrs = ["Cliente", "Email", "Fecha Creación", "Última Actividad", "Items", "Total Estimado"];
            for (int i = 0; i < hdrs.Length; i++) {
                ws.Cell(3, i + 1).Value = hdrs[i]; ws.Cell(3, i + 1).Style.Font.Bold = true;
                ws.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#d97706");
                ws.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
            }
            for (int r = 0; r < registros.Count; r++) {
                var c = registros[r]; int row = r + 4;
                ws.Cell(row, 1).Value = c.Usuario?.NombreCompleto ?? "—";
                ws.Cell(row, 2).Value = c.Usuario?.Email ?? "—";
                ws.Cell(row, 3).Value = c.FechaCreacion.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 4).Value = c.FechaUltimaActividad.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 5).Value = c.TotalItems;
                ws.Cell(row, 6).Value = (double)c.TotalEstimado; ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                if (r % 2 == 0) ws.Range(row, 1, row, hdrs.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#fffbeb");
            }
            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream(); wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CarritosAbandonados_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ════════════════════════════════════════════════════════════════
        //  CONCILIACIÓN DE PAGOS Y COMISIONES
        // ════════════════════════════════════════════════════════════════

        [Authorize(Roles = "GerenteGeneral,GerenteRegional,Admin")]
        public async Task<IActionResult> ConciliacionPdf(int? mes, int? anio)
        {
            mes  ??= DateTime.Now.Month;
            anio ??= DateTime.Now.Year;
            var pedidos = await db.Pedidos.Include(p => p.Usuario)
                .Where(p => p.FechaPedido.Month == mes && p.FechaPedido.Year == anio)
                .OrderBy(p => p.MetodoPago).ToListAsync();
            var pdf = GenerarPdfConciliacion(pedidos, mes.Value, anio.Value);
            return File(pdf, "application/pdf", $"Conciliacion_{anio}-{mes:D2}.pdf");
        }

        [Authorize(Roles = "GerenteGeneral,GerenteRegional,Admin")]
        public async Task<IActionResult> ConciliacionExcel(int? mes, int? anio)
        {
            mes  ??= DateTime.Now.Month;
            anio ??= DateTime.Now.Year;
            var pedidos = await db.Pedidos.Include(p => p.Usuario)
                .Where(p => p.FechaPedido.Month == mes && p.FechaPedido.Year == anio)
                .OrderBy(p => p.MetodoPago).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Conciliación");

            var nombreMes = new System.Globalization.CultureInfo("es-MX").DateTimeFormat.GetMonthName(mes.Value);
            ws.Cell(1, 1).Value = $"CONCILIACIÓN DE PAGOS Y COMISIONES — {nombreMes.ToUpper()} {anio} — {DateTime.Now:HH:mm:ss}";
            ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 12;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#6c63ff");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Range(1, 1, 1, 7).Merge();

            // Resumen por método de pago
            var grupos = pedidos.GroupBy(p => p.MetodoPago).ToList();
            ws.Cell(3, 1).Value = "RESUMEN POR MÉTODO DE PAGO"; ws.Cell(3, 1).Style.Font.Bold = true;
            int gr = 4;
            foreach (var g in grupos) {
                ws.Cell(gr, 1).Value = g.Key.ToString();
                ws.Cell(gr, 2).Value = $"{g.Count()} transacciones";
                ws.Cell(gr, 3).Value = (double)g.Sum(p => p.Total); ws.Cell(gr, 3).Style.NumberFormat.Format = "$#,##0.00"; ws.Cell(gr, 3).Style.Font.Bold = true;
                gr++;
            }

            string[] hdrs = ["Nº Pedido", "Fecha", "Hora", "Cliente", "Método", "Total", "Comisión (5%)"];
            int hdrRow = gr + 2;
            for (int i = 0; i < hdrs.Length; i++) {
                ws.Cell(hdrRow, i + 1).Value = hdrs[i]; ws.Cell(hdrRow, i + 1).Style.Font.Bold = true;
                ws.Cell(hdrRow, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#6c63ff");
                ws.Cell(hdrRow, i + 1).Style.Font.FontColor = XLColor.White;
            }
            for (int r = 0; r < pedidos.Count; r++) {
                var p = pedidos[r]; int row = hdrRow + r + 1;
                ws.Cell(row, 1).Value = p.NumeroPedido;
                ws.Cell(row, 2).Value = p.FechaPedido.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = p.FechaPedido.ToString("HH:mm:ss");
                ws.Cell(row, 4).Value = p.Usuario?.NombreCompleto ?? "—";
                ws.Cell(row, 5).Value = p.MetodoPago.ToString();
                ws.Cell(row, 6).Value = (double)p.Total; ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 7).Value = (double)p.Comision; ws.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
                if (r % 2 == 0) ws.Range(row, 1, row, hdrs.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#f5f3ff");
            }
            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream(); wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Conciliacion_{anio}-{mes:D2}.xlsx");
        }

        // ════════════════════════════════════════════════════════════════
        //  DEVOLUCIONES Y REEMBOLSOS
        // ════════════════════════════════════════════════════════════════

        public async Task<IActionResult> DevolucionesPdf()
        {
            var devueltos = await db.Pedidos.Include(p => p.Items).Include(p => p.Usuario)
                .Where(p => p.Estado == EstadoPedido.Devuelto)
                .OrderByDescending(p => p.FechaPedido).ToListAsync();
            var pdf = GenerarPdfDevoluciones(devueltos);
            return File(pdf, "application/pdf", $"Devoluciones_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        public async Task<IActionResult> DevolucionesExcel()
        {
            var devueltos = await db.Pedidos.Include(p => p.Items).Include(p => p.Usuario)
                .Where(p => p.Estado == EstadoPedido.Devuelto)
                .OrderByDescending(p => p.FechaPedido).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Devoluciones");
            ws.Cell(1, 1).Value = $"DEVOLUCIONES Y REEMBOLSOS — TechParts ERP — {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#ef4444");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Range(1, 1, 1, 7).Merge();

            string[] hdrs = ["Nº Pedido", "Fecha Compra", "Hora", "Cliente", "Método Pago", "Total Reembolso", "Items"];
            for (int i = 0; i < hdrs.Length; i++) {
                ws.Cell(3, i + 1).Value = hdrs[i]; ws.Cell(3, i + 1).Style.Font.Bold = true;
                ws.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#dc2626");
                ws.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
            }
            for (int r = 0; r < devueltos.Count; r++) {
                var p = devueltos[r]; int row = r + 4;
                ws.Cell(row, 1).Value = p.NumeroPedido;
                ws.Cell(row, 2).Value = p.FechaPedido.ToString("dd/MM/yyyy");
                ws.Cell(row, 3).Value = p.FechaPedido.ToString("HH:mm:ss");
                ws.Cell(row, 4).Value = p.Usuario?.NombreCompleto ?? "—";
                ws.Cell(row, 5).Value = p.MetodoPago.ToString();
                ws.Cell(row, 6).Value = (double)p.Total; ws.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00"; ws.Cell(row, 6).Style.Font.Bold = true;
                ws.Cell(row, 7).Value = p.Items.Count;
                ws.Range(row, 1, row, hdrs.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff1f2");
            }
            int totRow = devueltos.Count + 5;
            ws.Cell(totRow, 1).Value = "TOTAL REEMBOLSADO"; ws.Cell(totRow, 1).Style.Font.Bold = true;
            ws.Cell(totRow, 6).Value = (double)devueltos.Sum(p => p.Total);
            ws.Cell(totRow, 6).Style.NumberFormat.Format = "$#,##0.00"; ws.Cell(totRow, 6).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream(); wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Devoluciones_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // ════════════════════════════════════════════════════════════════
        //  IMPUESTOS POR MES
        // ════════════════════════════════════════════════════════════════

        [Authorize(Roles = "GerenteGeneral,GerenteRegional,Admin")]
        public async Task<IActionResult> ImpuestosMesPdf(int? mes, int? anio)
        {
            mes  ??= DateTime.Now.Month;
            anio ??= DateTime.Now.Year;
            var pedidos = await db.Pedidos
                .Where(p => p.FechaPedido.Month == mes && p.FechaPedido.Year == anio &&
                            p.Estado == EstadoPedido.Completado)
                .ToListAsync();
            var pdf = GenerarPdfImpuestosMes(pedidos, mes.Value, anio.Value);
            return File(pdf, "application/pdf", $"Impuestos_{anio}-{mes:D2}.pdf");
        }

        [Authorize(Roles = "GerenteGeneral,GerenteRegional,Admin")]
        public async Task<IActionResult> ImpuestosExcel(int? mes, int? anio)
        {
            mes  ??= DateTime.Now.Month;
            anio ??= DateTime.Now.Year;
            var ci = new System.Globalization.CultureInfo("es-MX");

            // Todos los meses del año para comparar
            var todoAnio = await db.Pedidos
                .Where(p => p.FechaPedido.Year == anio && p.Estado == EstadoPedido.Completado)
                .GroupBy(p => p.FechaPedido.Month)
                .Select(g => new { Mes = g.Key, Subtotal = g.Sum(p => p.Subtotal), IVA = g.Sum(p => p.Impuesto), Total = g.Sum(p => p.Total) })
                .OrderBy(x => x.Mes)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Impuestos por Mes");
            ws.Cell(1, 1).Value = $"IMPUESTOS IVA POR MES — AÑO {anio} — Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 13;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0d9488");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Range(1, 1, 1, 5).Merge();

            string[] hdrs = ["Mes", "Subtotal Ventas", "IVA (16%)", "Total con IVA", "% del Año"];
            for (int i = 0; i < hdrs.Length; i++) {
                ws.Cell(3, i + 1).Value = hdrs[i]; ws.Cell(3, i + 1).Style.Font.Bold = true;
                ws.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0d9488");
                ws.Cell(3, i + 1).Style.Font.FontColor = XLColor.White;
            }
            decimal totalAnioIVA = todoAnio.Sum(x => x.IVA);
            for (int r = 0; r < todoAnio.Count; r++) {
                var x = todoAnio[r]; int row = r + 4;
                ws.Cell(row, 1).Value = ci.DateTimeFormat.GetMonthName(x.Mes);
                ws.Cell(row, 2).Value = (double)x.Subtotal; ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 3).Value = (double)x.IVA;     ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00"; ws.Cell(row, 3).Style.Font.Bold = true;
                ws.Cell(row, 4).Value = (double)x.Total;   ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 5).Value = totalAnioIVA > 0 ? (double)Math.Round(x.IVA / totalAnioIVA * 100, 1) : 0;
                ws.Cell(row, 5).Style.NumberFormat.Format = "0.0\"%\"";
                if (x.Mes == mes) ws.Range(row, 1, row, hdrs.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#ccfbf1");
                else if (r % 2 == 0) ws.Range(row, 1, row, hdrs.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#f0fdfa");
            }
            int totRow2 = todoAnio.Count + 5;
            ws.Cell(totRow2, 1).Value = "TOTAL AÑO"; ws.Cell(totRow2, 1).Style.Font.Bold = true;
            ws.Cell(totRow2, 2).Value = (double)todoAnio.Sum(x => x.Subtotal); ws.Cell(totRow2, 2).Style.NumberFormat.Format = "$#,##0.00"; ws.Cell(totRow2, 2).Style.Font.Bold = true;
            ws.Cell(totRow2, 3).Value = (double)totalAnioIVA; ws.Cell(totRow2, 3).Style.NumberFormat.Format = "$#,##0.00"; ws.Cell(totRow2, 3).Style.Font.Bold = true;
            ws.Cell(totRow2, 4).Value = (double)todoAnio.Sum(x => x.Total); ws.Cell(totRow2, 4).Style.NumberFormat.Format = "$#,##0.00"; ws.Cell(totRow2, 4).Style.Font.Bold = true;
            ws.Range(totRow2, 1, totRow2, hdrs.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#99f6e4");
            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream(); wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Impuestos_{anio}.xlsx");
        }

        // ════════════════════════════════════════════════════════════════
        //  GENERADORES PDF — NUEVOS
        // ════════════════════════════════════════════════════════════════

        private static byte[] GenerarPdfFactura(Pedido p)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4); page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(inner =>
                            {
                                inner.Item().Text("⚡ TechParts ERP").FontSize(20).Bold().FontColor(Color.FromHex("6c63ff"));
                                inner.Item().Text("COMPROBANTE DE PAGO — FACTURA").FontSize(11).Bold().FontColor(Color.FromHex("888888"));
                            });
                            row.ConstantItem(160).AlignRight().Column(inner =>
                            {
                                inner.Item().Text(p.NumeroPedido).FontSize(14).Bold();
                                inner.Item().Text($"Fecha: {p.FechaPedido:dd/MM/yyyy}").FontSize(9).FontColor(Color.FromHex("888888"));
                                inner.Item().Text($"Hora: {p.FechaPedido:HH:mm:ss}").FontSize(10).Bold().FontColor(Color.FromHex("00d9b5"));
                                inner.Item().Text($"Estado: {p.Estado}").FontSize(9);
                            });
                        });
                        col.Item().PaddingTop(6).BorderBottom(2).BorderColor(Color.FromHex("6c63ff"));
                        col.Item().Height(10);
                    });

                    page.Content().Column(col =>
                    {
                        // Datos cliente
                        col.Item().Background(Color.FromHex("f8f7ff")).Padding(12).Column(info =>
                        {
                            info.Item().Text("DATOS DEL CLIENTE").FontSize(8).Bold().FontColor(Color.FromHex("6c63ff"));
                            info.Item().PaddingTop(4).Text($"{p.Usuario?.NombreCompleto ?? p.Usuario?.FullName ?? "Cliente"}").FontSize(11).Bold();
                            info.Item().Text($"{p.Usuario?.Email ?? "—"}").FontSize(9).FontColor(Color.FromHex("666666"));
                            info.Item().PaddingTop(4).Text($"Método de pago: {p.MetodoPago}  |  Documento generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(8);
                        });
                        col.Item().Height(12);

                        // Tabla items
                        col.Item().Text("DETALLE DEL PEDIDO").FontSize(9).Bold().FontColor(Color.FromHex("6c63ff"));
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(cols => {
                                cols.ConstantColumn(25); cols.RelativeColumn(4); cols.ConstantColumn(60);
                                cols.ConstantColumn(50); cols.ConstantColumn(80);
                            });
                            static IContainer HCell(IContainer ct) =>
                                ct.Background(Color.FromHex("6c63ff")).Padding(6)
                                  .DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(9));
                            table.Header(h => {
                                h.Cell().Element(HCell).Text("#");
                                h.Cell().Element(HCell).Text("Producto");
                                h.Cell().Element(HCell).AlignRight().Text("P. Unit.");
                                h.Cell().Element(HCell).AlignCenter().Text("Cant.");
                                h.Cell().Element(HCell).AlignRight().Text("Subtotal");
                            });
                            bool alt = false;
                            int n = 1;
                            foreach (var item in p.Items) {
                                var bg = alt ? Color.FromHex("f0eeff") : Colors.White;
                                table.Cell().Background(bg).Padding(5).Text(n.ToString());
                                table.Cell().Background(bg).Padding(5).Text(item.ProductoNombre).SemiBold();
                                table.Cell().Background(bg).Padding(5).AlignRight().Text($"${item.PrecioUnitario:N2}");
                                table.Cell().Background(bg).Padding(5).AlignCenter().Text(item.Cantidad.ToString());
                                table.Cell().Background(bg).Padding(5).AlignRight().Text($"${item.Subtotal:N2}").Bold();
                                alt = !alt; n++;
                            }
                        });

                        // Totales
                        col.Item().PaddingTop(12).AlignRight().Column(tot =>
                        {
                            tot.Item().Row(r => { r.ConstantItem(120).Text("Subtotal:").AlignRight(); r.ConstantItem(100).Text($"${p.Subtotal:N2}").AlignRight(); });
                            tot.Item().Row(r => { r.ConstantItem(120).Text("IVA (16%):").AlignRight(); r.ConstantItem(100).Text($"${p.Impuesto:N2}").AlignRight(); });
                            tot.Item().BorderTop(1).BorderColor(Color.FromHex("6c63ff")).PaddingTop(4)
                               .Row(r => { r.ConstantItem(120).Text("TOTAL:").AlignRight().Bold().FontSize(13); r.ConstantItem(100).Text($"${p.Total:N2}").AlignRight().Bold().FontSize(14).FontColor(Color.FromHex("6c63ff")); });
                        });

                        if (!string.IsNullOrEmpty(p.Notas)) {
                            col.Item().PaddingTop(20).Text($"Notas: {p.Notas}").FontSize(9).Italic().FontColor(Color.FromHex("888888"));
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text($"TechParts ERP · RFC: TEC2024ABC001 · Compra simulada · {DateTime.Now:yyyy}").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        row.ConstantItem(80).AlignRight().Text(x => {
                            x.Span("Pág ").FontSize(7); x.CurrentPageNumber().FontSize(7);
                            x.Span("/").FontSize(7); x.TotalPages().FontSize(7);
                        });
                    });
                });
            }).GeneratePdf();
        }

        private static byte[] GenerarPdfVentasPeriodo(List<Pedido> pedidos, DateTime desde, DateTime hasta)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape()); page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));
                    page.Header().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Column(inner => {
                                inner.Item().Text("TechParts ERP").FontSize(18).Bold().FontColor(Color.FromHex("6c63ff"));
                                inner.Item().Text($"Ventas por Período: {desde:dd/MM/yyyy} — {hasta:dd/MM/yyyy}").FontSize(11).FontColor(Color.FromHex("888888"));
                            });
                            r.ConstantItem(200).AlignRight().Column(inner => {
                                inner.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor(Color.FromHex("aaaaaa"));
                                inner.Item().Text($"Total ventas: ${pedidos.Sum(p => p.Total):N2}").FontSize(10).Bold().FontColor(Color.FromHex("059669"));
                                inner.Item().Text($"Pedidos: {pedidos.Count}").FontSize(9);
                            });
                        });
                        col.Item().PaddingTop(5).BorderBottom(2).BorderColor(Color.FromHex("6c63ff"));
                        col.Item().Height(8);
                    });
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols => {
                            cols.RelativeColumn(2); cols.ConstantColumn(75); cols.ConstantColumn(65);
                            cols.RelativeColumn(2); cols.ConstantColumn(80);
                            cols.ConstantColumn(80); cols.ConstantColumn(80); cols.ConstantColumn(80);
                        });
                        static IContainer HC(IContainer ct) =>
                            ct.Background(Color.FromHex("6c63ff")).Padding(6).DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(9));
                        table.Header(h => {
                            h.Cell().Element(HC).Text("Nº Pedido");
                            h.Cell().Element(HC).Text("Fecha");
                            h.Cell().Element(HC).Text("Hora");
                            h.Cell().Element(HC).Text("Cliente");
                            h.Cell().Element(HC).AlignCenter().Text("Método");
                            h.Cell().Element(HC).AlignRight().Text("Subtotal");
                            h.Cell().Element(HC).AlignRight().Text("IVA");
                            h.Cell().Element(HC).AlignRight().Text("Total");
                        });
                        bool alt = false;
                        foreach (var p in pedidos) {
                            var bg = p.Estado == EstadoPedido.Devuelto ? Color.FromHex("fff1f2") :
                                     (alt ? Color.FromHex("f0eeff") : Colors.White);
                            table.Cell().Background(bg).Padding(4).Text(p.NumeroPedido).FontColor(Color.FromHex("6c63ff"));
                            table.Cell().Background(bg).Padding(4).Text(p.FechaPedido.ToString("dd/MM/yyyy"));
                            table.Cell().Background(bg).Padding(4).Text(p.FechaPedido.ToString("HH:mm:ss")).FontColor(Color.FromHex("00d9b5"));
                            table.Cell().Background(bg).Padding(4).Text(p.Usuario?.NombreCompleto ?? "—");
                            table.Cell().Background(bg).Padding(4).AlignCenter().Text(p.MetodoPago.ToString());
                            table.Cell().Background(bg).Padding(4).AlignRight().Text($"${p.Subtotal:N2}");
                            table.Cell().Background(bg).Padding(4).AlignRight().Text($"${p.Impuesto:N2}");
                            table.Cell().Background(bg).Padding(4).AlignRight().Text($"${p.Total:N2}").Bold();
                            alt = !alt;
                        }
                        table.Cell().ColumnSpan(5).Background(Color.FromHex("0d0f1a")).Padding(6).AlignRight()
                            .Text("TOTALES:").FontColor(Colors.White).Bold();
                        table.Cell().Background(Color.FromHex("0d0f1a")).Padding(6).AlignRight()
                            .Text($"${pedidos.Sum(p => p.Subtotal):N2}").FontColor(Colors.White).Bold();
                        table.Cell().Background(Color.FromHex("0d0f1a")).Padding(6).AlignRight()
                            .Text($"${pedidos.Sum(p => p.Impuesto):N2}").FontColor(Colors.White).Bold();
                        table.Cell().Background(Color.FromHex("6c63ff")).Padding(6).AlignRight()
                            .Text($"${pedidos.Sum(p => p.Total):N2}").FontColor(Colors.White).Bold().FontSize(11);
                    });
                    page.Footer().AlignCenter().Text(x => {
                        x.Span($"TechParts ERP — Ventas {desde:dd/MM} al {hasta:dd/MM/yyyy} — Página ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        x.CurrentPageNumber().FontSize(7); x.Span("/").FontSize(7); x.TotalPages().FontSize(7);
                    });
                });
            }).GeneratePdf();
        }

        private static byte[] GenerarPdfMasVendidos(List<(string Nombre, string Tipo, int Total, decimal Ingresos)> items)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4); page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    page.Header().Column(col =>
                    {
                        col.Item().Text("TechParts ERP").FontSize(18).Bold().FontColor(Color.FromHex("6c63ff"));
                        col.Item().Text("Productos Más Vendidos").FontSize(13).FontColor(Color.FromHex("888888"));
                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor(Color.FromHex("aaaaaa"));
                        col.Item().PaddingTop(5).BorderBottom(2).BorderColor(Color.FromHex("6c63ff"));
                        col.Item().Height(8);
                    });
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols => {
                            cols.ConstantColumn(35); cols.RelativeColumn(4);
                            cols.RelativeColumn(2); cols.ConstantColumn(80); cols.ConstantColumn(100);
                        });
                        static IContainer HC(IContainer ct) =>
                            ct.Background(Color.FromHex("6c63ff")).Padding(7).DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(10));
                        table.Header(h => {
                            h.Cell().Element(HC).AlignCenter().Text("#");
                            h.Cell().Element(HC).Text("Producto");
                            h.Cell().Element(HC).AlignCenter().Text("Tipo");
                            h.Cell().Element(HC).AlignCenter().Text("Unidades");
                            h.Cell().Element(HC).AlignRight().Text("Ingresos");
                        });
                        bool alt = false;
                        int n = 1;
                        foreach (var item in items) {
                            var bg = alt ? Color.FromHex("f0eeff") : Colors.White;
                            var medal = n == 1 ? "🥇" : n == 2 ? "🥈" : n == 3 ? "🥉" : $"{n}";
                            table.Cell().Background(bg).Padding(7).AlignCenter().Text(medal).Bold();
                            table.Cell().Background(bg).Padding(7).Text(item.Nombre).SemiBold();
                            table.Cell().Background(bg).Padding(7).AlignCenter().Text(item.Tipo);
                            table.Cell().Background(bg).Padding(7).AlignCenter().Text(item.Total.ToString()).Bold().FontColor(Color.FromHex("6c63ff"));
                            table.Cell().Background(bg).Padding(7).AlignRight().Text($"${item.Ingresos:N2}").Bold().FontColor(Color.FromHex("059669"));
                            alt = !alt; n++;
                        }
                    });
                    page.Footer().AlignCenter().Text(x => {
                        x.Span($"TechParts ERP — Productos Más Vendidos — Pág ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        x.CurrentPageNumber().FontSize(7); x.Span("/").FontSize(7); x.TotalPages().FontSize(7);
                    });
                });
            }).GeneratePdf();
        }

        private static byte[] GenerarPdfStockBajo(List<Componente> items)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4); page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    page.Header().Column(col =>
                    {
                        col.Item().Text("TechParts ERP").FontSize(18).Bold().FontColor(Color.FromHex("ef4444"));
                        col.Item().Text("⚠ Inventarios con Stock Bajo (≤ 10 unidades)").FontSize(12).FontColor(Color.FromHex("888888"));
                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}  |  {items.Count} productos críticos").FontSize(8).FontColor(Color.FromHex("aaaaaa"));
                        col.Item().PaddingTop(5).BorderBottom(2).BorderColor(Color.FromHex("ef4444"));
                        col.Item().Height(8);
                    });
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols => {
                            cols.RelativeColumn(3); cols.RelativeColumn(2);
                            cols.RelativeColumn(2); cols.ConstantColumn(60); cols.ConstantColumn(90);
                        });
                        static IContainer HC(IContainer ct) =>
                            ct.Background(Color.FromHex("ef4444")).Padding(7).DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(9));
                        table.Header(h => {
                            h.Cell().Element(HC).Text("Producto");
                            h.Cell().Element(HC).Text("Categoría");
                            h.Cell().Element(HC).Text("Marca");
                            h.Cell().Element(HC).AlignCenter().Text("Stock");
                            h.Cell().Element(HC).AlignRight().Text("Precio");
                        });
                        foreach (var item in items) {
                            var bg = item.Stock == 0 ? Color.FromHex("fee2e2") : Color.FromHex("fff7ed");
                            var stockColor = item.Stock == 0 ? Color.FromHex("ef4444") : Color.FromHex("f59e0b");
                            table.Cell().Background(bg).Padding(6).Text(item.Nombre).SemiBold();
                            table.Cell().Background(bg).Padding(6).Text(item.Tipo);
                            table.Cell().Background(bg).Padding(6).Text(item.Marca ?? "—");
                            table.Cell().Background(bg).Padding(6).AlignCenter().Text(item.Stock == 0 ? "AGOTADO" : item.Stock.ToString()).Bold().FontColor(stockColor);
                            table.Cell().Background(bg).Padding(6).AlignRight().Text($"${item.Precio:N2}");
                        }
                    });
                    page.Footer().AlignCenter().Text(x => {
                        x.Span("TechParts ERP — Stock Bajo — Pág ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        x.CurrentPageNumber().FontSize(7); x.Span("/").FontSize(7); x.TotalPages().FontSize(7);
                    });
                });
            }).GeneratePdf();
        }

        private static byte[] GenerarPdfCarritosAbandonados(List<CarritoAbandonado> registros)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4); page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));
                    page.Header().Column(col =>
                    {
                        col.Item().Text("TechParts ERP").FontSize(18).Bold().FontColor(Color.FromHex("f59e0b"));
                        col.Item().Text("🛒 Carritos Abandonados").FontSize(12).FontColor(Color.FromHex("888888"));
                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}  |  Total estimado perdido: ${registros.Sum(r => r.TotalEstimado):N2}").FontSize(8).FontColor(Color.FromHex("aaaaaa"));
                        col.Item().PaddingTop(5).BorderBottom(2).BorderColor(Color.FromHex("f59e0b"));
                        col.Item().Height(8);
                    });
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols => {
                            cols.RelativeColumn(2); cols.ConstantColumn(90);
                            cols.ConstantColumn(90); cols.ConstantColumn(50); cols.ConstantColumn(90);
                        });
                        static IContainer HC(IContainer ct) =>
                            ct.Background(Color.FromHex("d97706")).Padding(6).DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(9));
                        table.Header(h => {
                            h.Cell().Element(HC).Text("Cliente");
                            h.Cell().Element(HC).Text("Fecha Creación");
                            h.Cell().Element(HC).Text("Última Actividad");
                            h.Cell().Element(HC).AlignCenter().Text("Items");
                            h.Cell().Element(HC).AlignRight().Text("Total Est.");
                        });
                        bool alt = false;
                        foreach (var r in registros) {
                            var bg = alt ? Color.FromHex("fffbeb") : Colors.White;
                            table.Cell().Background(bg).Padding(5).Text(r.Usuario?.NombreCompleto ?? r.Usuario?.Email ?? "—");
                            table.Cell().Background(bg).Padding(5).Text(r.FechaCreacion.ToString("dd/MM/yyyy HH:mm"));
                            table.Cell().Background(bg).Padding(5).Text(r.FechaUltimaActividad.ToString("dd/MM/yyyy HH:mm")).FontColor(Color.FromHex("f59e0b"));
                            table.Cell().Background(bg).Padding(5).AlignCenter().Text(r.TotalItems.ToString());
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"${r.TotalEstimado:N2}").Bold();
                            alt = !alt;
                        }
                        table.Cell().ColumnSpan(4).Background(Color.FromHex("d97706")).Padding(6).AlignRight()
                            .Text("TOTAL ESTIMADO PERDIDO:").FontColor(Colors.White).Bold();
                        table.Cell().Background(Color.FromHex("f59e0b")).Padding(6).AlignRight()
                            .Text($"${registros.Sum(r => r.TotalEstimado):N2}").FontColor(Colors.White).Bold().FontSize(11);
                    });
                    page.Footer().AlignCenter().Text(x => {
                        x.Span("TechParts ERP — Carritos Abandonados — Pág ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        x.CurrentPageNumber().FontSize(7); x.Span("/").FontSize(7); x.TotalPages().FontSize(7);
                    });
                });
            }).GeneratePdf();
        }

        private static byte[] GenerarPdfConciliacion(List<Pedido> pedidos, int mes, int anio)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var ci = new System.Globalization.CultureInfo("es-MX");
            var nombreMes = ci.DateTimeFormat.GetMonthName(mes);
            return Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4); page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));
                    page.Header().Column(col =>
                    {
                        col.Item().Text("TechParts ERP").FontSize(18).Bold().FontColor(Color.FromHex("6c63ff"));
                        col.Item().Text($"Conciliación de Pagos y Comisiones — {nombreMes} {anio}").FontSize(12).FontColor(Color.FromHex("888888"));
                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor(Color.FromHex("aaaaaa"));
                        col.Item().PaddingTop(5).BorderBottom(2).BorderColor(Color.FromHex("6c63ff"));
                        col.Item().Height(8);
                    });
                    page.Content().Column(col =>
                    {
                        // Resumen por método
                        col.Item().Text("Resumen por Método de Pago").FontSize(10).Bold().FontColor(Color.FromHex("6c63ff"));
                        col.Item().PaddingTop(4).Table(t =>
                        {
                            t.ColumnsDefinition(cols => { cols.RelativeColumn(2); cols.ConstantColumn(100); cols.ConstantColumn(100); cols.ConstantColumn(100); });
                            static IContainer HC(IContainer ct) =>
                                ct.Background(Color.FromHex("6c63ff")).Padding(5).DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(9));
                            t.Header(h => {
                                h.Cell().Element(HC).Text("Método"); h.Cell().Element(HC).AlignCenter().Text("Transacciones");
                                h.Cell().Element(HC).AlignRight().Text("Total"); h.Cell().Element(HC).AlignRight().Text("Comisiones");
                            });
                            foreach (var g in pedidos.GroupBy(p => p.MetodoPago)) {
                                t.Cell().Padding(5).Text(g.Key.ToString());
                                t.Cell().Padding(5).AlignCenter().Text(g.Count().ToString()).Bold();
                                t.Cell().Padding(5).AlignRight().Text($"${g.Sum(p => p.Total):N2}").Bold().FontColor(Color.FromHex("059669"));
                                t.Cell().Padding(5).AlignRight().Text($"${g.Sum(p => p.Comision):N2}").FontColor(Color.FromHex("6c63ff"));
                            }
                        });
                        col.Item().Height(12);

                        // Detalle completo
                        col.Item().Text("Detalle de Transacciones").FontSize(10).Bold().FontColor(Color.FromHex("6c63ff"));
                        col.Item().PaddingTop(4).Table(t =>
                        {
                            t.ColumnsDefinition(cols => {
                                cols.RelativeColumn(2); cols.ConstantColumn(75); cols.ConstantColumn(65);
                                cols.ConstantColumn(70); cols.ConstantColumn(80); cols.ConstantColumn(80);
                            });
                            static IContainer HC2(IContainer ct) =>
                                ct.Background(Color.FromHex("0d0f1a")).Padding(5).DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(8));
                            t.Header(h => {
                                h.Cell().Element(HC2).Text("Nº Pedido"); h.Cell().Element(HC2).Text("Fecha");
                                h.Cell().Element(HC2).Text("Hora"); h.Cell().Element(HC2).AlignCenter().Text("Método");
                                h.Cell().Element(HC2).AlignRight().Text("Total"); h.Cell().Element(HC2).AlignRight().Text("Comisión");
                            });
                            bool alt = false;
                            foreach (var p in pedidos) {
                                var bg = alt ? Color.FromHex("f5f3ff") : Colors.White;
                                t.Cell().Background(bg).Padding(4).Text(p.NumeroPedido).FontSize(8).FontColor(Color.FromHex("6c63ff"));
                                t.Cell().Background(bg).Padding(4).Text(p.FechaPedido.ToString("dd/MM/yyyy")).FontSize(8);
                                t.Cell().Background(bg).Padding(4).Text(p.FechaPedido.ToString("HH:mm:ss")).FontSize(8).FontColor(Color.FromHex("00d9b5"));
                                t.Cell().Background(bg).Padding(4).AlignCenter().Text(p.MetodoPago.ToString()).FontSize(8);
                                t.Cell().Background(bg).Padding(4).AlignRight().Text($"${p.Total:N2}").FontSize(8).Bold();
                                t.Cell().Background(bg).Padding(4).AlignRight().Text($"${p.Comision:N2}").FontSize(8).FontColor(Color.FromHex("6c63ff"));
                                alt = !alt;
                            }
                            t.Cell().ColumnSpan(4).Background(Color.FromHex("6c63ff")).Padding(5).AlignRight().Text("TOTALES:").FontColor(Colors.White).Bold().FontSize(9);
                            t.Cell().Background(Color.FromHex("6c63ff")).Padding(5).AlignRight().Text($"${pedidos.Sum(p => p.Total):N2}").FontColor(Colors.White).Bold();
                            t.Cell().Background(Color.FromHex("6c63ff")).Padding(5).AlignRight().Text($"${pedidos.Sum(p => p.Comision):N2}").FontColor(Colors.White).Bold();
                        });
                    });
                    page.Footer().AlignCenter().Text(x => {
                        x.Span($"TechParts ERP — Conciliación {nombreMes} {anio} — Pág ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        x.CurrentPageNumber().FontSize(7); x.Span("/").FontSize(7); x.TotalPages().FontSize(7);
                    });
                });
            }).GeneratePdf();
        }

        private static byte[] GenerarPdfDevoluciones(List<Pedido> devueltos)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            return Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4); page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9));
                    page.Header().Column(col =>
                    {
                        col.Item().Text("TechParts ERP").FontSize(18).Bold().FontColor(Color.FromHex("ef4444"));
                        col.Item().Text("↩ Devoluciones y Reembolsos").FontSize(12).FontColor(Color.FromHex("888888"));
                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}  |  Total reembolsado: ${devueltos.Sum(p => p.Total):N2}").FontSize(8).FontColor(Color.FromHex("aaaaaa"));
                        col.Item().PaddingTop(5).BorderBottom(2).BorderColor(Color.FromHex("ef4444"));
                        col.Item().Height(8);
                    });
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols => {
                            cols.RelativeColumn(2); cols.RelativeColumn(2); cols.ConstantColumn(75);
                            cols.ConstantColumn(65); cols.ConstantColumn(80); cols.ConstantColumn(80);
                        });
                        static IContainer HC(IContainer ct) =>
                            ct.Background(Color.FromHex("dc2626")).Padding(6).DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(9));
                        table.Header(h => {
                            h.Cell().Element(HC).Text("Nº Pedido"); h.Cell().Element(HC).Text("Cliente");
                            h.Cell().Element(HC).Text("Fecha"); h.Cell().Element(HC).Text("Hora");
                            h.Cell().Element(HC).AlignCenter().Text("Método"); h.Cell().Element(HC).AlignRight().Text("Reembolso");
                        });
                        bool alt = false;
                        foreach (var p in devueltos) {
                            var bg = alt ? Color.FromHex("fff1f2") : Colors.White;
                            table.Cell().Background(bg).Padding(5).Text(p.NumeroPedido).FontColor(Color.FromHex("ef4444"));
                            table.Cell().Background(bg).Padding(5).Text(p.Usuario?.NombreCompleto ?? "—");
                            table.Cell().Background(bg).Padding(5).Text(p.FechaPedido.ToString("dd/MM/yyyy"));
                            table.Cell().Background(bg).Padding(5).Text(p.FechaPedido.ToString("HH:mm:ss")).FontColor(Color.FromHex("00d9b5"));
                            table.Cell().Background(bg).Padding(5).AlignCenter().Text(p.MetodoPago.ToString());
                            table.Cell().Background(bg).Padding(5).AlignRight().Text($"${p.Total:N2}").Bold().FontColor(Color.FromHex("ef4444"));
                            alt = !alt;
                        }
                        table.Cell().ColumnSpan(5).Background(Color.FromHex("dc2626")).Padding(6).AlignRight()
                            .Text("TOTAL REEMBOLSADO:").FontColor(Colors.White).Bold();
                        table.Cell().Background(Color.FromHex("ef4444")).Padding(6).AlignRight()
                            .Text($"${devueltos.Sum(p => p.Total):N2}").FontColor(Colors.White).Bold().FontSize(11);
                    });
                    page.Footer().AlignCenter().Text(x => {
                        x.Span("TechParts ERP — Devoluciones — Pág ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        x.CurrentPageNumber().FontSize(7); x.Span("/").FontSize(7); x.TotalPages().FontSize(7);
                    });
                });
            }).GeneratePdf();
        }

        private static byte[] GenerarPdfImpuestosMes(List<Pedido> pedidos, int mes, int anio)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var ci = new System.Globalization.CultureInfo("es-MX");
            var nombreMes = ci.DateTimeFormat.GetMonthName(mes);
            decimal totalIVA = pedidos.Sum(p => p.Impuesto);
            decimal totalVentas = pedidos.Sum(p => p.Total);
            return Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A4); page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    page.Header().Column(col =>
                    {
                        col.Item().Text("TechParts ERP").FontSize(18).Bold().FontColor(Color.FromHex("0d9488"));
                        col.Item().Text($"Impuestos IVA — {nombreMes} {anio}").FontSize(13).FontColor(Color.FromHex("888888"));
                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor(Color.FromHex("aaaaaa"));
                        col.Item().PaddingTop(5).BorderBottom(2).BorderColor(Color.FromHex("0d9488"));
                        col.Item().Height(10);
                    });
                    page.Content().Column(col =>
                    {
                        // KPIs
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Background(Color.FromHex("f0fdfa")).Border(1).BorderColor(Color.FromHex("0d9488")).Padding(12).Column(k => {
                                k.Item().Text("Total Ventas del Mes").FontSize(8).FontColor(Color.FromHex("0d9488")).Bold();
                                k.Item().Text($"${totalVentas:N2}").FontSize(16).Bold();
                            });
                            r.ConstantItem(10);
                            r.RelativeItem().Background(Color.FromHex("f0fdfa")).Border(1).BorderColor(Color.FromHex("0d9488")).Padding(12).Column(k => {
                                k.Item().Text("IVA 16% Retenido").FontSize(8).FontColor(Color.FromHex("0d9488")).Bold();
                                k.Item().Text($"${totalIVA:N2}").FontSize(16).Bold().FontColor(Color.FromHex("0d9488"));
                            });
                            r.ConstantItem(10);
                            r.RelativeItem().Background(Color.FromHex("f0fdfa")).Border(1).BorderColor(Color.FromHex("0d9488")).Padding(12).Column(k => {
                                k.Item().Text("Pedidos Completados").FontSize(8).FontColor(Color.FromHex("0d9488")).Bold();
                                k.Item().Text(pedidos.Count.ToString()).FontSize(16).Bold();
                            });
                        });
                        col.Item().Height(14);

                        // Tabla detalle por día
                        col.Item().Text("Detalle por día").FontSize(10).Bold().FontColor(Color.FromHex("0d9488"));
                        col.Item().PaddingTop(6).Table(t =>
                        {
                            t.ColumnsDefinition(cols => {
                                cols.ConstantColumn(80); cols.ConstantColumn(65);
                                cols.ConstantColumn(70); cols.RelativeColumn(); cols.ConstantColumn(90); cols.ConstantColumn(90);
                            });
                            static IContainer HC(IContainer ct) =>
                                ct.Background(Color.FromHex("0d9488")).Padding(6).DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(9));
                            t.Header(h => {
                                h.Cell().Element(HC).Text("Fecha"); h.Cell().Element(HC).Text("Hora");
                                h.Cell().Element(HC).AlignCenter().Text("Pedido"); h.Cell().Element(HC).Text("Cliente");
                                h.Cell().Element(HC).AlignRight().Text("Subtotal"); h.Cell().Element(HC).AlignRight().Text("IVA (16%)");
                            });
                            bool alt = false;
                            foreach (var p in pedidos.OrderBy(p => p.FechaPedido)) {
                                var bg = alt ? Color.FromHex("f0fdfa") : Colors.White;
                                t.Cell().Background(bg).Padding(5).Text(p.FechaPedido.ToString("dd/MM/yyyy"));
                                t.Cell().Background(bg).Padding(5).Text(p.FechaPedido.ToString("HH:mm:ss")).FontColor(Color.FromHex("0d9488"));
                                t.Cell().Background(bg).Padding(5).AlignCenter().Text(p.NumeroPedido).FontSize(8);
                                t.Cell().Background(bg).Padding(5).Text(p.Usuario?.NombreCompleto ?? "—");
                                t.Cell().Background(bg).Padding(5).AlignRight().Text($"${p.Subtotal:N2}");
                                t.Cell().Background(bg).Padding(5).AlignRight().Text($"${p.Impuesto:N2}").Bold().FontColor(Color.FromHex("0d9488"));
                                alt = !alt;
                            }
                            t.Cell().ColumnSpan(4).Background(Color.FromHex("0d9488")).Padding(6).AlignRight()
                                .Text("TOTAL IVA DEL MES:").FontColor(Colors.White).Bold();
                            t.Cell().Background(Color.FromHex("0d9488")).Padding(6).AlignRight()
                                .Text($"${pedidos.Sum(p => p.Subtotal):N2}").FontColor(Colors.White).Bold();
                            t.Cell().Background(Color.FromHex("14b8a6")).Padding(6).AlignRight()
                                .Text($"${totalIVA:N2}").FontColor(Colors.White).Bold().FontSize(11);
                        });
                    });
                    page.Footer().AlignCenter().Text(x => {
                        x.Span($"TechParts ERP — IVA {nombreMes} {anio} — Pág ").FontSize(7).FontColor(Color.FromHex("aaaaaa"));
                        x.CurrentPageNumber().FontSize(7); x.Span("/").FontSize(7); x.TotalPages().FontSize(7);
                    });
                });
            }).GeneratePdf();
        }
    }
}
