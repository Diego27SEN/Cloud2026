using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace HelloWorld;

/// <summary>
/// Configuración de arranque del módulo. Aquí se registran las dependencias que
/// Cloud Code inyectará en los constructores (por ejemplo, el cliente de las APIs
/// de juego que usaremos a partir de la semana 6 para Economy y Cloud Save).
/// </summary>
public class ModuleSetup : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.AddGameApiClient();
    }
}
