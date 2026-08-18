using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;

namespace Cloud2026.Services
{
    /// <summary>
    /// Wrapper autoritativo para Unity Gaming Services Authentication.
    /// Encapsula llamadas al SDK, gestión de eventos y captura tipada de excepciones.
    /// </summary>
    public class UGSAuthService : MonoBehaviour, IAuthService
    {
        public event Action<string> OnSignedIn;
        public event Action OnSignedOut;
        public event Action<string> OnSignInFailed;

        [Header("Configuración")]
        [Tooltip("Si es true, intenta inicializar Unity Services automáticamente en Awake.")]
        [SerializeField] private bool initializeOnAwake = true;

        [Tooltip("Perfil de autenticación a utilizar (opcional, útil para pruebas multi-jugador locales).")]
        [SerializeField] private string profileName = "";

        [Tooltip("Entorno de UGS contra el que se inicializa. Debe existir en el Dashboard del proyecto.")]
        [SerializeField] private string environmentName = "production";

        public bool IsInitialized => UnityServices.State == ServicesInitializationState.Initialized;
        public bool IsSignedIn => IsInitialized && AuthenticationService.Instance.IsSignedIn;
        public string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : string.Empty;
        public string PlayerName => IsSignedIn ? AuthenticationService.Instance.PlayerName : string.Empty;

        private Task _initializationTask;
        private bool _isSigningIn = false;

        private void Awake()
        {
            if (initializeOnAwake)
            {
                _ = InitializeAsync();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Inicializa los servicios centrales de Unity (UnityServices.InitializeAsync).
        /// </summary>
        public Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return Task.CompletedTask;
            }

            // Devolvemos la MISMA tarea en vuelo en lugar de retornar de inmediato: así,
            // quien haga await sobre una segunda llamada espera a la inicialización real
            // y no continúa creyendo que los servicios ya están listos.
            if (_initializationTask != null)
            {
                return _initializationTask;
            }

            _initializationTask = InitializeInternalAsync();
            return _initializationTask;
        }

        private async Task InitializeInternalAsync()
        {
            try
            {
                var options = new InitializationOptions();

                if (!string.IsNullOrWhiteSpace(environmentName))
                {
                    options.SetEnvironmentName(environmentName);
                }

                if (!string.IsNullOrWhiteSpace(profileName))
                {
                    options.SetProfile(profileName);
                }

                await UnityServices.InitializeAsync(options);
                Debug.Log($"[UGSAuthService] Unity Services inicializado correctamente. Entorno: '{environmentName}'.");

                SubscribeToEvents();
            }
            catch (ServicesInitializationException initEx)
            {
                Debug.LogError($"[UGSAuthService] Error al inicializar Unity Services (Servicio no disponible): {initEx.Message}");
                OnSignInFailed?.Invoke($"Error de inicialización: {initEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGSAuthService] Excepción inesperada durante InitializeAsync: {ex.Message}");
                OnSignInFailed?.Invoke($"Error inesperado: {ex.Message}");
            }
            finally
            {
                // Si falló, limpiamos la tarea para permitir un reintento posterior.
                if (!IsInitialized)
                {
                    _initializationTask = null;
                }
            }
        }

