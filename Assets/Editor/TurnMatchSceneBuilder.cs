using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using Cloud2026.Core;
using Cloud2026.Services;
using Cloud2026.UI;

namespace Cloud2026.EditorTools
{
    /// <summary>
    /// Genera la escena del PoC de turnos: Login -> Play -> Partida.
    /// </summary>
    public static class TurnMatchSceneBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string ScenePath = ScenesFolder + "/TurnMatch.unity";
        private const float PanelWidth = 860f;

        [MenuItem("Cloud2026/Crear escena Partida por turnos")]
        public static void CreateTurnMatchScene()
        {
            if (System.IO.File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Sobrescribir escena",
                    "Ya existe " + ScenePath + ". ¿Quieres reemplazarla?",
                    "Reemplazar", "Cancelar"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            BuildBootstrap();
            var canvas = UiFactory.CreateCanvas();
            BuildEventSystem();
            BuildUi(canvas.transform);

            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            Debug.Log("[TurnMatchSceneBuilder] Escena creada en " + ScenePath +
                      ". Despliega el módulo TurnMatch desde la ventana de Deployment antes de darle a Play.");
        }

        /// <summary>
        /// Aquí NO activamos el login automático: el PoC empieza en la pantalla de
        /// login a propósito, porque el flujo que se quiere enseñar es completo.
        /// </summary>
        private static void BuildBootstrap()
        {
            var go = new GameObject("GameBootstrap",
                typeof(UGSAuthService),
                typeof(UGSCloudCodeService),
                typeof(UGSTurnMatchService),
                typeof(GameBootstrap));

            var serialized = new SerializedObject(go.GetComponent<GameBootstrap>());
            UiFactory.Wire(serialized, "authService", go.GetComponent<UGSAuthService>());
            UiFactory.Wire(serialized, "cloudCodeService", go.GetComponent<UGSCloudCodeService>());
            UiFactory.Wire(serialized, "turnMatchService", go.GetComponent<UGSTurnMatchService>());
            serialized.FindProperty("autoLoginAnonymous").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void BuildUi(Transform canvasTransform)
        {
            var resources = UiFactory.Resources();

            var root = UiFactory.CreatePanel(canvasTransform, "TurnMatchPanel", PanelWidth, withBackground: true);
            var panel = root.AddComponent<TurnMatchPanel>();

            UiFactory.CreateText(root.transform, resources, "Title",
                "Partida por turnos · idempotencia", 38f, 54f, Color.white);

            var login = BuildLoginPanel(root.transform, resources, out var guestButton, out var loginStatus);
            var lobby = BuildLobbyPanel(root.transform, resources,
                out var createButton, out var joinInput, out var joinButton, out var lobbyStatus);
            var match = BuildMatchPanel(root.transform, resources,
                out var codeText, out var turnLabel, out var historyText, out var outcomeText,
                out var passButton, out var resendButton, out var leaveButton);

            var serialized = new SerializedObject(panel);

            UiFactory.Wire(serialized, "loginPanel", login);
            UiFactory.Wire(serialized, "guestLoginButton", guestButton);
            UiFactory.Wire(serialized, "loginStatusText", loginStatus);

            UiFactory.Wire(serialized, "lobbyPanel", lobby);
            UiFactory.Wire(serialized, "createMatchButton", createButton);
            UiFactory.Wire(serialized, "joinCodeInput", joinInput);
            UiFactory.Wire(serialized, "joinMatchButton", joinButton);
            UiFactory.Wire(serialized, "lobbyStatusText", lobbyStatus);

            UiFactory.Wire(serialized, "matchPanel", match);
            UiFactory.Wire(serialized, "matchCodeText", codeText);
            UiFactory.Wire(serialized, "turnText", turnLabel);
            UiFactory.Wire(serialized, "historyText", historyText);
            UiFactory.Wire(serialized, "outcomeText", outcomeText);
            UiFactory.Wire(serialized, "passTurnButton", passButton);
            UiFactory.Wire(serialized, "resendTurnButton", resendButton);
            UiFactory.Wire(serialized, "leaveMatchButton", leaveButton);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildLoginPanel(
            Transform parent, TMP_DefaultControls.Resources resources,
            out UnityEngine.UI.Button guestButton, out TextMeshProUGUI status)
        {
            var panel = UiFactory.CreatePanel(parent, "LoginPanel", PanelWidth, withBackground: false);

            UiFactory.CreateText(panel.transform, resources, "Explain",
                "Cloud Code identifica al jugador por su sesión, así que lo primero es entrar.",
                22f, 56f, UiFactory.SubtleText);

            guestButton = UiFactory.CreateButton(panel.transform, resources, "GuestLoginButton", "Entrar como invitado");
            status = UiFactory.CreateText(panel.transform, resources, "LoginStatusText", "", 22f, 40f, Color.white);

            return panel;
        }

        private static GameObject BuildLobbyPanel(
            Transform parent, TMP_DefaultControls.Resources resources,
            out UnityEngine.UI.Button createButton, out TMP_InputField joinInput,
            out UnityEngine.UI.Button joinButton, out TextMeshProUGUI status)
        {
            var panel = UiFactory.CreatePanel(parent, "LobbyPanel", PanelWidth, withBackground: false);

            createButton = UiFactory.CreateButton(panel.transform, resources, "CreateMatchButton", "Crear partida");

            UiFactory.CreateText(panel.transform, resources, "Separator",
                "— o únete a la de otra persona —", 20f, 34f, UiFactory.SubtleText);

            joinInput = UiFactory.CreateInputField(panel.transform, resources, "JoinCodeInput", "Código de 4 letras");
            joinButton = UiFactory.CreateButton(panel.transform, resources, "JoinMatchButton", "Unirse");
            status = UiFactory.CreateText(panel.transform, resources, "LobbyStatusText", "", 22f, 40f, Color.white);

            return panel;
        }

        private static GameObject BuildMatchPanel(
            Transform parent, TMP_DefaultControls.Resources resources,
            out TextMeshProUGUI codeText, out TextMeshProUGUI turnLabel,
            out TextMeshProUGUI historyText, out TextMeshProUGUI outcomeText,
            out UnityEngine.UI.Button passButton, out UnityEngine.UI.Button resendButton,
            out UnityEngine.UI.Button leaveButton)
        {
            var panel = UiFactory.CreatePanel(parent, "MatchPanel", PanelWidth, withBackground: false);

            codeText = UiFactory.CreateText(panel.transform, resources, "MatchCodeText", "", 30f, 44f, Color.white);
            turnLabel = UiFactory.CreateText(panel.transform, resources, "TurnText", "", 26f, 44f, Color.white);
            historyText = UiFactory.CreateText(panel.transform, resources, "HistoryText", "", 20f, 130f, UiFactory.SubtleText);

            passButton = UiFactory.CreateButton(panel.transform, resources, "PassTurnButton", "Pasar turno");

            resendButton = UiFactory.CreateButton(panel.transform, resources, "ResendTurnButton",
                "Reenviar la misma petición (simula un reintento)", 56f, 20f);

            outcomeText = UiFactory.CreateText(panel.transform, resources, "OutcomeText", "", 21f, 80f, Color.white);

            leaveButton = UiFactory.CreateButton(panel.transform, resources, "LeaveMatchButton", "Salir", 48f, 20f);

            return panel;
        }

        private static void AddSceneToBuildSettings()
        {
            var current = EditorBuildSettings.scenes;

            foreach (var entry in current)
            {
                if (entry.path == ScenePath) return;
            }

            var updated = new EditorBuildSettingsScene[current.Length + 1];
            current.CopyTo(updated, 0);
            updated[current.Length] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = updated;
        }
    }
}
