using System;
using System.Collections.Generic;
using System.Linq;

namespace TurnMatch;

/// <summary>
/// Las reglas de la partida, sin nada de entrada/salida.
///
/// Está separado del módulo a propósito: aquí no hay Cloud Save ni contexto de
/// ejecución, sólo estado que entra y estado que sale. Eso lo hace comprobable
/// con pruebas unitarias normales, sin servidor y sin simular el SDK.
/// </summary>
public static class MatchRules
{
    /// <summary>
    /// Cuántas peticiones recordamos. Acotarlo evita que el estado crezca sin
    /// límite; el precio es que un reintento absurdamente tardío no se reconoce.
    /// Con turnos alternos, veinte cubre de sobra cualquier reintento razonable.
    /// </summary>
    public const int MaxProcessedRequests = 20;

    /// <summary>El historial es para enseñar en pantalla, no para decidir nada.</summary>
    public const int MaxHistoryEntries = 50;

    /// <summary>
    /// Busca si esta petición ya se aplicó. Devuelve null si es nueva.
    /// </summary>
    public static ProcessedRequest? FindProcessed(MatchState state, string requestId)
    {
        return state.ProcessedRequests.FirstOrDefault(p => p.RequestId == requestId);
    }

    /// <summary>
    /// Decide si la jugada es legal. Devuelve una constante de <see cref="TurnOutcome"/>:
    /// Applied si puede aplicarse, o el motivo del rechazo.
    ///
    /// El orden importa. Primero se comprueba si el cliente va con el estado viejo,
    /// porque "refresca y vuelve a mirar" es más útil que "no es tu turno" cuando
    /// ambas cosas son ciertas a la vez.
    /// </summary>
    public static string ValidateTurn(MatchState state, string playerId, int expectedTurnNumber)
    {
        if (state.Status != MatchStatus.Playing)
        {
            return TurnOutcome.NotStarted;
        }

        if (expectedTurnNumber != state.TurnNumber)
        {
            return TurnOutcome.Stale;
        }

        if (state.CurrentPlayerId != playerId)
        {
            return TurnOutcome.NotYourTurn;
        }

        return TurnOutcome.Applied;
    }

    /// <summary>
    /// Aplica el turno: lo anota, pasa el turno al rival y deja constancia de la
    /// petición para que un reintento no lo vuelva a aplicar.
    ///
    /// Llamar a esto sin haber pasado antes por <see cref="ValidateTurn"/> y por
    /// <see cref="FindProcessed"/> sería un error: es justo lo que abre la puerta
    /// a que un reintento cuente dos veces.
    /// </summary>
    public static void ApplyTurn(MatchState state, string playerId, string requestId, DateTime nowUtc)
    {
        var stamp = nowUtc.ToString("o");

        state.History.Add(new TurnRecord
        {
            TurnNumber = state.TurnNumber,
            PlayerId = playerId,
            PlayedAtUtc = stamp
        });

        TrimFromStart(state.History, MaxHistoryEntries);

        state.TurnNumber++;
        state.CurrentPlayerId = state.OpponentOf(playerId);
        state.UpdatedAtUtc = stamp;

        state.ProcessedRequests.Add(new ProcessedRequest
        {
            RequestId = requestId,
            ResultingTurnNumber = state.TurnNumber,
            PlayerId = playerId
        });

        TrimFromStart(state.ProcessedRequests, MaxProcessedRequests);
    }

    /// <summary>
    /// Prepara una partida recién creada para empezar: entra el invitado y el
    /// primer turno es del anfitrión.
    /// </summary>
    public static void StartWithGuest(MatchState state, string guestPlayerId, DateTime nowUtc)
    {
        state.GuestPlayerId = guestPlayerId;
        state.Status = MatchStatus.Playing;
        state.TurnNumber = 1;
        state.CurrentPlayerId = state.HostPlayerId;
        state.UpdatedAtUtc = nowUtc.ToString("o");
    }

    /// <summary>
    /// Arma la respuesta para un jugador concreto. IsYourTurn lo calcula el
    /// servidor: el cliente no decide de quién es el turno, sólo lo pinta.
    /// </summary>
    public static MatchView BuildView(MatchState state, string playerId, string outcome, string message, DateTime nowUtc)
    {
        return new MatchView
        {
            MatchCode = state.MatchCode,
            Status = state.Status,
            TurnNumber = state.TurnNumber,
            YourPlayerId = playerId,
            OpponentPlayerId = state.OpponentOf(playerId),
            CurrentPlayerId = state.CurrentPlayerId,
            IsYourTurn = state.Status == MatchStatus.Playing && state.CurrentPlayerId == playerId,
            History = state.History.TakeLast(10).ToList(),
            Outcome = outcome,
            Message = message,
            ServerTimeUtc = nowUtc.ToString("o")
        };
    }

    private static void TrimFromStart<T>(List<T> items, int maximum)
    {
        if (items.Count <= maximum) return;

        items.RemoveRange(0, items.Count - maximum);
    }
}
