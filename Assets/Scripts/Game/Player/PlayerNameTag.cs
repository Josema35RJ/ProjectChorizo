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
        string steamName = SteamClient.IsValid ? SteamClient.Name : "Jugador_" + UnityEngine.Random.Range(10, 99);
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
                await UnityServices.InitializeAsync();

            if (!VivoxService.Instance.IsLoggedIn)
            {
                await VivoxService.Instance.LoginAsync(new LoginOptions { 
                    DisplayName = displayName.Replace(" ", "_") 
                });
            }

            // Propiedades 3D clásicas
            var properties = new Channel3DProperties(distanciaMaxima, 2, 1.0f, AudioFadeModel.InverseByDistance);
            await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, properties);
            
            // Micro abierto
            VivoxService.Instance.UnmuteInputDevice(); 
            Debug.Log("<color=green>Vivox: Conectado y Micro Abierto.</color>");
        }
        catch (Exception e) { Debug.LogError("Error Vivox: " + e.Message); }
    }

    private void Update()
    {
        // DETECCIÓN DE VOZ
        if (isLocalPlayer && VivoxService.Instance.IsLoggedIn && nameTag != null)
        {
            bool estaHablando = false;
            
            // Buscamos al participante local ("Self") dentro de la colección del canal
            var participantes = VivoxService.Instance.ActiveChannels[channelName];
            if (participantes != null)
            {
                var self = participantes.FirstOrDefault(p => p.IsSelf);
                if (self != null) estaHablando = self.SpeechDetected;
            }

            nameTag.color = estaHablando ? Color.green : Color.white;
        }
    }

    private void LateUpdate()
    {
        // Billboard
        if (nameTag != null && Camera.main != null)
        {
            nameTag.transform.LookAt(nameTag.transform.position + Camera.main.transform.rotation * Vector3.forward,
                                     Camera.main.transform.rotation * Vector3.up);
        }

        // POSICIONAMIENTO 3D (CORRECCIÓN FINAL)
        if (isLocalPlayer && VivoxService.Instance.IsLoggedIn && Time.time >= nextSpatialUpdate)
        {
            nextSpatialUpdate = Time.time + spatialUpdateRate;
            
            // En v16.9.0 la firma correcta para el servicio global es:
            // (string channelName, Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity)
          
        }
    }

    private void OnDestroy()
    {
        if (isLocalPlayer && VivoxService.Instance.IsLoggedIn)
            VivoxService.Instance.LeaveAllChannelsAsync();
    }
}