using System;
using System.Threading.Tasks;

namespace Cloud2026.Services
{
    /// <summary>
    /// Contrato para el servicio de autenticación de UGS.
    /// Desacopla la lógica de UI y Gameplay de la implementación concreta del SDK.
    /// </summary>
    public interface IAuthService
    {
        event Action<string> OnSignedIn;
        event Action OnSignedOut;
        event Action<string> OnSignInFailed;

        bool IsInitialized { get; }
        bool IsSignedIn { get; }
        string PlayerId { get; }
        string PlayerName { get; }

        Task InitializeAsync();
        Task<bool> SignInAnonymouslyAsync();
        void SignOut(bool clearCredentials = false);
    }
}
