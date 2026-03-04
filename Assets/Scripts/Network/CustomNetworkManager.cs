using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    // --- LÓGICA DE ESCENAS ---

    public override void OnClientSceneChanged()
    {
        // No llamamos a base.OnClientSceneChanged() para tener control total o 
        // lo llamamos al final. Aquí bloqueamos el cursor.
        base.OnClientSceneChanged();

        ConfigurarCursor();
        Debug.Log("CLIENTE: Escena cambiada y cursor configurado.");
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        ConfigurarCursor();
        Debug.Log($"SERVIDOR: Escena cambiada a {sceneName}");
    }

    private void ConfigurarCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // --- CARGA DE JUEGO ---

    public void ChangeScene()
    {
        if (NetworkServer.active)
        {
            // Es buena práctica usar ServerChangeScene directamente si no necesitas el delay.
            // Si necesitas el delay de 3s, Invoke está bien.
            Invoke(nameof(ChangingScene), 3f);
        }
    }

    private void ChangingScene()
    {
        ServerChangeScene("GameScene");
    }


    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {

        if (conn.identity != null)
        {
            Debug.LogWarning($"[Mirror] El cliente {conn.connectionId} ya tiene un jugador. Saltando OnServerAddPlayer.");
            return;
        }

        base.OnServerAddPlayer(conn);
    }

    // --- LOGS DE DEPURACIÓN ---

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log("<color=green>SERVIDOR: Un cliente se ha conectado a Mirror exitosamente.</color>");
    }

    public override void OnClientConnect()
    {
        // IMPORTANTE: Si usas AddPlayer manual en algún sitio, aquí es donde suele romperse.
        // Como dependemos del "Auto Create Player" del Inspector, solo llamamos al base.
        base.OnClientConnect();
        Debug.Log("<color=blue>CLIENTE: Me he conectado al Host de Mirror.</color>");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("<color=red>CLIENTE: Desconectado o conexión fallida.</color>");
    }
}