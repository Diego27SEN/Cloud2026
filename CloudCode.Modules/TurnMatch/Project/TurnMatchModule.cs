using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace TurnMatch;

/// <summary>
/// Partida por turnos entre dos jugadores, con el servidor como única autoridad.
///
/// El PoC gira alrededor de una idea: **la red pierde respuestas, no peticiones**.
/// Un cliente que envía "paso turno" y no recibe respuesta no sabe si el servidor
/// lo aplicó o no. Su única salida sensata es reintentar, y el servidor tiene que
/// estar preparado para que ese reintento no cuente dos veces.
///
/// La solución son dos mecanismos que resuelven problemas distintos y que a menudo
/// se confunden:
///
///   - **Clave de idempotencia** (requestId): la misma petición enviada N veces se
///     aplica una. Protege contra el reintento de un mismo cliente.
///   - **Write lock**: dos peticiones distintas que llegan a la vez no se pisan.
///     Protege contra la carrera entre los dos jugadores.
///
/// Hacen falta las dos. Con sólo la primera, dos jugadores simultáneos se
/// machacan el estado. Con sólo la segunda, un reintento pasa dos turnos.
/// </summary>
public class TurnMatchModule
{
    /// <summary>Intentos de derivar un código libre antes de rendirse.</summary>
    private const int MaxCodeAttempts = 5;

    private readonly MatchRepository m_Repository;
    private readonly ILogger<TurnMatchModule> m_Logger;

    public TurnMatchModule(IGameApiClient gameApiClient, ILogger<TurnMatchModule> logger)
    {
        m_Repository = new MatchRepository(gameApiClient);
        m_Logger = logger;
    }

    /// <summary>
    /// Crea una partida y devuelve su código para que el rival pueda unirse.
    ///
    /// Es idempotente sin necesidad de recordar nada: el código se deriva del
    /// requestId, así que reintentar cae sobre la partida que ya se creó.
    /// </summary>
    [CloudCodeFunction("CreateMatch")]
    public async Task<MatchView> CreateMatch(IExecutionContext context, string requestId)
    {
        var playerId = RequirePlayer(context);
        RequireRequestId(requestId);

        var now = DateTime.UtcNow;

        for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
        {
            var code = MatchCode.FromRequestId(requestId, attempt);
            var stored = await m_Repository.LoadAsync(context, code);

            if (stored == null)
            {
                var state = new MatchState
                {
                    MatchCode = code,
                    HostPlayerId = playerId,
                    CreatedByRequestId = requestId,
                    Status = MatchStatus.WaitingForGuest,
                    TurnNumber = 0,
                    CreatedAtUtc = now.ToString("o"),
                    UpdatedAtUtc = now.ToString("o")
                };

                if (!await m_Repository.TrySaveAsync(context, state, null))
                {
                    return MatchRules.BuildView(state, playerId, TurnOutcome.Conflict,
                        "Otra escritura se adelantó al crear la partida. Vuelve a intentarlo.", now);
                }

                m_Logger.LogInformation("Partida {Code} creada por {PlayerId}", code, playerId);
                return MatchRules.BuildView(state, playerId, TurnOutcome.Applied, string.Empty, now);
            }

            // Ya existe algo con ese código. Si salió de esta misma petición, es
            // que el intento anterior sí llegó: devolvemos la partida tal cual.
            if (stored.State.CreatedByRequestId == requestId && stored.State.HostPlayerId == playerId)
            {
                m_Logger.LogInformation("CreateMatch reintentado sobre la partida {Code}", code);
                return MatchRules.BuildView(stored.State, playerId, TurnOutcome.Replayed,
                    "Esta partida ya se había creado con esta misma petición.", now);
            }

            // Colisión con la partida de otra persona: probamos el siguiente código.
        }

        throw new Exception(
            "No se ha encontrado un código de partida libre. Vuelve a intentarlo con una petición nueva.");
    }

    /// <summary>
    /// Une al jugador que llama a una partida existente y arranca el primer turno.
    /// </summary>
    [CloudCodeFunction("JoinMatch")]
    public async Task<MatchView> JoinMatch(IExecutionContext context, string matchCode, string requestId)
    {
        var playerId = RequirePlayer(context);
        RequireRequestId(requestId);

        var code = RequireCode(matchCode);
        var now = DateTime.UtcNow;

        var stored = await LoadOrThrowAsync(context, code);
        var state = stored.State;

        // Reintento: si ya estás dentro, unirte otra vez no cambia nada.
        if (state.GuestPlayerId == playerId)
        {
            return MatchRules.BuildView(state, playerId, TurnOutcome.Replayed,
                "Ya estabas en esta partida.", now);
        }

        if (state.HostPlayerId == playerId)
        {
            throw new Exception("No puedes unirte a tu propia partida. Pásale el código a otra persona.");
        }

        if (state.GuestPlayerId.Length > 0)
        {
            throw new Exception("Esta partida ya tiene dos jugadores.");
        }

        MatchRules.StartWithGuest(state, playerId, now);

        if (!await m_Repository.TrySaveAsync(context, state, stored.WriteLock))
        {
            return MatchRules.BuildView(state, playerId, TurnOutcome.Conflict,
                "Alguien se unió justo antes que tú. Refresca para ver cómo quedó.", now);
        }

        m_Logger.LogInformation("{PlayerId} se unió a la partida {Code}", playerId, code);
        return MatchRules.BuildView(state, playerId, TurnOutcome.Applied, string.Empty, now);
    }

