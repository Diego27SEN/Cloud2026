using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cloud2026.Core;
using Cloud2026.Services;

namespace Cloud2026.UI
{
    /// <summary>
    /// Controlador de UI reactivo para el flujo de autenticación anónima.
    /// Utiliza el servicio IAuthService sin depender directamente del SDK de UGS.
    /// </summary>
    public class AnonymousLoginUI : MonoBehaviour
    {
        [Header("Contenedores de Estado")]
        [Tooltip("Panel visible cuando el usuario NO ha iniciado sesión.")]
        [SerializeField] private GameObject loggedOutPanel;

        [Tooltip("Panel visible cuando el usuario SÍ ha iniciado sesión.")]
        [SerializeField] private GameObject loggedInPanel;

        [Header("Botones")]
        [Tooltip("Botón para iniciar sesión de forma anónima.")]
        [SerializeField] private Button loginAnonymousButton;

        [Tooltip("Botón para cerrar sesión actual.")]
        [SerializeField] private Button signOutButton;

        [Tooltip("Botón para crear un nuevo usuario invitado (borra token local).")]
        [SerializeField] private Button newGuestButton;

        [Tooltip("Botón para continuar a la partida.")]
        [SerializeField] private Button playButton;

        [Header("Textos y Feedback (TMP)")]
        [Tooltip("Texto para mostrar el estado actual o errores.")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Tooltip("Texto para mostrar el PlayerId cuando la sesión esté activa.")]
        [SerializeField] private TextMeshProUGUI playerIdText;

        [Header("Indicador de Carga")]
        [Tooltip("Objeto o spinner que se activa durante operaciones asíncronas.")]
        [SerializeField] private GameObject loadingIndicator;

        [Tooltip("Velocidad de rotación del spinner si se usa.")]
        [SerializeField] private float spinnerSpeed = 200f;

        [Header("Transición de Gameplay (Opcional)")]
        [Tooltip("Objeto o canvas de gameplay que se activará al pulsar Jugar.")]
        [SerializeField] private GameObject gameplayRoot;

        [Tooltip("Si es true, oculta automáticamente este panel al iniciar partida.")]
        [SerializeField] private bool hideOnPlay = true;

        private IAuthService _authService;
        private bool _isBusy = false;

        private void Start()
        {
            FindAndConnectAuthService();
            SetupButtonListeners();
            UpdateUIState();
        }

