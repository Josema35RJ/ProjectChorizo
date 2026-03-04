using CMF;
using UnityEngine;
using Mirror;

public class LocalComponentChecker : NetworkBehaviour
{
    private Mover mover;
    private AdvancedWalkerController advancedWalkerController;
    private CharacterKeyboardInput characterKeyboardInput;
    [SerializeField] private GameObject playerCam; // Attach in inspector
    
    // OnStartLocalPlayer es un evento de Mirror que se dispara de forma segura
    // SOLO cuando se confirma que este cliente es el dueño de este objeto.
    public override void OnStartLocalPlayer()
    {
        // Aseguramos que nuestra propia cámara se encienda
        if (playerCam != null)
        {
            playerCam.SetActive(true);
        }
    }

    private void Start()
    {
        // Si no somos el dueño local (es decir, es el clon de otro jugador en nuestra pantalla)
        if (!isLocalPlayer)
        {
            mover = GetComponent<Mover>();
            advancedWalkerController = GetComponent<AdvancedWalkerController>();
            characterKeyboardInput = GetComponent<CharacterKeyboardInput>();

            // Destruimos los componentes para que los demás jugadores no reaccionen a nuestros inputs
            if (mover != null) Destroy(mover);
            if (advancedWalkerController != null) Destroy(advancedWalkerController);
            if (characterKeyboardInput != null) Destroy(characterKeyboardInput);
            
            // Apagamos la cámara del "otro" jugador
            if (playerCam != null)
            {
                playerCam.SetActive(false);
            }
        }
    }
}