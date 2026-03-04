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
using System.Collections;

public class PlayerNameTag : NetworkBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI nameTag;

    [Header("Voz Proximidad")]
    public string channelName = "MisionGlobal";
    public int distanciaMaxima = 30;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    // Estados de control para evitar errores de Vivox
    private static bool _estaInicializandoServicios = false;
    private static bool _estaLogueandoVivox = false;

    private float nextSpatialUpdate;
    private const float spatialUpdateRate = 0.1f;

    public override void OnStartLocalPlayer()
    {
        // 1. Configurar nombre desde Steam o Random
        string steamName = SteamClient.IsValid ? SteamClient.Name : "Jugador_" + UnityEngine.Random.Range(100, 999);
        CmdSetPlayerName(steamName);

        // 2. Iniciar Vivox de forma segura
        _ = IniciarVoz(steamName);
    }

    [Command] void CmdSetPlayerName(string name) => playerName = name;

    void OnNameChanged(string old, string n)
    {
        if (nameTag != null) nameTag.text = n;
    }

    async Task IniciarVoz(string displayName)
    {
        try
        {
            // Evitar que múltiples instancias inicialicen servicios al mismo tiempo
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                if (_estaInicializandoServicios) return;
                _estaInicializandoServicios = true;
                await UnityServices.InitializeAsync();
                _estaInicializandoServicios = false;
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // Evitar el error "Already signing in"
            if (!VivoxService.Instance.IsLoggedIn)
            {
                if (_estaLogueandoVivox) return;
                _estaLogueandoVivox = true;

                var options = new LoginOptions
                {
                    DisplayName = displayName.Replace(" ", "_"),
                    PlayerId = AuthenticationService.Instance.PlayerId
                };

                await VivoxService.Instance.LoginAsync(options);
                _estaLogueandoVivox = false;
            }

            // Unirse al canal si no estamos en él
            if (VivoxService.Instance.IsLoggedIn && !VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
            {
                var properties = new Channel3DProperties(distanciaMaxima, 1, 1.0f, AudioFadeModel.InverseByDistance);
                await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, properties);
            }

            Debug.Log("<color=green>Vivox: Conectado y en canal posicional.</color>");
        }
        catch (Exception e)
        {
            _estaInicializandoServicios = false;
            _estaLogueandoVivox = false;
            Debug.LogError($"Error Vivox Init: {e.Message}");
        }
    }

    private void Update()
    {
        // Solo verificamos si alguien habla si Vivox está listo y el objeto tiene nombre
        if (VivoxService.Instance == null || !VivoxService.Instance.IsLoggedIn || string.IsNullOrEmpty(playerName) || nameTag == null)
            return;

        bool estaHablando = false;

        // Comprobamos si este jugador específico está hablando
        foreach (var channel in VivoxService.Instance.ActiveChannels.Values)
        {
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

        // 2. Actualizar posición 3D (Solo para el jugador local)
        if (isLocalPlayer && VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn && Time.time >= nextSpatialUpdate)
        {
            nextSpatialUpdate = Time.time + spatialUpdateRate;

            if (VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
            {
                VivoxService.Instance.Set3DPosition(
                    transform.position,    // speakerPos
                    transform.position,    // listenerPos
                    transform.forward,     // forward
                    transform.up,          // up
                    channelName,           // channelName
                    true                   // updateInput
                );
            }
        }
    }

    private async void OnDestroy()
    {
        // Limpieza al destruir el objeto
        if (isLocalPlayer && VivoxService.Instance != null)
        {
            try
            {
                if (VivoxService.Instance.IsLoggedIn)
                {
                    await VivoxService.Instance.LogoutAsync();
                    Debug.Log("Vivox: Logout completado.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Vivox Logout omitido: {e.Message}");
            }
        }
    }
}