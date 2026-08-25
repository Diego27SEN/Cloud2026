using System.Collections.Generic;

namespace TurnMatch;

/// <summary>
/// Lo que el servidor devuelve al cliente. No es el estado entero: el cliente no
/// necesita la lista de peticiones procesadas, y cuanto menos sepa, menos puede
/// intentar falsear.
/// </summary>
public class MatchView
{
    public string MatchCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TurnNumber { get; set; }

    public string YourPlayerId { get; set; } = string.Empty;
    public string OpponentPlayerId { get; set; } = string.Empty;
    public string CurrentPlayerId { get; set; } = string.Empty;

    /// <summary>Lo calcula el servidor, no el cliente. El cliente sólo lo pinta.</summary>
    public bool IsYourTurn { get; set; }

    public List<TurnRecord> History { get; set; } = new();

    /// <summary>Qué ha pasado con la petición. Ver <see cref="TurnOutcome"/>.</summary>
    public string Outcome { get; set; } = TurnOutcome.Ok;

    /// <summary>Explicación para enseñar en pantalla cuando Outcome no es Ok ni Applied.</summary>
    public string Message { get; set; } = string.Empty;

    public string ServerTimeUtc { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de una petición. Las jugadas rechazadas no son errores: son parte
/// normal del juego, y el cliente necesita distinguirlas para reaccionar bien.
/// Por eso viajan aquí y no como excepción.
/// </summary>
public static class TurnOutcome
{
    /// <summary>Consulta de estado, sin intención de cambiar nada.</summary>
    public const string Ok = "ok";

    /// <summary>El turno se aplicó y la partida avanzó.</summary>
    public const string Applied = "applied";

    /// <summary>
    /// La petición ya se había aplicado antes. No se toca el estado y se devuelve
    /// el mismo resultado. Es lo que hace que un reintento sea seguro.
    /// </summary>
    public const string Replayed = "replayed";

    /// <summary>Le toca al rival.</summary>
    public const string NotYourTurn = "not_your_turn";

    /// <summary>Todavía no se ha unido nadie, así que no hay turnos que pasar.</summary>
    public const string NotStarted = "not_started";

    /// <summary>
    /// El cliente actuaba sobre un turno que ya pasó. Su copia del estado está
    /// vieja: tiene que refrescar antes de volver a intentarlo.
    /// </summary>
    public const string Stale = "stale";

    /// <summary>
    /// Otra escritura ganó la carrera mientras procesábamos ésta. Reintentar con
    /// el mismo requestId es seguro, justamente por la idempotencia.
    /// </summary>
    public const string Conflict = "conflict";
}
