using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using GamerZoneAPI.Data;

namespace GamerZoneAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private Conexion conexion = new Conexion();

        [HttpPost("login")]
        public IActionResult Login(string usuario, string password)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();

                string query = "SELECT * FROM usuarios WHERE usuario=@usuario AND password=@password";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@usuario", usuario);
                cmd.Parameters.AddWithValue("@password", password);

                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return Ok(new
                    {
                        mensaje = "Login correcto",
                        usuario = reader["usuario"],
                        rol = reader["rol"]
                    });
                }

                return Unauthorized("Credenciales incorrectas");
            }
        }
    }
}