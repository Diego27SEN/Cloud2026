namespace HelloWorld;

/// <summary>
/// Respuesta del endpoint SayHello. Los tres campos los rellena el servidor:
/// el cliente no aporta ninguno, sólo los muestra.
/// </summary>
public class GreetingResponse
{
    /// <summary>Saludo ya compuesto, listo para pintar en pantalla.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Identificador del jugador, tomado del token de sesión que valida UGS.</summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>Hora del servidor en UTC (ISO 8601). La única hora en la que confiamos.</summary>
    public string ServerTimeUtc { get; set; } = string.Empty;
}
