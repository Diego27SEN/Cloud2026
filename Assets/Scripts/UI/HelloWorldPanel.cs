using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cloud2026.Core;
using Cloud2026.Services;

namespace Cloud2026.UI
{
    /// <summary>
    /// Panel del "hola mundo" de Cloud Code. Pulsas un botón, el servidor compone el
    /// saludo, y aquí sólo se pinta la respuesta.
    ///
    /// Fíjate en lo que este script NO hace: no construye el mensaje, no sabe qué
    /// PlayerId tiene el jugador y no mira el reloj local. Los tres datos vienen del
    /// servidor. Ese reparto es el que mantendremos durante todo el curso.
    /// </summary>
    public class HelloWorldPanel : MonoBehaviour
    {
        [Header("Entrada")]
        [Tooltip("Nombre a mostrar en el saludo. Es cosmético: el servidor lo sanea y lo devuelve.")]
        [SerializeField] private TMP_InputField nameInput;

        [Tooltip("Botón que dispara la llamada al módulo de Cloud Code.")]
        [SerializeField] private Button sayHelloButton;

        [Header("Respuesta del servidor")]
        [Tooltip("Texto donde se pinta el mensaje que devuelve Cloud Code.")]
        [SerializeField] private TextMeshProUGUI messageText;

        [Tooltip("Texto con el PlayerId y la hora del servidor que acompañan al saludo.")]
        [SerializeField] private TextMeshProUGUI detailsText;

        [Header("Estado")]
        [Tooltip("Texto de estado y errores.")]
        [SerializeField] private TextMeshProUGUI statusText;

        private ICloudCodeService _cloudCodeService;
        private bool _isBusy;
        private bool _wasReady;

        private void Start()
        {
            FindAndConnectService();

            if (sayHelloButton != null)
            {
                sayHelloButton.onClick.AddListener(OnSayHelloClicked);
            }

            UpdateUIState();
        }

        private void OnDestroy()
        {
            if (sayHelloButton != null)
            {
                sayHelloButton.onClick.RemoveListener(OnSayHelloClicked);
            }

            if (_cloudCodeService != null)
            {
                _cloudCodeService.OnCallFailed -= HandleCallFailed;
            }
        }

        /// <summary>
        /// El login es asíncrono y puede terminar en cualquier frame posterior a Start.
        /// En vez de encadenar eventos, comprobamos si el servicio ya está listo y
        /// refrescamos la interfaz sólo cuando ese estado cambia.
        /// </summary>
        private void Update()
        {
            if (_isBusy || _cloudCodeService == null) return;

            bool isReady = _cloudCodeService.IsReady;
            if (isReady != _wasReady)
            {
                _wasReady = isReady;
                UpdateUIState();
            }
        }

        private void FindAndConnectService()
        {
            if (GameBootstrap.Instance != null)
            {
                _cloudCodeService = GameBootstrap.Instance.CloudCodeService;
            }

            _cloudCodeService ??= FindFirstObjectByType<UGSCloudCodeService>();

            if (_cloudCodeService != null)
            {
                _cloudCodeService.OnCallFailed += HandleCallFailed;
            }
            else
            {
                SetStatus("Falta el componente UGSCloudCodeService en la escena.", Color.red);
            }
        }

        private async void OnSayHelloClicked()
        {
            if (_isBusy || _cloudCodeService == null) return;

            SetBusyState(true);
            SetStatus("Llamando a Cloud Code...", Color.white);

            string displayName = nameInput != null ? nameInput.text : string.Empty;
            GreetingResult result = await _cloudCodeService.SayHelloAsync(displayName);

            SetBusyState(false);

            // Si result es null, el servicio ya avisó del motivo por OnCallFailed.
            if (result == null)
            {
                return;
            }

            if (messageText != null)
            {
                messageText.text = result.Message;
            }

            if (detailsText != null)
            {
                detailsText.text = $"PlayerId: {result.PlayerId}\nHora del servidor (UTC): {result.ServerTimeUtc}";
            }

            SetStatus("Respuesta recibida del servidor.", new Color(0.2f, 0.9f, 0.3f));
        }

        private void HandleCallFailed(string errorMessage) => SetStatus(errorMessage, Color.red);

        /// <summary>
        /// Sin sesión iniciada no hay llamada: Cloud Code identifica al jugador por su token.
        /// </summary>
        private void UpdateUIState()
        {
            bool isReady = _cloudCodeService != null && _cloudCodeService.IsReady;

            if (sayHelloButton != null)
            {
                sayHelloButton.interactable = isReady && !_isBusy;
            }

            if (_cloudCodeService == null) return;

            SetStatus(
                isReady ? "Sesión lista. Pulsa el botón para saludar al servidor."
                        : "Esperando a que se inicie la sesión en UGS...",
                isReady ? Color.white : Color.yellow);
        }

        private void SetBusyState(bool busy)
        {
            _isBusy = busy;

            if (sayHelloButton != null)
            {
                sayHelloButton.interactable = !busy;
            }
        }

        private void SetStatus(string message, Color color)
        {
            if (statusText == null) return;

            statusText.text = message;
            statusText.color = color;
        }
    }
}
