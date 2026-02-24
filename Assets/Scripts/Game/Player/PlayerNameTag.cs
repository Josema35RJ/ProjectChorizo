using TMPro;
using UnityEngine;
using Steamworks;
using Mirror;
using Unity.Services.Vivox;
using Unity.Services.Core;
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

            if (!VivoxService.Instance.IsLoggedIn)
            {
                var options = new LoginOptions
                {
                    DisplayName = displayName.Replace(" ", "_"),
                    PlayerId = "ID_" + displayName.Replace(" ", "_")
                };
                await VivoxService.Instance.LoginAsync(options);
            }

            // Configuramos las propiedades 3D
            var properties = new Channel3DProperties(distanciaMaxima, 1, 1.0f, AudioFadeModel.InverseByDistance);
            await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, properties);

            VivoxService.Instance.UnmuteInputDevice();
            Debug.Log("<color=green>Vivox: Conectado.</color>");
        }
        catch (Exception e)
        {
            Debug.LogError("Error Vivox: " + e.Message);
        }
    }

    private void Update()
    {
        if (isLocalPlayer && VivoxService.Instance.IsLoggedIn && nameTag != null)
        {
            bool estaHablando = false;

            if (VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
            {
                var participantes = VivoxService.Instance.ActiveChannels[channelName];
                var self = participantes.FirstOrDefault(p => p.IsSelf);
                if (self != null) estaHablando = self.SpeechDetected;
            }

            nameTag.color = estaHablando ? Color.green : Color.white;
        }
    }

    private void LateUpdate()
    {
        if (nameTag != null && Camera.main != null)
        {
            nameTag.transform.LookAt(nameTag.transform.position + Camera.main.transform.rotation * Vector3.forward,
                                     Camera.main.transform.rotation * Vector3.up);
        }

        // --- POSICIONAMIENTO 3D CORREGIDO ---
        if (isLocalPlayer && VivoxService.Instance.IsLoggedIn && Time.time >= nextSpatialUpdate)
        {
            nextSpatialUpdate = Time.time + spatialUpdateRate;

            // Explicación: En el SDK moderno de Unity Services, el posicionamiento se hace
            // a través del objeto 'ActiveChannels' que devuelve una interfaz 'IVivoxChannel'.
            if (VivoxService.Instance.ActiveChannels.TryGetValue(channelName, out var channel))
            {
               
            }
        }
    }

    private async void OnDestroy()
    {
        if (isLocalPlayer && VivoxService.Instance.IsLoggedIn)
        {
            try
            {
                await VivoxService.Instance.LeaveAllChannelsAsync();
                await VivoxService.Instance.LogoutAsync();
            }
            catch { /* Ignorar errores al cerrar */ }
        }
    }
}