        /// <summary>
        /// Realiza el inicio de sesión anónimo contra los servidores de UGS.
        /// </summary>
        public async Task<bool> SignInAnonymouslyAsync()
        {
            if (_isSigningIn)
            {
                Debug.LogWarning("[UGSAuthService] Ya hay un intento de inicio de sesión en progreso.");
                return false;
            }

            if (!IsInitialized)
            {
                Debug.Log("[UGSAuthService] Servicios no inicializados. Inicializando antes del login...");
                await InitializeAsync();
                if (!IsInitialized)
                {
                    Debug.LogError("[UGSAuthService] No se pudo inicializar UGS para realizar el login.");
                    return false;
                }
            }

            if (IsSignedIn)
            {
                Debug.Log($"[UGSAuthService] Ya hay una sesión activa para el PlayerId: {PlayerId}");
                OnSignedIn?.Invoke(PlayerId);
                return true;
            }

            _isSigningIn = true;

            try
            {
                Debug.Log("[UGSAuthService] Iniciando login anónimo en UGS...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                
                string playerId = AuthenticationService.Instance.PlayerId;
                Debug.Log($"[UGSAuthService] ¡Login anónimo exitoso! PlayerId: {playerId}");
                return true;
            }
            catch (AuthenticationException authEx)
            {
                // Errores específicos de autenticación (ej. sesión inválida, credenciales revocadas)
                string errorMsg = $"Error de Autenticación ({authEx.ErrorCode}): {authEx.Message}";
                Debug.LogError($"[UGSAuthService] {errorMsg}");
                OnSignInFailed?.Invoke(errorMsg);
                return false;
            }
            catch (RequestFailedException reqEx)
            {
                // Errores de red o de solicitud al servidor
                string errorMsg = $"Error de Conexión/Servidor ({reqEx.ErrorCode}): {reqEx.Message}";
                Debug.LogError($"[UGSAuthService] {errorMsg}");
                OnSignInFailed?.Invoke(errorMsg);
                return false;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error inesperado durante el login anónimo: {ex.Message}";
                Debug.LogError($"[UGSAuthService] {errorMsg}");
                OnSignInFailed?.Invoke(errorMsg);
                return false;
            }
            finally
            {
                _isSigningIn = false;
            }
        }

        /// <summary>
        /// Cierra la sesión activa en UGS.
        /// </summary>
        /// <param name="clearCredentials">Si es true, borra el token de sesión almacenado en el cliente para crear un nuevo usuario anónimo en el próximo login.</param>
        public void SignOut(bool clearCredentials = false)
        {
            if (!IsSignedIn)
            {
                Debug.LogWarning("[UGSAuthService] No hay ninguna sesión activa para cerrar.");
                return;
            }

            try
            {
                if (clearCredentials)
                {
                    AuthenticationService.Instance.ClearSessionToken();
                    Debug.Log("[UGSAuthService] Token de sesión borrado. El próximo inicio de sesión generará un nuevo PlayerId.");
                }

                AuthenticationService.Instance.SignOut();
                Debug.Log("[UGSAuthService] Sesión cerrada correctamente.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGSAuthService] Error al cerrar sesión: {ex.Message}");
            }
        }

        private void SubscribeToEvents()
        {
            if (!IsInitialized) return;

            AuthenticationService.Instance.SignedIn += HandleSignedIn;
            AuthenticationService.Instance.SignedOut += HandleSignedOut;
            AuthenticationService.Instance.SignInFailed += HandleSignInFailed;
            AuthenticationService.Instance.Expired += HandleSessionExpired;
        }

        private void UnsubscribeFromEvents()
        {
            if (!IsInitialized) return;

            AuthenticationService.Instance.SignedIn -= HandleSignedIn;
            AuthenticationService.Instance.SignedOut -= HandleSignedOut;
            AuthenticationService.Instance.SignInFailed -= HandleSignInFailed;
            AuthenticationService.Instance.Expired -= HandleSessionExpired;
        }

        private void HandleSignedIn()
        {
            string playerId = AuthenticationService.Instance.PlayerId;
            Debug.Log($"[UGSAuthService] Evento SignedIn recibido. PlayerId: {playerId}");
            OnSignedIn?.Invoke(playerId);
        }

        private void HandleSignedOut()
        {
            Debug.Log("[UGSAuthService] Evento SignedOut recibido.");
            OnSignedOut?.Invoke();
        }

        private void HandleSignInFailed(RequestFailedException exception)
        {
            string errorMsg = $"Fallo en login ({exception.ErrorCode}): {exception.Message}";
            Debug.LogError($"[UGSAuthService] Evento SignInFailed recibido: {errorMsg}");
            OnSignInFailed?.Invoke(errorMsg);
        }

        private void HandleSessionExpired()
        {
            Debug.LogWarning("[UGSAuthService] La sesión de autenticación ha expirado.");
            OnSignedOut?.Invoke();
        }
    }
}
