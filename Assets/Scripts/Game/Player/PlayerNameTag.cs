using TMPro;
using UnityEngine;
using Steamworks;
using Mirror;
using Unity.Services.Vivox;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;
using System.Threading.Tasks;
using System.Linq;

public class PlayerNameTag : NetworkBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI nameTag;

    [Header("Voz Proximidad")]
    public string channelName = "MisionGlobal";
    public int distanciaMaxima = 30;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    private float nextSpatialUpdate;
    private const float spatialUpdateRate = 0.1f;

    public override void OnStartLocalPlayer()
    {
        string steamName = SteamClient.IsValid ? SteamClient.Name : "Jugador_" + UnityEngine.Random.Range(100, 999);
        CmdSetPlayerName(steamName);

        _ = IniciarVoz(steamName);
    }

    [Command] void CmdSetPlayerName(string name) => playerName = name;
    void OnNameChanged(string old, string n) => nameTag.text = n;

    async Task IniciarVoz(string displayName)
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            if (!VivoxService.Instance.IsLoggedIn)
            {
                var options = new LoginOptions
                {
                    DisplayName = displayName.Replace(" ", "_"),
                    PlayerId = AuthenticationService.Instance.PlayerId
                };
                await VivoxService.Instance.LoginAsync(options);
            }

            if (!VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
            {
                // El modelo de audio InverseByDistance es el estándar para 3D realista
                var properties = new Channel3DProperties(distanciaMaxima, 1, 1.0f, AudioFadeModel.InverseByDistance);
                await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, properties);
            }

            Debug.Log("<color=green>Vivox: Conectado.</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error Vivox Init: {e.Message}");
        }
    }

    private void Update()
    {
        // Verificación de seguridad inicial
        if (VivoxService.Instance == null || !VivoxService.Instance.IsLoggedIn || nameTag == null)
            return;

        bool estaHablando = false;

        // Buscamos al participante en todos los canales activos
        foreach (var channel in VivoxService.Instance.ActiveChannels.Values)
        {
            // Buscamos por PlayerId (que es el ID único de Unity Services)
            var participant = channel.FirstOrDefault(p => p.DisplayName == playerName.Replace(" ", "_"));
            if (participant != null && participant.SpeechDetected)
            {
                estaHablando = true;
                break;
            }
        }

        nameTag.color = estaHablando ? Color.green : Color.white;
    }

    private void LateUpdate()
    {
        // 1. Billboard (Mirar a la cámara)
        if (nameTag != null && Camera.main != null)
        {
            nameTag.transform.LookAt(nameTag.transform.position + Camera.main.transform.rotation * Vector3.forward,
                                     Camera.main.transform.rotation * Vector3.up);
        }


        if (isLocalPlayer && VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn && Time.time >= nextSpatialUpdate)
        {
            nextSpatialUpdate = Time.time + spatialUpdateRate;

            // Según el error, el método requiere estos 6 parámetros:
            VivoxService.Instance.Set3DPosition(
                transform.position,     // speakerPos: donde sale tu voz
                transform.position,     // listenerPos: donde escuchas (tus oídos)
                transform.forward,      // forward: hacia donde miras
                transform.up,           // up: vector hacia arriba
                channelName,            // channelName: el nombre del canal
                true                    // updateInput: ¿actualizar el micrófono? Sí.
            );
        }
    }

    private async void OnDestroy()
    {
        // 1. Verificamos que sea el jugador local
        // 2. Verificamos que la instancia de Vivox aún exista en memoria
        if (isLocalPlayer && VivoxService.Instance != null)
        {
            try
            {
                // Verificamos si realmente estamos logueados antes de intentar el logout
                if (VivoxService.Instance.IsLoggedIn)
                {
                    await VivoxService.Instance.LogoutAsync();
                    Debug.Log("Vivox: Logout completado con éxito.");
                }
            }
            catch (Exception e)
            {
                // Silenciamos el error si el servicio ya no está disponible al cerrar
                Debug.LogWarning($"Vivox Logout omitido o fallido: {e.Message}");
            }
        }
    }
}