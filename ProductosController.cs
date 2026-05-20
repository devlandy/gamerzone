using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using GamerZoneAPI.Data;
using GamerZoneAPI.Models;

namespace GamerZoneAPI.Controllers
{
    [ApiController]
    [Route("api/productos")]
    public class ProductosController : ControllerBase
    {
        private Conexion conexion = new Conexion();

        // =========================
        // CREAR PRODUCTO
        // =========================
        [HttpPost]
        public IActionResult CrearProducto([FromBody] ProductoRequest request)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = @"INSERT INTO productos 
                (nombre, id_categoria, id_subcategoria, precio_compra, precio_venta, stock)
                VALUES (@nombre, @cat, @sub, @compra, @venta, @stock)";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nombre", request.nombre);
                cmd.Parameters.AddWithValue("@cat", request.id_categoria);
                cmd.Parameters.AddWithValue("@sub", request.id_subcategoria);
                cmd.Parameters.AddWithValue("@compra", request.precio_compra);
                cmd.Parameters.AddWithValue("@venta", request.precio_venta);
                cmd.Parameters.AddWithValue("@stock", request.stock);

                cmd.ExecuteNonQuery();

                return Ok(new { mensaje = "Producto creado" });
            }
        }

        // =========================
        // AGREGAR STOCK
        // =========================
        [HttpPut("stock")]
        public IActionResult AgregarStock([FromBody] StockRequest request)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                try
                {
                    // actualizar stock
                    string update = "UPDATE productos SET stock = stock + @cantidad WHERE id_producto=@id";
                    MySqlCommand cmd = new MySqlCommand(update, conn, transaction);
                    cmd.Parameters.AddWithValue("@cantidad", request.cantidad);
                    cmd.Parameters.AddWithValue("@id", request.id_producto);
                    cmd.ExecuteNonQuery();

                    // historial
                    string historial = @"INSERT INTO historial_inventario 
                    (id_producto, tipo_movimiento, cantidad, observacion)
                    VALUES (@id, 'ENTRADA', @cantidad, @obs)";

                    MySqlCommand cmdHist = new MySqlCommand(historial, conn, transaction);
                    cmdHist.Parameters.AddWithValue("@id", request.id_producto);
                    cmdHist.Parameters.AddWithValue("@cantidad", request.cantidad);
                    cmdHist.Parameters.AddWithValue("@obs", request.observacion);
                    cmdHist.ExecuteNonQuery();

                    transaction.Commit();

                    return Ok(new { mensaje = "Stock actualizado" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex.Message);
                }
            }
        }

        // =========================
        // VER INVENTARIO
        // =========================
        [HttpGet]
        public IActionResult ObtenerProductos()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = "SELECT * FROM productos";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                List<object> lista = new List<object>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        id = reader["id_producto"],
                        nombre = reader["nombre"],
                        precio_venta = reader["precio_venta"],
                        stock = reader["stock"]
                    });
                }

                return Ok(lista);
            }
        }

        // =========================
        // ALERTAS
        // =========================
        [HttpGet("alertas")]
        public IActionResult Alertas()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = @"SELECT nombre, stock,
                CASE 
                    WHEN stock = 0 THEN 'AGOTADO'
                    WHEN stock <= 5 THEN 'BAJO'
                    ELSE 'OK'
                END AS estado
                FROM productos";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                List<object> lista = new List<object>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        nombre = reader["nombre"],
                        stock = reader["stock"],
                        estado = reader["estado"]
                    });
                }

                return Ok(lista);
            }
        }
    }
}