using System.Collections.Generic;

namespace TurnMatch;

/// <summary>
/// Estado autoritativo de una partida. Vive en Cloud Save como dato privado de
/// custom data: sólo Cloud Code puede leerlo y escribirlo, ni siquiera los dos
/// jugadores que participan pueden tocarlo desde el cliente.
/// </summary>
public class MatchState
{
    /// <summary>Código corto que los jugadores se pasan para encontrarse.</summary>
    public string MatchCode { get; set; } = string.Empty;

    public string HostPlayerId { get; set; } = string.Empty;

    /// <summary>
    /// Petición que creó la partida. Permite reconocer un reintento de CreateMatch
    /// sin guardar nada aparte: si el código derivado ya existe y lo creó esta misma
    /// petición, es que el intento anterior sí llegó.
    /// </summary>
    public string CreatedByRequestId { get; set; } = string.Empty;

    /// <summary>Vacío mientras nadie se ha unido.</summary>
    public string GuestPlayerId { get; set; } = string.Empty;

    public string Status { get; set; } = MatchStatus.WaitingForGuest;

    /// <summary>
    /// Turno en curso, empezando en 1 cuando entra el segundo jugador. Es la
    /// version del estado: el cliente dice sobre qué turno cree que está actuando
    /// y el servidor rechaza la jugada si ya ha avanzado.
    /// </summary>
    public int TurnNumber { get; set; }

    public string CurrentPlayerId { get; set; } = string.Empty;

    public List<TurnRecord> History { get; set; } = new();

    /// <summary>
    /// Peticiones ya aplicadas. Si llega una repetida no se vuelve a aplicar:
    /// se devuelve el estado tal cual. Es el corazón de la idempotencia.
    /// </summary>
    public List<ProcessedRequest> ProcessedRequests { get; set; } = new();

    public string CreatedAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;

    /// <summary>Devuelve el identificador del rival del jugador indicado, o vacío.</summary>
    public string OpponentOf(string playerId)
    {
        if (playerId == HostPlayerId) return GuestPlayerId;
        if (playerId == GuestPlayerId) return HostPlayerId;
        return string.Empty;
    }

    public bool IsParticipant(string playerId)
    {
        return playerId == HostPlayerId || (GuestPlayerId.Length > 0 && playerId == GuestPlayerId);
    }
}

public static class MatchStatus
{
    public const string WaitingForGuest = "waiting_for_guest";
    public const string Playing = "playing";
}

public class TurnRecord
{
    public int TurnNumber { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public string PlayedAtUtc { get; set; } = string.Empty;
}

/// <summary>
/// Rastro de una petición ya procesada. Guardamos con qué turno acabó para poder
/// responder exactamente lo mismo si el cliente reintenta.
/// </summary>
public class ProcessedRequest
{
    public string RequestId { get; set; } = string.Empty;
    public int ResultingTurnNumber { get; set; }
    public string PlayerId { get; set; } = string.Empty;
}
