using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using GamerZoneAPI.Data;

namespace GamerZoneAPI.Controllers
{
    [ApiController]
    [Route("api/factura")]
    public class FacturaController : ControllerBase
    {
        private Conexion conexion = new Conexion();

        [HttpGet("{id}")]
        public IActionResult ObtenerFactura(int id)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                // =========================
                // DATOS PRINCIPALES
                // =========================
                string ventaQuery = @"SELECT v.id_venta, v.fecha, v.total, v.forma_cobro, v.metodo_pago,
                                      c.nombre AS cliente
                                      FROM ventas v
                                      JOIN clientes c ON v.id_cliente = c.id_cliente
                                      WHERE v.id_venta = @id";

                MySqlCommand cmdVenta = new MySqlCommand(ventaQuery, conn);
                cmdVenta.Parameters.AddWithValue("@id", id);

                var readerVenta = cmdVenta.ExecuteReader();

                if (!readerVenta.Read())
                {
                    return NotFound("Venta no encontrada");
                }

                var factura = new
                {
                    id = readerVenta["id_venta"],
                    fecha = readerVenta["fecha"],
                    cliente = readerVenta["cliente"],
                    total = readerVenta["total"],
                    forma_cobro = readerVenta["forma_cobro"],
                    metodo_pago = readerVenta["metodo_pago"],
                    productos = new List<object>()
                };

                readerVenta.Close();

                // =========================
                // DETALLE PRODUCTOS
                // =========================
                string detalleQuery = @"SELECT p.nombre, d.cantidad, d.precio, d.subtotal
                                       FROM detalle_ventas d
                                       JOIN productos p ON d.id_producto = p.id_producto
                                       WHERE d.id_venta = @id";

                MySqlCommand cmdDetalle = new MySqlCommand(detalleQuery, conn);
                cmdDetalle.Parameters.AddWithValue("@id", id);

                var readerDetalle = cmdDetalle.ExecuteReader();

                List<object> listaProductos = new List<object>();

                while (readerDetalle.Read())
                {
                    listaProductos.Add(new
                    {
                        nombre = readerDetalle["nombre"],
                        cantidad = readerDetalle["cantidad"],
                        precio = readerDetalle["precio"],
                        subtotal = readerDetalle["subtotal"]
                    });
                }

                readerDetalle.Close();

                return Ok(new
                {
                    factura.id,
                    factura.fecha,
                    factura.cliente,
                    factura.total,
                    factura.forma_cobro,
                    factura.metodo_pago,
                    productos = listaProductos
                });
            }
        }
    }
}

