using System;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace HelloWorld;

/// <summary>
/// Primer módulo del curso: un saludo que demuestra el reparto de responsabilidades.
///
/// El cliente sólo pide "salúdame". Quién es el jugador y qué hora es lo decide el
/// servidor: el PlayerId sale del token de sesión que UGS ya validó, y la hora sale
/// del reloj del servidor. Ninguno de los dos llega por parámetros, porque cualquier
/// dato que envíe el cliente puede estar manipulado.
/// </summary>
public class HelloWorldModule
{
    readonly ILogger<HelloWorldModule> m_Logger;

    public HelloWorldModule(ILogger<HelloWorldModule> logger)
    {
        m_Logger = logger;
    }

    /// <summary>
    /// Devuelve un saludo compuesto por el servidor.
    /// </summary>
    /// <param name="context">
    /// Contexto de ejecución que inyecta Cloud Code. Trae la identidad real del
    /// jugador que hizo la llamada; no es un parámetro que el cliente pueda falsear.
    /// </param>
    /// <param name="displayName">
    /// Nombre que el jugador quiere ver en pantalla. Es puramente cosmético: no
    /// concede permisos ni identifica a nadie, así que aceptarlo del cliente es
    /// inofensivo. Aun así lo saneamos antes de devolverlo.
    /// </param>
    [CloudCodeFunction("SayHello")]
    public GreetingResponse SayHello(IExecutionContext context, string? displayName = null)
    {
        // El SDK declara PlayerId como anulable; en una llamada autenticada siempre viene.
        var playerId = context.PlayerId ?? string.Empty;
        var serverTimeUtc = DateTime.UtcNow;

        var name = SanitizeDisplayName(displayName);

        m_Logger.LogInformation("SayHello ejecutado para el jugador {PlayerId}", playerId);

        return new GreetingResponse
        {
            Message = $"¡Hola, {name}! Te saluda Cloud Code desde el servidor.",
            PlayerId = playerId,
            ServerTimeUtc = serverTimeUtc.ToString("o")
        };
    }

    /// <summary>
    /// Recorta y limita el nombre recibido del cliente. Nunca devolvemos tal cual
    /// una cadena que venga de fuera: podría ser vacía, enorme o contener basura.
    /// </summary>
    public static string SanitizeDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "jugador";
        }

        var trimmed = displayName.Trim();
        return trimmed.Length > 24 ? trimmed[..24] : trimmed;
    }
}
