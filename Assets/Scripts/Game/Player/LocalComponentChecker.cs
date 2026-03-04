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
        // Solo apagamos los componentes de los personajes de los DEMÁS jugadores
        if (!isLocalPlayer)
        {
            mover = GetComponent<Mover>();
            advancedWalkerController = GetComponent<AdvancedWalkerController>();
            characterKeyboardInput = GetComponent<CharacterKeyboardInput>();

            // DESACTIVAR en lugar de destruir. Esto evita el error de dependencias.
            if (mover != null) mover.enabled = false;
            if (advancedWalkerController != null) advancedWalkerController.enabled = false;
            if (characterKeyboardInput != null) characterKeyboardInput.enabled = false;
            
            // También puedes obtener y desactivar el AudioControl y AnimationControl aquí si lo deseas
            // var audioControl = GetComponent<AudioControl>();
            // if (audioControl != null) audioControl.enabled = false;

            // Desactivar la cámara del jugador clon
            if (playerCam != null) playerCam.SetActive(false);
        }
    }
}