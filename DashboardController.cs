using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using GamerZoneAPI.Data;

namespace GamerZoneAPI.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private Conexion conexion = new Conexion();

        [HttpGet]
        public IActionResult ObtenerDashboard()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                // =============================
                // VENTAS DEL DÍA
                // =============================
                string ventasQuery = @"
                SELECT IFNULL(SUM(total),0) 
                FROM ventas 
                WHERE DATE(fecha) = CURDATE()";

                MySqlCommand cmdVentas = new MySqlCommand(ventasQuery, conn);
                decimal ventasDia = Convert.ToDecimal(cmdVentas.ExecuteScalar());

                // =============================
                // PEDIDOS PENDIENTES
                // =============================
                string pendientesQuery = @"
                SELECT COUNT(*) 
                FROM ventas 
                WHERE forma_cobro = 'PENDIENTE'";

                MySqlCommand cmdPendientes = new MySqlCommand(pendientesQuery, conn);
                int pendientes = Convert.ToInt32(cmdPendientes.ExecuteScalar());

                // =============================
                // PRODUCTOS AGOTADOS
                // =============================
                string agotadosQuery = @"
                SELECT COUNT(*) 
                FROM productos 
                WHERE stock = 0";

                MySqlCommand cmdAgotados = new MySqlCommand(agotadosQuery, conn);
                int agotados = Convert.ToInt32(cmdAgotados.ExecuteScalar());

                // =============================
                // PRODUCTOS POR TERMINAR (<5)
                // =============================
                string porTerminarQuery = @"
                SELECT COUNT(*) 
                FROM productos 
                WHERE stock > 0 AND stock <= 5";

                MySqlCommand cmdPorTerminar = new MySqlCommand(porTerminarQuery, conn);
                int porTerminar = Convert.ToInt32(cmdPorTerminar.ExecuteScalar());

                // =============================
                // GASTOS DEL DÍA
                // =============================
                string gastosQuery = @"
                SELECT IFNULL(SUM(monto),0) 
                FROM gastos 
                WHERE DATE(fecha) = CURDATE()";

                MySqlCommand cmdGastos = new MySqlCommand(gastosQuery, conn);
                decimal gastosDia = Convert.ToDecimal(cmdGastos.ExecuteScalar());

                // =============================
                // BALANCE
                // =============================
                decimal balance = ventasDia - gastosDia;

                // =============================
                // CIERRE DEL DÍA
                // =============================
                string cierreQuery = @"
                SELECT COUNT(*) 
                FROM cierre_diario 
                WHERE DATE(fecha) = CURDATE()";

                MySqlCommand cmdCierre = new MySqlCommand(cierreQuery, conn);
                int cierre = Convert.ToInt32(cmdCierre.ExecuteScalar());

                string estadoCierre = cierre > 0 ? "REALIZADO" : "PENDIENTE";

                // =============================
                // CONSOLAS PENDIENTES
                // =============================
                string consolasQuery = @"
                SELECT COUNT(*) 
                FROM ventas 
                WHERE tipo = 'CONSOLA' 
                AND forma_cobro = 'PENDIENTE'";

                MySqlCommand cmdConsolas = new MySqlCommand(consolasQuery, conn);
                int consolasPendientes = Convert.ToInt32(cmdConsolas.ExecuteScalar());

                return Ok(new
                {
                    ventas_dia = ventasDia,
                    pedidos_pendientes = pendientes,
                    productos_agotados = agotados,
                    productos_por_terminar = porTerminar,
                    gastos_dia = gastosDia,
                    balance = balance,
                    cierre_dia = estadoCierre,
                    consolas_pendientes = consolasPendientes
                });
            }
        }

        [HttpPost("cierre")]
        public IActionResult CerrarDia()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                // =============================
                // VALIDAR SI YA EXISTE CIERRE
                // =============================
                string validarQuery = @"
        SELECT COUNT(*) 
        FROM cierre_diario 
        WHERE DATE(fecha) = CURDATE()";

                MySqlCommand cmdValidar = new MySqlCommand(validarQuery, conn);
                int existe = Convert.ToInt32(cmdValidar.ExecuteScalar());

                if (existe > 0)
                {
                    return BadRequest("El cierre de hoy ya fue realizado");
                }

                // =============================
                // VENTAS DEL DÍA
                // =============================
                string ventasQuery = @"
        SELECT IFNULL(SUM(total),0) 
        FROM ventas 
        WHERE DATE(fecha) = CURDATE()";

                MySqlCommand cmdVentas = new MySqlCommand(ventasQuery, conn);
                decimal ventasDia = Convert.ToDecimal(cmdVentas.ExecuteScalar());

                // =============================
                // GASTOS DEL DÍA
                // =============================
                string gastosQuery = @"
        SELECT IFNULL(SUM(monto),0) 
        FROM gastos 
        WHERE DATE(fecha) = CURDATE()";

                MySqlCommand cmdGastos = new MySqlCommand(gastosQuery, conn);
                decimal gastosDia = Convert.ToDecimal(cmdGastos.ExecuteScalar());

                // =============================
                // BALANCE
                // =============================
                decimal balance = ventasDia - gastosDia;

                // =============================
                // GUARDAR CIERRE
                // =============================
                string insertQuery = @"
        INSERT INTO cierre_diario 
        (total_ventas, total_gastos, balance, estado)
        VALUES (@ventas, @gastos, @balance, 'CERRADO')";

                MySqlCommand cmdInsert = new MySqlCommand(insertQuery, conn);
                cmdInsert.Parameters.AddWithValue("@ventas", ventasDia);
                cmdInsert.Parameters.AddWithValue("@gastos", gastosDia);
                cmdInsert.Parameters.AddWithValue("@balance", balance);

                cmdInsert.ExecuteNonQuery();

                return Ok(new
                {
                    mensaje = "Cierre realizado correctamente",
                    ventas = ventasDia,
                    gastos = gastosDia,
                    balance = balance
                });
            }
        }


        [HttpPost("puntos/juego")]
        public IActionResult PuntosJuego(int id_cliente, int puntos)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = @"INSERT INTO historial_puntos
        (id_cliente, tipo, puntos, motivo)
        VALUES (@cliente, 'JUEGO', @puntos, 'MANUAL')";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@cliente", id_cliente);
                cmd.Parameters.AddWithValue("@puntos", puntos);

                cmd.ExecuteNonQuery();

                return Ok(new { mensaje = "Puntos de juego agregados" });
            }
        }

        [HttpPost("puntos/consumo")]
        public IActionResult PuntosConsumo(int id_cliente, decimal monto)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                decimal puntos = monto * 0.05m;

                string query = @"INSERT INTO historial_puntos
        (id_cliente, tipo, puntos, motivo)
        VALUES (@cliente, 'CONSUMO', @puntos, 'COMPRA')";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@cliente", id_cliente);
                cmd.Parameters.AddWithValue("@puntos", puntos);

                cmd.ExecuteNonQuery();

                return Ok(new
                {
                    mensaje = "Puntos de consumo agregados",
                    puntos = puntos
                });
            }
        }

        [HttpPost("venta-rapida")]
        public IActionResult VentaRapida(int id_cliente, decimal total)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = @"INSERT INTO ventas
        (id_cliente, total, forma_cobro, fecha)
        VALUES (@cliente, @total, 'CANCELADO', NOW())";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@cliente", id_cliente);
                cmd.Parameters.AddWithValue("@total", total);

                cmd.ExecuteNonQuery();

                return Ok(new { mensaje = "Venta rápida registrada" });
            }
        }
    }
}