using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TurnMatch;

/// <summary>
/// Códigos de partida derivados del identificador de la petición.
///
/// No se sortean al azar a propósito: si el código saliera de un generador
/// aleatorio, un reintento de "crear partida" produciría una partida distinta y
/// el jugador acabaría con dos. Derivándolo del requestId, reintentar cae en el
/// mismo código y el servidor reconoce que ya existe.
/// </summary>
public static class MatchCode
{
    /// <summary>
    /// Sin I, O, 0 ni 1: son los que se confunden al dictarlos en voz alta o al
    /// copiarlos de una pizarra, y este código se pasa entre personas.
    /// </summary>
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public const int Length = 4;

    /// <summary>
    /// Deriva el código. <paramref name="attempt"/> permite buscar el siguiente
    /// candidato cuando el código ya lo ocupa otra partida.
    /// </summary>
    public static string FromRequestId(string requestId, int attempt = 0)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{requestId}#{attempt}"));

        var chars = new char[Length];
        for (var i = 0; i < Length; i++)
        {
            // 256 es múltiplo de 32, así que el módulo no sesga el reparto.
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(chars);
    }

    /// <summary>Acepta lo que el jugador haya tecleado y lo deja en forma canónica.</summary>
    public static string Normalize(string? input)
    {
        return (input ?? string.Empty).Trim().ToUpperInvariant();
    }

    public static bool IsWellFormed(string? code)
    {
        var normalized = Normalize(code);
        return normalized.Length == Length && normalized.All(Alphabet.Contains);
    }
}
