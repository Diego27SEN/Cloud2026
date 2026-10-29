using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace TurnMatch;

/// <summary>
/// El estado de la partida, más el write lock con el que se leyó.
///
/// El write lock es el control de concurrencia de Cloud Save: al guardar se envía
/// el que traía la lectura, y si otra escritura se coló en medio el servidor
/// rechaza la nuestra en vez de pisarla en silencio.
/// </summary>
public record StoredMatch(MatchState State, string? WriteLock);

/// <summary>
/// Lectura y escritura del estado de la partida en Cloud Save.
///
/// Usa custom data *privado*: los datos cuelgan de un identificador propio (el
/// código de la partida) en lugar de colgar de un jugador, que es lo que permite
/// que dos personas compartan el mismo estado. Y al ser privado, sólo Cloud Code
/// puede tocarlo: ninguno de los dos jugadores puede reescribirlo desde su cliente.
///
/// De ahí que todas las llamadas vayan con <c>context.ServiceToken</c> y no con
/// <c>context.AccessToken</c>. El primero es el token del servidor; el segundo es el
/// del jugador que hizo la petición, y un jugador no tiene permiso sobre datos
/// privados. Usar el token equivocado devuelve Unauthorized, que es exactamente lo
/// que debe pasar: si el token del jugador bastara, el dato no sería privado.
/// </summary>
public class MatchRepository
{
    private const string ItemKey = "state";

    private readonly IGameApiClient m_ApiClient;

    public MatchRepository(IGameApiClient apiClient)
    {
        m_ApiClient = apiClient;
    }

    /// <summary>Devuelve null si no existe ninguna partida con ese código.</summary>
    public async Task<StoredMatch?> LoadAsync(IExecutionContext context, string matchCode)
    {
        try
        {
            var response = await m_ApiClient.CloudSaveData.GetPrivateCustomItemsAsync(
                context,
                context.ServiceToken,
                context.ProjectId,
                matchCode,
                new List<string> { ItemKey },
                null!, // el SDK admite null aquí (paginación), pero no lo tiene anotado
                default);

            var item = response.Data?.Results?.FirstOrDefault(i => i.Key == ItemKey);
            if (item?.Value == null)
            {
                return null;
            }

            var state = ToMatchState(item.Value);
            return state == null ? null : new StoredMatch(state, item.WriteLock);
        }
        catch (ApiException e) when (StatusOf(e) == HttpStatusCode.NotFound)
        {
            // Todavía no ha escrito nadie con ese código: no es un fallo.
            return null;
        }
    }

    /// <summary>
    /// Guarda el estado. Pasa <paramref name="writeLock"/> tal cual vino de la
    /// lectura; si otro proceso escribió entre medias, esto lanza un conflicto.
    /// </summary>
    /// <returns>true si se guardó, false si otra escritura ganó la carrera.</returns>
    public async Task<bool> TrySaveAsync(IExecutionContext context, MatchState state, string? writeLock)
    {
        // writeLock nulo = escritura incondicional; es lo correcto al crear la partida.
        var body = new SetItemBody(ItemKey, JObject.FromObject(state), writeLock!);

        try
        {
            await m_ApiClient.CloudSaveData.SetPrivateCustomItemAsync(
                context,
                context.ServiceToken,
                context.ProjectId,
                state.MatchCode,
                body,
                default);

            return true;
        }
        catch (ApiException e) when (StatusOf(e) == HttpStatusCode.Conflict)
        {
            return false;
        }
    }

    /// <summary>
    /// Cloud Save devuelve el valor como objeto suelto. Aceptamos tanto un objeto
    /// JSON como una cadena con JSON dentro, porque según por dónde se haya escrito
    /// el dato puede llegar de las dos formas.
    /// </summary>
    private static MatchState? ToMatchState(object value)
    {
        return value switch
        {
            JObject json => json.ToObject<MatchState>(),
            string text when text.Length > 0 => JObject.Parse(text).ToObject<MatchState>(),
            _ => JObject.FromObject(value).ToObject<MatchState>()
        };
    }

    private static HttpStatusCode? StatusOf(ApiException exception)
    {
        return exception.Response?.StatusCode;
    }
}
