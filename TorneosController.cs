using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using GamerZoneAPI.Data;
using GamerZoneAPI.Models;

namespace GamerZoneAPI.Controllers
{
    [ApiController]
    [Route("api/torneos")]
    public class TorneosController : ControllerBase
    {
        private Conexion conexion = new Conexion();

        [HttpPost]
        public IActionResult CrearTorneo([FromBody] TorneoRequest request)
        {
            using (var conn = conexion.GetConnection())
            {
                conn.Open();
                var transaction = conn.BeginTransaction();

                try
                {
                    // =========================
                    // CREAR TORNEO
                    // =========================
                    string torneoQuery = @"INSERT INTO torneos (nombre)
                                           VALUES (@nombre);
                                           SELECT LAST_INSERT_ID();";

                    MySqlCommand cmdTorneo = new MySqlCommand(torneoQuery, conn, transaction);
                    cmdTorneo.Parameters.AddWithValue("@nombre", request.nombre);

                    int idTorneo = Convert.ToInt32(cmdTorneo.ExecuteScalar());

                    // =========================
                    // PARTICIPANTES + PUNTOS
                    // =========================
                    foreach (var p in request.participantes)
                    {
                        int puntos = 0;

                        if (p.posicion == 1)
                            puntos = 10;
                        else if (p.posicion == 2)
                            puntos = 5;
                        else if (p.posicion == 3)
                            puntos = 3;
                        else
                            puntos = 2;

                        // guardar participante
                        string participanteQuery = @"INSERT INTO torneo_participantes
                        (id_torneo, id_cliente, posicion)
                        VALUES (@torneo, @cliente, @posicion)";

                        MySqlCommand cmdPart = new MySqlCommand(participanteQuery, conn, transaction);
                        cmdPart.Parameters.AddWithValue("@torneo", idTorneo);
                        cmdPart.Parameters.AddWithValue("@cliente", p.id_cliente);
                        cmdPart.Parameters.AddWithValue("@posicion", p.posicion);
                        cmdPart.ExecuteNonQuery();

                        // =========================
                        // GUARDAR PUNTOS (JUEGO)
                        // =========================
                        string puntosQuery = @"INSERT INTO historial_puntos
                        (id_cliente, tipo, puntos, motivo)
                        VALUES (@cliente, 'JUEGO', @puntos, 'TORNEO')";

                        MySqlCommand cmdPuntos = new MySqlCommand(puntosQuery, conn, transaction);
                        cmdPuntos.Parameters.AddWithValue("@cliente", p.id_cliente);
                        cmdPuntos.Parameters.AddWithValue("@puntos", puntos);
                        cmdPuntos.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    return Ok(new
                    {
                        mensaje = "Torneo registrado con puntos asignados"
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return BadRequest(ex.Message);
                }
            }

        }

            [HttpGet("top10")]
            public IActionResult Top10()
            {
                using (var conn = conexion.GetConnection())
                {
                    conn.Open();

                    string query = @"
        SELECT c.nombre, SUM(h.puntos) AS total_puntos
        FROM historial_puntos h
        INNER JOIN clientes c ON h.id_cliente = c.id_cliente
        WHERE h.tipo = 'JUEGO'
        GROUP BY h.id_cliente
        ORDER BY total_puntos DESC
        LIMIT 10";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    var reader = cmd.ExecuteReader();

                    List<object> lista = new List<object>();

                    while (reader.Read())
                    {
                        lista.Add(new
                        {
                            nombre = reader["nombre"].ToString(),
                            puntos = reader["total_puntos"]
                        });
                    }

                    return Ok(lista);
                }
            }
        }
    }
