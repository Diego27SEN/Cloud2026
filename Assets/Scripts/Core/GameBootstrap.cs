using System;
using UnityEngine;
using Cloud2026.Services;

namespace Cloud2026.Core
{
    /// <summary>
    /// Punto de entrada del juego. Gestiona el ciclo de vida de los servicios UGS
    /// y la persistencia de la sesión entre escenas.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        [Header("Servicios")]
        [SerializeField] private UGSAuthService authService;

        [Header("Configuración de Arranque")]
        [Tooltip("Si es true, no destruye este GameObject al cargar nuevas escenas.")]
        [SerializeField] private bool persistAcrossScenes = true;

        [Tooltip("Si es true, intenta realizar login anónimo automático tras inicializar.")]
        [SerializeField] private bool autoLoginAnonymous = false;

        public IAuthService AuthService => authService;

        public event Action OnServicesReady;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureServicesAssigned();
        }

        private async void Start()
        {
            if (authService != null)
            {
                await authService.InitializeAsync();
                OnServicesReady?.Invoke();

                if (autoLoginAnonymous && !authService.IsSignedIn)
                {
                    await authService.SignInAnonymouslyAsync();
                }
            }
        }

        private void EnsureServicesAssigned()
        {
            if (authService == null)
            {
                authService = GetComponentInChildren<UGSAuthService>();
                if (authService == null)
                {
                    authService = gameObject.AddComponent<UGSAuthService>();
                }
            }
        }
    }
}
