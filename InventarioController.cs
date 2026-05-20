using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using GamerZoneAPI.Data;

namespace GamerZoneAPI.Controllers
{
    [ApiController]
    [Route("api/inventario")]
    public class InventarioController : ControllerBase
    {
        private Conexion conexion = new Conexion();

        // AQUÍ VAN LOS MÉTODOS 👇

        [HttpGet("alertas")]
        public IActionResult Alertas()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string agotadosQuery = @"
        SELECT nombre, stock 
        FROM productos 
        WHERE stock = 0";

                MySqlCommand cmdAgotados = new MySqlCommand(agotadosQuery, conn);
                var readerAgotados = cmdAgotados.ExecuteReader();

                List<object> agotados = new List<object>();

                while (readerAgotados.Read())
                {
                    agotados.Add(new
                    {
                        nombre = readerAgotados["nombre"],
                        stock = readerAgotados["stock"]
                    });
                }

                readerAgotados.Close();

                string bajosQuery = @"
        SELECT nombre, stock 
        FROM productos 
        WHERE stock > 0 AND stock <= 5";

                MySqlCommand cmdBajos = new MySqlCommand(bajosQuery, conn);
                var readerBajos = cmdBajos.ExecuteReader();

                List<object> bajos = new List<object>();

                while (readerBajos.Read())
                {
                    bajos.Add(new
                    {
                        nombre = readerBajos["nombre"],
                        stock = readerBajos["stock"]
                    });
                }

                return Ok(new
                {
                    agotados,
                    por_terminar = bajos
                });
            }
        }


        [HttpGet("historial")]
        public IActionResult Historial()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = @"
        SELECT p.nombre, h.tipo_movimiento, h.cantidad, h.observacion, h.fecha
        FROM historial_inventario h
        JOIN productos p ON h.id_producto = p.id_producto
        ORDER BY h.fecha DESC";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                List<object> lista = new List<object>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        producto = reader["nombre"],
                        tipo = reader["tipo_movimiento"],
                        cantidad = reader["cantidad"],
                        observacion = reader["observacion"],
                        fecha = reader["fecha"]
                    });
                }

                return Ok(lista);
            }
        }


    }
}