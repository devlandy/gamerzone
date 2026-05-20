using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using GamerZoneAPI.Data;
using GamerZoneAPI.Models;

// 🔴 NUEVOS USING PARA QR
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace GamerZoneAPI.Controllers
{
    [ApiController]
    [Route("api/clientes")]
    public class ClientesController : ControllerBase
    {
        private Conexion conexion = new Conexion();

        // 🔥 CREAR CLIENTE
        [HttpPost]
        public IActionResult CrearCliente([FromBody] ClienteRequest request)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                // generar código único
                string codigo = "CLI-" + DateTime.Now.Ticks.ToString().Substring(10);

                string query = @"
                INSERT INTO clientes (nombre, telefono, apodo, codigo, estado)
                VALUES (@nombre, @telefono, @apodo, @codigo, 'ACTIVO')";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nombre", request.nombre);
                cmd.Parameters.AddWithValue("@telefono", request.telefono);
                cmd.Parameters.AddWithValue("@apodo", request.apodo);
                cmd.Parameters.AddWithValue("@codigo", codigo);

                cmd.ExecuteNonQuery();

                return Ok(new
                {
                    mensaje = "Cliente creado correctamente",
                    codigo = codigo
                });
            }
        }

        // 🔍 BUSCAR CLIENTE
        [HttpGet("buscar")]
        public IActionResult Buscar(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return BadRequest("Debe ingresar texto para buscar");

            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = @"
                SELECT * FROM clientes
                WHERE nombre LIKE @texto 
                OR telefono LIKE @texto 
                OR apodo LIKE @texto";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@texto", "%" + texto + "%");

                var reader = cmd.ExecuteReader();

                List<object> lista = new List<object>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        codigo = reader["codigo"]?.ToString(),
                        id = reader["id_cliente"],
                        nombre = reader["nombre"]?.ToString(),
                        telefono = reader["telefono"]?.ToString(),
                        apodo = reader["apodo"]?.ToString()
                    });
                }

                return Ok(lista);
            }
        }

        // 📋 LISTAR CLIENTES
        [HttpGet]
        public IActionResult Listar()
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = "SELECT * FROM clientes";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();

                List<object> lista = new List<object>();

                while (reader.Read())
                {
                    lista.Add(new
                    {
                        codigo = reader["codigo"]?.ToString(),
                        id = reader["id_cliente"],
                        nombre = reader["nombre"]?.ToString(),
                        telefono = reader["telefono"]?.ToString(),
                        apodo = reader["apodo"]?.ToString()
                    });
                }

                return Ok(lista);
            }
        }

        // 📱 GENERAR QR
        [HttpGet("qr/{codigo}")]
        public IActionResult GenerarQR(string codigo)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                // validar cliente
                string query = "SELECT COUNT(*) FROM clientes WHERE codigo = @codigo";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@codigo", codigo);

                int existe = Convert.ToInt32(cmd.ExecuteScalar());

                if (existe == 0)
                    return NotFound("Cliente no encontrado");

                // generar QR
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrData = qrGenerator.CreateQrCode(codigo, QRCodeGenerator.ECCLevel.Q))
                using (QRCode qrCode = new QRCode(qrData))
                using (Bitmap qrImage = qrCode.GetGraphic(20))
                using (MemoryStream ms = new MemoryStream())
                {
                    qrImage.Save(ms, ImageFormat.Png);
                    return File(ms.ToArray(), "image/png");
                }
            }
        }
    }
}