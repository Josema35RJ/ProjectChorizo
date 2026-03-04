using CMF;
using UnityEngine;
using Mirror;

public class LocalComponentChecker : NetworkBehaviour
{
    private Mover mover;
    private AdvancedWalkerController advancedWalkerController;
    private CharacterKeyboardInput characterKeyboardInput;
    
    [SerializeField] private GameObject playerCam; // attach in inspector
    
    private void Start()
    {
        if (!isLocalPlayer)
        {
            mover = GetComponent<Mover>();
            advancedWalkerController = GetComponent<AdvancedWalkerController>();
            characterKeyboardInput = GetComponent<CharacterKeyboardInput>();

            // DESACTIVAR componentes de movimiento
            if (mover != null) mover.enabled = false;
            if (advancedWalkerController != null) advancedWalkerController.enabled = false;
            if (characterKeyboardInput != null) characterKeyboardInput.enabled = false;

            // Apagar la cámara del clon
            if (playerCam != null) playerCam.SetActive(false);

            // NUEVO: Buscar y apagar CUALQUIER AudioListener en este clon (para que no haya orejas dobles)
            AudioListener[] listeners = GetComponentsInChildren<AudioListener>(true);
            foreach (AudioListener listener in listeners)
            {
                listener.enabled = false;
            }
        }
        else
        {
            // (Opcional) Si este ES el jugador local, apagar la cámara por defecto de la escena para evitar conflictos
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.gameObject != playerCam)
            {
                mainCam.gameObject.SetActive(false);
            }
        }
    }
}