using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using GamerZoneAPI.Data;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using System.IO;

namespace GamerZoneAPI.Controllers
{
    [ApiController]
    [Route("api/exportar")]
    public class ExportarController : ControllerBase
    {
        private Conexion conexion = new Conexion();

        [HttpGet("ventas")]
        public IActionResult ExportarVentas()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                var workbook = new XLWorkbook();

                // ===========================
                // 🟢 HOJA 1: VENTAS
                // ===========================

                string queryVentas = @"SELECT v.id_venta, v.fecha, v.total, v.forma_cobro, v.metodo_pago,
                                 c.nombre AS cliente, u.nombre AS usuario
                                 FROM ventas v
                                 JOIN clientes c ON v.id_cliente = c.id_cliente
                                 JOIN usuarios u ON v.id_usuario = u.id_usuario";

                MySqlCommand cmdVentas = new MySqlCommand(queryVentas, conn);
                var readerVentas = cmdVentas.ExecuteReader();

                var wsVentas = workbook.Worksheets.Add("Ventas");

                // Encabezados
                wsVentas.Cell(1, 1).Value = "ID";
                wsVentas.Cell(1, 2).Value = "Fecha";
                wsVentas.Cell(1, 3).Value = "Total";
                wsVentas.Cell(1, 4).Value = "Cliente";
                wsVentas.Cell(1, 5).Value = "Usuario";
                wsVentas.Cell(1, 6).Value = "Forma Cobro";
                wsVentas.Cell(1, 7).Value = "Método Pago";

                // 🎨 ESTILO ENCABEZADO
                var headerVentas = wsVentas.Range("A1:G1");
                headerVentas.Style.Font.Bold = true;
                headerVentas.Style.Fill.BackgroundColor = XLColor.DarkBlue;
                headerVentas.Style.Font.FontColor = XLColor.White;
                headerVentas.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerVentas.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerVentas.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                int rowVentas = 2;

                while (readerVentas.Read())
                {
                    wsVentas.Cell(rowVentas, 1).Value = readerVentas["id_venta"]?.ToString();
                    wsVentas.Cell(rowVentas, 2).Value = readerVentas["fecha"]?.ToString();
                    wsVentas.Cell(rowVentas, 3).Value = Convert.ToDecimal(readerVentas["total"]);
                    wsVentas.Cell(rowVentas, 4).Value = readerVentas["cliente"]?.ToString();
                    wsVentas.Cell(rowVentas, 5).Value = readerVentas["usuario"]?.ToString();
                    wsVentas.Cell(rowVentas, 6).Value = readerVentas["forma_cobro"]?.ToString();
                    wsVentas.Cell(rowVentas, 7).Value = readerVentas["metodo_pago"]?.ToString();

                    rowVentas++;
                }

                readerVentas.Close();

                // 💰 FORMATO MONEDA
                wsVentas.Column(3).Style.NumberFormat.Format = "Q #,##0.00";

                // 🧾 TOTAL
                wsVentas.Cell(rowVentas, 2).Value = "TOTAL:";
                wsVentas.Cell(rowVentas, 2).Style.Font.Bold = true;

                wsVentas.Cell(rowVentas, 3).FormulaA1 = $"SUM(C2:C{rowVentas - 1})";
                wsVentas.Cell(rowVentas, 3).Style.Font.Bold = true;

                // ❄️ congelar encabezado
                wsVentas.SheetView.FreezeRows(1);

                // 📏 ajustar columnas
                wsVentas.Columns().AdjustToContents();

                // ===========================
                // 🔥 HOJA 2: TOP PRODUCTOS
                // ===========================

                string queryProductos = @"
                SELECT p.nombre, SUM(d.cantidad) AS total_vendidos
                FROM detalle_ventas d
                JOIN productos p ON d.id_producto = p.id_producto
                GROUP BY p.nombre
                ORDER BY total_vendidos DESC
                LIMIT 10";

                MySqlCommand cmdProductos = new MySqlCommand(queryProductos, conn);
                var readerProductos = cmdProductos.ExecuteReader();

                var wsProductos = workbook.Worksheets.Add("Top Productos");

                wsProductos.Cell(1, 1).Value = "Producto";
                wsProductos.Cell(1, 2).Value = "Vendidos";

                // 🎨 ESTILO ENCABEZADO
                var headerProd = wsProductos.Range("A1:B1");
                headerProd.Style.Font.Bold = true;
                headerProd.Style.Fill.BackgroundColor = XLColor.DarkGreen;
                headerProd.Style.Font.FontColor = XLColor.White;
                headerProd.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerProd.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerProd.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                int rowProductos = 2;

                while (readerProductos.Read())
                {
                    wsProductos.Cell(rowProductos, 1).Value = readerProductos["nombre"]?.ToString();
                    wsProductos.Cell(rowProductos, 2).Value = Convert.ToInt32(readerProductos["total_vendidos"]);

                    rowProductos++;
                }

                readerProductos.Close();

                // 🔢 FORMATO NUMÉRICO
                wsProductos.Column(2).Style.NumberFormat.Format = "#,##0";

                // 🧾 TOTAL PRODUCTOS
                wsProductos.Cell(rowProductos, 1).Value = "TOTAL:";
                wsProductos.Cell(rowProductos, 1).Style.Font.Bold = true;

                wsProductos.Cell(rowProductos, 2).FormulaA1 = $"SUM(B2:B{rowProductos - 1})";
                wsProductos.Cell(rowProductos, 2).Style.Font.Bold = true;

                // ❄️ congelar encabezado
                wsProductos.SheetView.FreezeRows(1);

                // 📏 ajustar columnas
                wsProductos.Columns().AdjustToContents();

                // ===========================

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "ReporteVentas_PRO.xlsx");
                }
            }
        }
    }
}