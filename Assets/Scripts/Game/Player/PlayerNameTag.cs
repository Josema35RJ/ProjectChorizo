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

    private static bool _estaInicializandoServicios = false;
    private static bool _estaLogueandoVivox = false;

    private float nextSpatialUpdate;
    private const float spatialUpdateRate = 0.1f;

    // --- NUEVO: Ocultamos el cartel de nuestro propio jugador ---
    private void Start()
    {
        if (isLocalPlayer && nameTag != null)
        {
            // Apagamos completamente el objeto del Canvas que tiene el texto
            // (Mejor apagar el padre directo (Canvas) para no renderizar nada inútil)
            nameTag.transform.parent.gameObject.SetActive(false); 
        }
    }
    // ------------------------------------------------------------

    public override void OnStartLocalPlayer()
    {
        // Iniciamos una corrutina para esperar a que la red esté lista
        StartCoroutine(SetupPlayerRoutine());
    }

    private IEnumerator SetupPlayerRoutine()
    {
        // ESPERA CRÍTICA: Esperamos a que la conexión de Mirror sea estable
        // y el transporte (Steam) haya terminado el apretón de manos.
        while (!NetworkClient.ready || !NetworkClient.isConnected)
        {
            yield return null;
        }

        // Un pequeño respiro extra para asegurar que el socket de Steam no dé "NoConnection"
        yield return new WaitForSeconds(0.5f);

        // 1. Configurar nombre
        string steamName = SteamClient.IsValid ? SteamClient.Name : "Jugador_" + UnityEngine.Random.Range(100, 999);

        // Ahora es seguro enviar el Command
        CmdSetPlayerName(steamName);

        // 2. Iniciar Vivox
        _ = IniciarVoz(steamName);
    }

    [Command]
    void CmdSetPlayerName(string name)
    {
        playerName = name;
    }

    void OnNameChanged(string old, string n)
    {
        if (nameTag != null) nameTag.text = n;
    }

    async Task IniciarVoz(string displayName)
    {
        try
        {
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

            if (VivoxService.Instance.IsLoggedIn && !VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
            {
                var properties = new Channel3DProperties(distanciaMaxima, 1, 1.0f, AudioFadeModel.InverseByDistance);
                await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, properties);
            }
        }
        catch (Exception e)
        {
            _estaInicializandoServicios = false;
            _estaLogueandoVivox = false;
            Debug.LogError($"Error Vivox: {e.Message}");
        }
    }

    private void Update()
    {
        if (VivoxService.Instance == null || !VivoxService.Instance.IsLoggedIn || string.IsNullOrEmpty(playerName) || nameTag == null)
            return;

        bool estaHablando = false;
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
        // Solo intentamos girar el cartel si no somos nosotros (ya que el nuestro lo hemos apagado en Start)
        if (!isLocalPlayer && nameTag != null && Camera.main != null)
        {
            nameTag.transform.parent.LookAt(nameTag.transform.parent.position + Camera.main.transform.rotation * Vector3.forward,
                                     Camera.main.transform.rotation * Vector3.up);
        }

        if (isLocalPlayer && VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn && Time.time >= nextSpatialUpdate)
        {
            nextSpatialUpdate = Time.time + spatialUpdateRate;
            if (VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
            {
                VivoxService.Instance.Set3DPosition(transform.position, transform.position, transform.forward, transform.up, channelName, true);
            }
        }
    }

    private async void OnDestroy()
    {
        if (isLocalPlayer && VivoxService.Instance != null)
        {
            try
            {
                if (VivoxService.Instance.IsLoggedIn) await VivoxService.Instance.LogoutAsync();
            }
            catch { /* Ignorar errores al cerrar */ }
        }
    }
}