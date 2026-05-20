using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using GamerZoneAPI.Data;

namespace GamerZoneAPI.Controllers
{
    [ApiController]
    [Route("api/reportes")]
    public class ReportesController : ControllerBase
    {
        private Conexion conexion = new Conexion();

        // =========================
        // REPORTE VENTAS
        // =========================
        [HttpGet("ventas")]
        public IActionResult Ventas()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = @"SELECT v.id_venta, v.fecha, v.total, v.forma_cobro, v.metodo_pago,
                                 c.nombre AS cliente, u.nombre AS usuario
                                 FROM ventas v
                                 JOIN clientes c ON v.id_cliente = c.id_cliente
                                 JOIN usuarios u ON v.id_usuario = u.id_usuario";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                List<object> lista = new List<object>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        id = reader["id_venta"],
                        fecha = reader["fecha"],
                        total = reader["total"],
                        cliente = reader["cliente"],
                        usuario = reader["usuario"],
                        forma_cobro = reader["forma_cobro"],
                        metodo_pago = reader["metodo_pago"]
                    });
                }

                return Ok(lista);
            }
        }

        // =========================
        // REPORTE INVENTARIO
        // =========================
        [HttpGet("inventario")]
        public IActionResult Inventario()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = @"SELECT nombre, precio_compra, precio_venta, stock,
                                (precio_venta - precio_compra) AS ganancia_unitaria
                                FROM productos";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                List<object> lista = new List<object>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        nombre = reader["nombre"],
                        precio_compra = reader["precio_compra"],
                        precio_venta = reader["precio_venta"],
                        stock = reader["stock"],
                        ganancia = reader["ganancia_unitaria"]
                    });
                }

                return Ok(lista);
            }
        }

        // =========================
        // REPORTE GASTOS
        // =========================
        [HttpGet("gastos")]
        public IActionResult Gastos()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = "SELECT * FROM gastos";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                List<object> lista = new List<object>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        id = reader["id_gasto"],
                        descripcion = reader["descripcion"],
                        monto = reader["monto"],
                        fecha = reader["fecha"]
                    });
                }

                return Ok(lista);
            }
        }

        // =========================
        // REPORTE CIERRE
        // =========================
        [HttpGet("cierre")]
        public IActionResult Cierre()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = "SELECT * FROM cierre_diario";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                List<object> lista = new List<object>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        id = reader["id_cierre"],
                        total_ventas = reader["total_ventas"],
                        total_gastos = reader["total_gastos"],
                        balance = reader["balance"],
                        estado = reader["estado"],
                        fecha = reader["fecha"]
                    });
                }

                return Ok(lista);
            }
        }

        [HttpGet("top-productos")]
        public IActionResult TopProductos()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = @"
        SELECT p.nombre, SUM(d.cantidad) AS total_vendidos
        FROM detalle_ventas d
        JOIN productos p ON d.id_producto = p.id_producto
        GROUP BY p.nombre
        ORDER BY total_vendidos DESC
        LIMIT 10";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                List<object> lista = new List<object>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        producto = reader["nombre"],
                        vendidos = reader["total_vendidos"]
                    });
                }

                return Ok(lista);
            }
        }

        [HttpGet("ventas-mensuales")]
        public IActionResult VentasMensuales()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = @"
        SELECT 
            DATE_FORMAT(fecha, '%Y-%m') AS mes,
            SUM(total) AS total
        FROM ventas
        GROUP BY mes
        ORDER BY mes DESC";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                List<object> lista = new List<object>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        mes = reader["mes"],
                        total = reader["total"]
                    });
                }

                return Ok(lista);
            }
        }
    }
}