        private void Update()
        {
            if (_isBusy && loadingIndicator != null && loadingIndicator.activeSelf)
            {
                loadingIndicator.transform.Rotate(0f, 0f, -spinnerSpeed * Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromAuthEvents();
            RemoveButtonListeners();
        }

        private void FindAndConnectAuthService()
        {
            if (GameBootstrap.Instance != null && GameBootstrap.Instance.AuthService != null)
            {
                _authService = GameBootstrap.Instance.AuthService;
            }
            else
            {
                _authService = FindFirstObjectByType<UGSAuthService>();
            }

            if (_authService != null)
            {
                _authService.OnSignedIn += HandleSignedIn;
                _authService.OnSignedOut += HandleSignedOut;
                _authService.OnSignInFailed += HandleSignInFailed;
            }
            else
            {
                SetStatus("Buscando servicio de autenticación...", Color.yellow);
            }
        }

        private void UnsubscribeFromAuthEvents()
        {
            if (_authService != null)
            {
                _authService.OnSignedIn -= HandleSignedIn;
                _authService.OnSignedOut -= HandleSignedOut;
                _authService.OnSignInFailed -= HandleSignInFailed;
            }
        }

        private void SetupButtonListeners()
        {
            if (loginAnonymousButton != null)
                loginAnonymousButton.onClick.AddListener(OnLoginButtonClicked);

            if (signOutButton != null)
                signOutButton.onClick.AddListener(OnSignOutButtonClicked);

            if (newGuestButton != null)
                newGuestButton.onClick.AddListener(OnNewGuestButtonClicked);

            if (playButton != null)
                playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        private void RemoveButtonListeners()
        {
            if (loginAnonymousButton != null)
                loginAnonymousButton.onClick.RemoveListener(OnLoginButtonClicked);

            if (signOutButton != null)
                signOutButton.onClick.RemoveListener(OnSignOutButtonClicked);

            if (newGuestButton != null)
                newGuestButton.onClick.RemoveListener(OnNewGuestButtonClicked);

            if (playButton != null)
                playButton.onClick.RemoveListener(OnPlayButtonClicked);
        }

        private async void OnLoginButtonClicked()
        {
            if (_isBusy || _authService == null) return;

            SetBusyState(true);
            SetStatus("Iniciando sesión anónima en UGS...", Color.white);

            bool success = await _authService.SignInAnonymouslyAsync();
            SetBusyState(false);

            if (success)
            {
                SetStatus("¡Sesión iniciada con éxito!", new Color(0.2f, 0.9f, 0.3f));
            }
            UpdateUIState();
        }

        private void OnSignOutButtonClicked()
        {
            if (_isBusy || _authService == null) return;

            _authService.SignOut(clearCredentials: false);
            SetStatus("Sesión cerrada.", Color.white);
            UpdateUIState();
        }

        private async void OnNewGuestButtonClicked()
        {
            if (_isBusy || _authService == null) return;

            SetBusyState(true);
            SetStatus("Creando nuevo perfil de invitado...", Color.white);

            _authService.SignOut(clearCredentials: true);
            bool success = await _authService.SignInAnonymouslyAsync();

            SetBusyState(false);
            if (success)
            {
                SetStatus("¡Nuevo invitado creado con éxito!", new Color(0.2f, 0.9f, 0.3f));
            }
            UpdateUIState();
        }

        private void OnPlayButtonClicked()
        {
            if (gameplayRoot != null)
            {
                gameplayRoot.SetActive(true);
            }

            if (hideOnPlay)
            {
                gameObject.SetActive(false);
            }
        }

        private void HandleSignedIn(string playerId)
        {
            SetStatus("¡Bienvenido!", new Color(0.2f, 0.9f, 0.3f));
            UpdateUIState();
        }

        private void HandleSignedOut()
        {
            SetStatus("Sesión finalizada.", Color.white);
            UpdateUIState();
        }

        private void HandleSignInFailed(string errorMessage)
        {
            SetBusyState(false);
            SetStatus($"Error: {errorMessage}", new Color(1f, 0.35f, 0.35f));
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            bool isSignedIn = _authService != null && _authService.IsSignedIn;

            if (loggedOutPanel != null)
                loggedOutPanel.SetActive(!isSignedIn);

            if (loggedInPanel != null)
                loggedInPanel.SetActive(isSignedIn);

            if (playerIdText != null)
            {
                if (isSignedIn && !string.IsNullOrEmpty(_authService.PlayerId))
                {
                    string fullId = _authService.PlayerId;
                    string formattedId = fullId.Length > 12 
                        ? $"{fullId.Substring(0, 6)}...{fullId.Substring(fullId.Length - 4)}" 
                        : fullId;
                    playerIdText.text = $"ID: <color=#FFE600>{formattedId}</color>";
                }
                else
                {
                    playerIdText.text = string.Empty;
                }
            }

            UpdateButtonsInteractable();
        }

        private void SetBusyState(bool busy)
        {
            _isBusy = busy;

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(busy);
            }

            UpdateButtonsInteractable();
        }

        private void UpdateButtonsInteractable()
        {
            bool canInteract = !_isBusy;

            if (loginAnonymousButton != null)
                loginAnonymousButton.interactable = canInteract;

            if (signOutButton != null)
                signOutButton.interactable = canInteract;

            if (newGuestButton != null)
                newGuestButton.interactable = canInteract;

            if (playButton != null)
                playButton.interactable = canInteract;
        }

        private void SetStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
        }
    }
}
