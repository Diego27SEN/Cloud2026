using System.Collections.Generic;

namespace Cloud2026.Services
{
    /// <summary>
    /// Espejo en cliente de MatchView, lo que devuelve el módulo TurnMatch.
    /// Transporta datos y nada más: ni una sola decisión de juego se toma aquí.
    ///
    /// Si cambias un campo en CloudCode.Modules/TurnMatch/Project/MatchView.cs,
    /// cámbialo también aquí. Es un contrato entre las dos mitades.
    /// </summary>
    public class MatchViewDto
    {
        public string MatchCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TurnNumber { get; set; }

        public string YourPlayerId { get; set; } = string.Empty;
        public string OpponentPlayerId { get; set; } = string.Empty;
        public string CurrentPlayerId { get; set; } = string.Empty;

        /// <summary>Lo decide el servidor. El cliente sólo lo obedece.</summary>
        public bool IsYourTurn { get; set; }

        public List<TurnRecordDto> History { get; set; } = new List<TurnRecordDto>();

        /// <summary>Qué pasó con la petición. Ver <see cref="MatchOutcome"/>.</summary>
        public string Outcome { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
        public string ServerTimeUtc { get; set; } = string.Empty;

        public bool HasOpponent => !string.IsNullOrEmpty(OpponentPlayerId);
    }

    public class TurnRecordDto
    {
        public int TurnNumber { get; set; }
        public string PlayerId { get; set; } = string.Empty;
        public string PlayedAtUtc { get; set; } = string.Empty;
    }

    /// <summary>
    /// Los valores de MatchView.Outcome. Deben coincidir con TurnOutcome del módulo.
    /// </summary>
    public static class MatchOutcome
    {
        public const string Ok = "ok";
        public const string Applied = "applied";
        public const string Replayed = "replayed";
        public const string NotYourTurn = "not_your_turn";
        public const string NotStarted = "not_started";
        public const string Stale = "stale";
        public const string Conflict = "conflict";
    }

    public static class MatchStatusValues
    {
        public const string WaitingForGuest = "waiting_for_guest";
        public const string Playing = "playing";
    }
}
