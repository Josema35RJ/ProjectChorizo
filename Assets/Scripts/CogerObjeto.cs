using UnityEngine;
using Mirror;

// Cambiamos MonoBehaviour por NetworkBehaviour
public class CogerObjeto : NetworkBehaviour
{
    public GameObject handPoint;
    private GameObject pickedObject = null;
    private GameObject objectInRange = null; 

    void Update()
    {
        // MUY IMPORTANTE: Solo queremos que el jugador local pueda usar estas teclas.
        // Evita que al pulsar 'E', todos los clones de tu personaje intenten coger algo.
        if (!isLocalPlayer) return;

        // SOLTAR OBJETO
        if (pickedObject != null && Input.GetKeyDown(KeyCode.Q))
        {
            CmdDropObject(); // Le pedimos al servidor que suelte el objeto
        }

        // COGER OBJETO
        if (objectInRange != null && pickedObject == null && Input.GetKeyDown(KeyCode.E))
        {
            // Buscamos la identidad de red del objeto para pasársela al servidor
            NetworkIdentity netId = objectInRange.GetComponent<NetworkIdentity>();
            if (netId != null)
            {
                CmdPickUpObject(netId); // Le pedimos al servidor coger este objeto exacto
            }
        }
    }

    // [Command] se ejecuta SOLO en el Servidor (El host)
    [Command]
    private void CmdPickUpObject(NetworkIdentity objNetId)
    {
        // Opcional: Aquí podrías comprobar si el objeto ya ha sido cogido por otro jugador
        // para evitar que dos personas lo cojan a la vez.

        // El servidor le dice a TODOS los clientes que este jugador agarró el objeto
        RpcPickUpObject(objNetId);
    }

    // [ClientRpc] lo reciben TODOS los jugadores de la partida al mismo tiempo
    [ClientRpc]
    private void RpcPickUpObject(NetworkIdentity objNetId)
    {
        pickedObject = objNetId.gameObject;
        Rigidbody rb = pickedObject.GetComponent<Rigidbody>();

        // Desactivamos físicas para que no se caiga ni colisione loco en la mano
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Lo emparentamos a la mano de ESTE personaje en la pantalla de todos
        pickedObject.transform.SetParent(handPoint.transform);
        pickedObject.transform.localPosition = Vector3.zero;
        pickedObject.transform.localRotation = Quaternion.identity;
    }

    [Command]
    private void CmdDropObject()
    {
        if (pickedObject != null)
        {
            RpcDropObject(); // El servidor ordena a todos soltarlo
        }
    }

    [ClientRpc]
    private void RpcDropObject()
    {
        if (pickedObject == null) return;

        Rigidbody rb = pickedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        pickedObject.transform.SetParent(null);
        pickedObject = null;
    }

    // Detectamos si entramos o salimos de la zona del objeto
    private void OnTriggerEnter(Collider other)
    {
        // Solo el jugador local necesita detectar la colisión para que le aparezca el prompt de coger
        if (!isLocalPlayer) return;

        if (other.CompareTag("Objets"))
        {
            objectInRange = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isLocalPlayer) return;

        if (other.CompareTag("Objets"))
        {
            if (objectInRange == other.gameObject)
            {
                objectInRange = null;
            }
        }
    }
}