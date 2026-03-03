using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    // This runs only on Client
    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        Debug.Log("client's scene changed");
    }

    // This runs only on Host-Server 
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName); 

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Eliminado: SpawnAllPlayers() y lógicas manuales. 
        // Mirror se encarga de instanciar a los jugadores automáticamente cuando la escena del cliente termina de cargar.
        Debug.Log("host's scene changed");
    }
    
    public void ChangeScene() // Calling by pressing Start Button, set from inspector.
    {
        if (NetworkServer.active)
        {
            // CHANGE SCENE
            Invoke(nameof(ChangingScene), 3f);
        }
    }

    public void ChangingScene()
    {
        ServerChangeScene("GameScene");
    }

    // =================================================================
    // --- LOGS DE DEPURACIÓN PARA VERIFICAR LA CONEXIÓN DE MIRROR ---
    // =================================================================
    
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log("<color=green>SERVIDOR: Un cliente se ha conectado a Mirror exitosamente.</color>");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("<color=blue>CLIENTE: Me he conectado al Host de Mirror exitosamente.</color>");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("<color=red>CLIENTE: Desconectado o conexión fallida con el Host de Mirror.</color>");
    }
}