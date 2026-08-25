namespace Cloud2026.Services
{
    /// <summary>
    /// Espejo en cliente de la respuesta del módulo HelloWorld de Cloud Code.
    /// Es un simple transporte de datos: no calcula nada y no debe hacerlo.
    ///
    /// Los nombres coinciden con los de GreetingResponse en el módulo de servidor
    /// (CloudCode.Modules/HelloWorld). Si allí se renombra un campo, aquí también.
    /// </summary>
    public class GreetingResult
    {
        /// <summary>Saludo ya compuesto por el servidor, listo para pintar.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Identificador del jugador según el servidor, no según el cliente.</summary>
        public string PlayerId { get; set; } = string.Empty;

        /// <summary>Hora del servidor en UTC (ISO 8601).</summary>
        public string ServerTimeUtc { get; set; } = string.Empty;
    }
}
