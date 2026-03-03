using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    //This runs only on Client
    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        Debug.Log("client's scene changed");
    }

    //This runs only on Host-Server 
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName); 

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // ELIMINADO: SpawnAllPlayers(); 
        // Mirror instanciará los jugadores automáticamente de forma segura.
        Debug.Log("host's scene changed");
    }
    
    public void ChangeScene() //Calling by pressing Start Button, set from inspector.
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
}