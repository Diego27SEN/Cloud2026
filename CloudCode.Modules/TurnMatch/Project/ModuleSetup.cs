using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace TurnMatch;

/// <summary>
/// Registra las dependencias que Cloud Code inyecta en el módulo. Aquí hace falta
/// el cliente de las APIs de juego porque el estado de la partida vive en Cloud Save.
/// </summary>
public class ModuleSetup : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.AddGameApiClient();
    }
}
