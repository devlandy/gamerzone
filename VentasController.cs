using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using GamerZoneAPI.Data;
using GamerZoneAPI.Models;
namespace GamerZoneAPI.Controllers
{
    [ApiController]
    [Route("api/ventas")]
    public class VentasController : ControllerBase
    {
        private Conexion conexion = new Conexion();

        [HttpPost]
        public IActionResult RegistrarVenta([FromBody] VentaRequest request)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                try
                {
                    decimal total = 0;

                    // Calcular total
                    foreach (var p in request.productos)
                    {
                        total += p.precio * p.cantidad;
                    }

                    // Insertar venta
                    string ventaQuery = @"INSERT INTO ventas 
                    (id_cliente, id_usuario, tipo, numero_orden, nombre_orden, forma_cobro, metodo_pago, total) 
                    VALUES (@cliente, @usuario, 'PRODUCTO', @numero, @nombre, @cobro, @metodo, @total);
                    SELECT LAST_INSERT_ID();";

                    MySqlCommand cmdVenta = new MySqlCommand(ventaQuery, conn, transaction);
                    cmdVenta.Parameters.AddWithValue("@cliente", request.id_cliente);
                    cmdVenta.Parameters.AddWithValue("@usuario", request.id_usuario);
                    cmdVenta.Parameters.AddWithValue("@numero", request.numero_orden);
                    cmdVenta.Parameters.AddWithValue("@nombre", request.nombre_orden);
                    cmdVenta.Parameters.AddWithValue("@cobro", request.forma_cobro);
                    cmdVenta.Parameters.AddWithValue("@metodo", request.metodo_pago);
                    cmdVenta.Parameters.AddWithValue("@total", total);

                    int idVenta = Convert.ToInt32(cmdVenta.ExecuteScalar());

                    // Insertar detalle + descontar inventario
                    foreach (var p in request.productos)
                    {
                        string detalleQuery = @"INSERT INTO detalle_ventas 
                        (id_venta, id_producto, cantidad, precio, subtotal) 
                        VALUES (@venta, @producto, @cantidad, @precio, @subtotal)";

                        MySqlCommand cmdDetalle = new MySqlCommand(detalleQuery, conn, transaction);
                        cmdDetalle.Parameters.AddWithValue("@venta", idVenta);
                        cmdDetalle.Parameters.AddWithValue("@producto", p.id_producto);
                        cmdDetalle.Parameters.AddWithValue("@cantidad", p.cantidad);
                        cmdDetalle.Parameters.AddWithValue("@precio", p.precio);
                        cmdDetalle.Parameters.AddWithValue("@subtotal", p.precio * p.cantidad);
                        cmdDetalle.ExecuteNonQuery();

                        // Descontar stock
                        string updateStock = "UPDATE productos SET stock = stock - @cantidad WHERE id_producto=@id";
                        MySqlCommand cmdStock = new MySqlCommand(updateStock, conn, transaction);
                        cmdStock.Parameters.AddWithValue("@cantidad", p.cantidad);
                        cmdStock.Parameters.AddWithValue("@id", p.id_producto);
                        cmdStock.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    return Ok(new { mensaje = "Venta registrada correctamente", total });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex.Message);
                }
            }
        }



        [HttpPost("consola")]
        public IActionResult VentaConsola([FromBody] VentaConsolaRequest request)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                try
                {
                    decimal total = 0;

                    // =========================
                    // CALCULAR PRECIO
                    // =========================
                    if (request.minutos == 10)
                        total = 2;
                    else if (request.minutos == 30)
                        total = 5;
                    else if (request.minutos == 60)
                        total = 10;
                    else
                        total = request.minutos * 0.20m; // flexible

                    // =========================
                    // INSERTAR VENTA
                    // =========================
                    string ventaQuery = @"INSERT INTO ventas
            (id_cliente, id_usuario, tipo, numero_orden, nombre_orden, forma_cobro, metodo_pago, total, estado)
            VALUES (@cliente, @usuario, 'CONSOLA', '000', 'CONSOLA', @cobro, @metodo, @total, @estado);
            SELECT LAST_INSERT_ID();";

                    MySqlCommand cmdVenta = new MySqlCommand(ventaQuery, conn, transaction);
                    cmdVenta.Parameters.AddWithValue("@cliente", request.id_cliente);
                    cmdVenta.Parameters.AddWithValue("@usuario", request.id_usuario);
                    cmdVenta.Parameters.AddWithValue("@cobro", request.forma_cobro);
                    cmdVenta.Parameters.AddWithValue("@metodo", request.metodo_pago);
                    cmdVenta.Parameters.AddWithValue("@total", total);
                    cmdVenta.Parameters.AddWithValue("@estado", request.forma_cobro == "PENDIENTE" ? "PENDIENTE" : "PAGADO");

                    int idVenta = Convert.ToInt32(cmdVenta.ExecuteScalar());

                    // =========================
                    // INSERTAR CONSOLA
                    // =========================
                    string consolaQuery = @"INSERT INTO venta_consola
            (id_venta, consola, minutos, total, observacion)
            VALUES (@venta, @consola, @minutos, @total, @obs)";

                    MySqlCommand cmdConsola = new MySqlCommand(consolaQuery, conn, transaction);
                    cmdConsola.Parameters.AddWithValue("@venta", idVenta);
                    cmdConsola.Parameters.AddWithValue("@consola", request.consola);
                    cmdConsola.Parameters.AddWithValue("@minutos", request.minutos);
                    cmdConsola.Parameters.AddWithValue("@total", total);
                    cmdConsola.Parameters.AddWithValue("@obs", request.observacion ?? "");

                    cmdConsola.ExecuteNonQuery();

                    transaction.Commit();

                    return Ok(new
                    {
                        mensaje = "Venta de consola registrada",
                        total,
                        consola = request.consola,
                        minutos = request.minutos
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex.Message);
                }
            }
        }


        [HttpGet("pendientes")]
        public IActionResult VentasPendientes()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = @"SELECT 
                        v.id_venta,
                        v.numero_orden,
                        v.tipo,
                        v.total,
                        v.estado,
                        v.fecha,
                        c.nombre AS cliente
                        FROM ventas v
                        JOIN clientes c ON v.id_cliente = c.id_cliente
                        WHERE v.estado = 'PENDIENTE'
                        ORDER BY v.fecha ASC";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                List<object> lista = new List<object>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        id = reader["id_venta"],
                        numero = reader["numero_orden"],
                        tipo = reader["tipo"],
                        total = reader["total"],
                        estado = reader["estado"],
                        cliente = reader["cliente"],
                        fecha = reader["fecha"]
                    });
                }

                return Ok(lista);
            }
        }

        [HttpPut("pagar/{id}")]
        public IActionResult PagarVenta(int id, [FromBody] string metodo_pago)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                try
                {
                    string query = @"UPDATE ventas 
                             SET estado = 'PAGADO',
                                 metodo_pago = @metodo
                             WHERE id_venta = @id";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@metodo", metodo_pago);
                    cmd.Parameters.AddWithValue("@id", id);

                    int filas = cmd.ExecuteNonQuery();

                    if (filas == 0)
                    {
                        return NotFound("Venta no encontrada");
                    }

                    return Ok(new
                    {
                        mensaje = "Venta pagada correctamente",
                        id_venta = id,
                        metodo_pago
                    });
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }


        [HttpPost("combo")]
        public IActionResult VentaCombo([FromBody] ComboRequest request)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                try
                {
                    decimal total = 0;
                    int minutos = 0;

                    // =========================
                    // DEFINIR COMBO
                    // =========================
                    if (request.combo == 1)
                    {
                        total = 50;
                        minutos = 120;
                    }
                    else if (request.combo == 2)
                    {
                        total = 70;
                        minutos = 60;
                    }
                    else if (request.combo == 3)
                    {
                        total = 85;
                        minutos = 60;
                    }

                    // =========================
                    // INSERTAR VENTA
                    // =========================
                    string ventaQuery = @"INSERT INTO ventas
            (id_cliente, id_usuario, tipo, numero_orden, nombre_orden, forma_cobro, metodo_pago, total, estado)
            VALUES (@cliente, @usuario, 'COMBO', '000', 'COMBO', 'CANCELADO', 'EFECTIVO', @total, 'PAGADO');
            SELECT LAST_INSERT_ID();";

                    MySqlCommand cmdVenta = new MySqlCommand(ventaQuery, conn, transaction);
                    cmdVenta.Parameters.AddWithValue("@cliente", request.id_cliente);
                    cmdVenta.Parameters.AddWithValue("@usuario", request.id_usuario);
                    cmdVenta.Parameters.AddWithValue("@total", total);

                    int idVenta = Convert.ToInt32(cmdVenta.ExecuteScalar());

                    // =========================
                    // INSERTAR BEBIDAS
                    // =========================
                    foreach (var bebida in request.bebidas_ids)
                    {
                        string detalle = @"INSERT INTO detalle_ventas
                (id_venta, id_producto, cantidad, precio, subtotal)
                VALUES (@venta, @producto, 1, 0, 0)";

                        MySqlCommand cmdDetalle = new MySqlCommand(detalle, conn, transaction);
                        cmdDetalle.Parameters.AddWithValue("@venta", idVenta);
                        cmdDetalle.Parameters.AddWithValue("@producto", bebida);
                        cmdDetalle.ExecuteNonQuery();
                    }

                    // =========================
                    // INSERTAR CONSOLA
                    // =========================
                    string consolaQuery = @"INSERT INTO venta_consola
            (id_venta, consola, minutos, total)
            VALUES (@venta, 'COMBO', @minutos, 0)";

                    MySqlCommand cmdConsola = new MySqlCommand(consolaQuery, conn, transaction);
                    cmdConsola.Parameters.AddWithValue("@venta", idVenta);
                    cmdConsola.Parameters.AddWithValue("@minutos", minutos);
                    cmdConsola.ExecuteNonQuery();

                    transaction.Commit();

                    return Ok(new
                    {
                        mensaje = "Combo registrado",
                        total,
                        combo = request.combo
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex.Message);
                }
            }
        }


            [HttpGet("{id}")]
            public IActionResult ObtenerVenta(int id)
            {
                using (var conn = conexion.GetConnection())
                {
                    conn.Open();

                    // =============================
                    // DATOS GENERALES DE LA VENTA
                    // =============================
                    string ventaQuery = @"
        SELECT v.id_venta, v.numero_orden, v.total, v.forma_cobro, v.fecha,
               c.nombre AS cliente, u.nombre AS usuario
        FROM ventas v
        LEFT JOIN clientes c ON v.id_cliente = c.id_cliente
        LEFT JOIN usuarios u ON v.id_usuario = u.id_usuario
        WHERE v.id_venta = @id";

                    MySqlCommand cmdVenta = new MySqlCommand(ventaQuery, conn);
                    cmdVenta.Parameters.AddWithValue("@id", id);

                    var readerVenta = cmdVenta.ExecuteReader();

                    if (!readerVenta.Read())
                        return NotFound("Venta no encontrada");

                    var venta = new
                    {
                        id = readerVenta["id_venta"],
                        numero_orden = readerVenta["numero_orden"],
                        cliente = readerVenta["cliente"],
                        usuario = readerVenta["usuario"],
                        total = readerVenta["total"],
                        estado = readerVenta["forma_cobro"],
                        fecha = readerVenta["fecha"]
                    };

                    readerVenta.Close();

                    // =============================
                    // DETALLE DE PRODUCTOS
                    // =============================
                    string detalleQuery = @"
        SELECT p.nombre, d.cantidad, d.precio, (d.cantidad * d.precio) AS subtotal
        FROM detalle_ventas d
        JOIN productos p ON d.id_producto = p.id_producto
        WHERE d.id_venta = @id";

                    MySqlCommand cmdDetalle = new MySqlCommand(detalleQuery, conn);
                    cmdDetalle.Parameters.AddWithValue("@id", id);

                    var readerDetalle = cmdDetalle.ExecuteReader();

                    List<object> productos = new List<object>();

                    while (readerDetalle.Read())
                    {
                        productos.Add(new
                        {
                            nombre = readerDetalle["nombre"],
                            cantidad = readerDetalle["cantidad"],
                            precio = readerDetalle["precio"],
                            subtotal = readerDetalle["subtotal"]
                        });
                    }

                    readerDetalle.Close();

                    // =============================
                    // CONSOLA (SI EXISTE)
                    // =============================
                    string consolaQuery = @"
        SELECT consola, minutos, total
        FROM venta_consola
        WHERE id_venta = @id";

                    MySqlCommand cmdConsola = new MySqlCommand(consolaQuery, conn);
                    cmdConsola.Parameters.AddWithValue("@id", id);

                    var readerConsola = cmdConsola.ExecuteReader();

                    object consola = null;

                    if (readerConsola.Read())
                    {
                        consola = new
                        {
                            tipo = readerConsola["consola"],
                            minutos = readerConsola["minutos"],
                            total = readerConsola["total"]
                        };
                    }

                    return Ok(new
                    {
                        venta,
                        productos,
                        consola
                    });
                }
            }

        [HttpPut("{id}")]
        public IActionResult EditarVenta(int id, [FromBody] EditarVentaRequest request)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                // verificar si existe
                string checkQuery = "SELECT COUNT(*) FROM ventas WHERE id_venta = @id";
                MySqlCommand cmdCheck = new MySqlCommand(checkQuery, conn);
                cmdCheck.Parameters.AddWithValue("@id", id);

                int existe = Convert.ToInt32(cmdCheck.ExecuteScalar());

                if (existe == 0)
                    return NotFound("Venta no encontrada");

                // actualizar
                string updateQuery = @"
        UPDATE ventas
        SET forma_cobro = @forma,
            metodo_pago = @metodo,
            observacion = @obs
        WHERE id_venta = @id";

                MySqlCommand cmdUpdate = new MySqlCommand(updateQuery, conn);
                cmdUpdate.Parameters.AddWithValue("@forma", request.forma_cobro);
                cmdUpdate.Parameters.AddWithValue("@metodo", request.metodo_pago);
                cmdUpdate.Parameters.AddWithValue("@obs", request.observacion ?? "");
                cmdUpdate.Parameters.AddWithValue("@id", id);

                cmdUpdate.ExecuteNonQuery();

                return Ok(new
                {
                    mensaje = "Venta actualizada correctamente"
                });
            }
        }
    }
}







     