    /// <summary>
    /// Pasa el turno. Éste es el endpoint que da sentido al PoC.
    /// </summary>
    /// <param name="requestId">
    /// Identificador de *esta jugada*, no de este envío. El cliente lo genera una
    /// vez y lo reutiliza en todos los reintentos de la misma jugada; si genera uno
    /// nuevo al reintentar, la idempotencia deja de protegerle.
    /// </param>
    /// <param name="expectedTurnNumber">
    /// Turno sobre el que el cliente cree estar jugando. Si el servidor ya ha
    /// avanzado, la jugada se rechaza en vez de aplicarse sobre un estado que el
    /// jugador no llegó a ver.
    /// </param>
    [CloudCodeFunction("SubmitTurn")]
    public async Task<MatchView> SubmitTurn(
        IExecutionContext context, string matchCode, string requestId, int expectedTurnNumber)
    {
        var playerId = RequirePlayer(context);
        RequireRequestId(requestId);

        var code = RequireCode(matchCode);
        var now = DateTime.UtcNow;

        var stored = await LoadOrThrowAsync(context, code);
        var state = stored.State;

        // Lo primero, antes que cualquier validación: ¿ya habíamos aplicado esto?
        // Si se comprobara después, un reintento legítimo se rechazaría por
        // "no es tu turno" (porque el turno ya pasó al rival) y el jugador se
        // quedaría sin saber si su jugada contó.
        var processed = MatchRules.FindProcessed(state, requestId);
        if (processed != null)
        {
            m_Logger.LogInformation(
                "SubmitTurn repetido en {Code}: la petición {RequestId} ya dejó la partida en el turno {Turn}",
                code, requestId, processed.ResultingTurnNumber);

            return MatchRules.BuildView(state, playerId, TurnOutcome.Replayed,
                $"Esta jugada ya se había aplicado; dejó la partida en el turno {processed.ResultingTurnNumber}.",
                now);
        }

        if (!state.IsParticipant(playerId))
        {
            throw new Exception("No participas en esta partida.");
        }

        var verdict = MatchRules.ValidateTurn(state, playerId, expectedTurnNumber);
        if (verdict != TurnOutcome.Applied)
        {
            return MatchRules.BuildView(state, playerId, verdict, DescribeRejection(verdict, state), now);
        }

        MatchRules.ApplyTurn(state, playerId, requestId, now);

        if (!await m_Repository.TrySaveAsync(context, state, stored.WriteLock))
        {
            // El estado en memoria ya está modificado, pero no llegó a guardarse.
            // Devolvemos el turno que el cliente tenía para que no crea que avanzó.
            return MatchRules.BuildView(state, playerId, TurnOutcome.Conflict,
                "El rival escribió a la vez que tú. Reintenta con la misma petición: es seguro.", now);
        }

        m_Logger.LogInformation("{PlayerId} pasó el turno en {Code}; ahora va por el {Turn}",
            playerId, code, state.TurnNumber);

        return MatchRules.BuildView(state, playerId, TurnOutcome.Applied, string.Empty, now);
    }

    /// <summary>
    /// Consulta el estado. El cliente la usa para ver cuándo ha jugado el rival.
    /// </summary>
    [CloudCodeFunction("GetMatch")]
    public async Task<MatchView> GetMatch(IExecutionContext context, string matchCode)
    {
        var playerId = RequirePlayer(context);
        var code = RequireCode(matchCode);

        var stored = await LoadOrThrowAsync(context, code);

        if (!stored.State.IsParticipant(playerId))
        {
            throw new Exception("No participas en esta partida.");
        }

        return MatchRules.BuildView(stored.State, playerId, TurnOutcome.Ok, string.Empty, DateTime.UtcNow);
    }

    private async Task<StoredMatch> LoadOrThrowAsync(IExecutionContext context, string code)
    {
        var stored = await m_Repository.LoadAsync(context, code);
        if (stored == null)
        {
            throw new Exception($"No hay ninguna partida con el código {code}.");
        }

        return stored;
    }

    private static string DescribeRejection(string verdict, MatchState state) => verdict switch
    {
        TurnOutcome.NotStarted => "La partida aún espera a que se una el segundo jugador.",
        TurnOutcome.Stale => $"Tu copia de la partida va atrasada: aquí vamos por el turno {state.TurnNumber}.",
        TurnOutcome.NotYourTurn => "No es tu turno.",
        _ => string.Empty
    };

    private static string RequirePlayer(IExecutionContext context)
    {
        var playerId = context.PlayerId;
        if (string.IsNullOrEmpty(playerId))
        {
            throw new Exception("Esta operación necesita una sesión de jugador iniciada.");
        }

        return playerId;
    }

    /// <summary>
    /// El requestId lo pone el cliente, así que no confiamos en su forma. Sólo
    /// exigimos que exista y que no sea absurdamente largo: su contenido nos da
    /// igual mientras el cliente lo reutilice al reintentar.
    /// </summary>
    private static void RequireRequestId(string? requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId) || requestId.Length > 64)
        {
            throw new Exception("El identificador de la petición falta o no es válido.");
        }
    }

    private static string RequireCode(string? matchCode)
    {
        if (!MatchCode.IsWellFormed(matchCode))
        {
            throw new Exception("El código de partida no tiene el formato esperado.");
        }

        return MatchCode.Normalize(matchCode);
    }
}
