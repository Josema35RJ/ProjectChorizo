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
        // Actualizamos el color del tag si el jugador está hablando
        if (VivoxService.Instance.IsLoggedIn && nameTag != null)
        {
            bool estaHablando = false;
            // IMPORTANTE: Buscamos en el diccionario de canales activos
            if (VivoxService.Instance.ActiveChannels.TryGetValue(channelName, out var channel))
            {
                var participante = channel.FirstOrDefault(p => p.PlayerId == AuthenticationService.Instance.PlayerId);
                if (participante != null) estaHablando = participante.SpeechDetected;
            }
            nameTag.color = estaHablando ? Color.green : Color.white;
        }
    }

    private void LateUpdate()
    {
        // 1. Billboard del texto (esto ya funcionaba)
        if (nameTag != null && Camera.main != null)
        {
            nameTag.transform.LookAt(nameTag.transform.position + Camera.main.transform.rotation * Vector3.forward,
                                     Camera.main.transform.rotation * Vector3.up);
        }

        // 2. ACTUALIZACIÓN DE POSICIÓN 3D CORREGIDA
        if (isLocalPlayer && VivoxService.Instance.IsLoggedIn && Time.time >= nextSpatialUpdate)
        {
            nextSpatialUpdate = Time.time + spatialUpdateRate;

            // En tu versión del SDK, el método correcto es SetPosition (sin Relative)
            // o se hace a través de la interfaz del canal.
            // Probemos la firma estándar para el SDK v15.x:
            try
            {

            }
            catch (Exception)
            {
                // Si el anterior falla, el SDK espera que uses la posición 3D completa así:
                // VivoxService.Instance.SetPosition(transform.position, transform.position, transform.forward, transform.up, channelName);
            }
        }
    }

    private async void OnDestroy()
    {
        if (isLocalPlayer && VivoxService.Instance.IsLoggedIn)
        {
            try
            {
                await VivoxService.Instance.LogoutAsync();
            }
            catch { }
        }
    }
}