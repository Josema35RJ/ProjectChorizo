using UnityEngine;
using Mirror;

public class CogerObjeto : NetworkBehaviour
{
    public GameObject handPoint;
    private GameObject pickedObject = null;
    private GameObject objectInRange = null;

    void Update()
    {
        if (!isLocalPlayer) return;

        // SOLTAR OBJETO
        if (pickedObject != null && Input.GetKeyDown(KeyCode.Q))
        {
            NetworkIdentity netId = pickedObject.GetComponent<NetworkIdentity>();
            if (netId != null)
            {
                CmdDropObject(netId);
            }
        }

        // COGER OBJETO
        if (objectInRange != null && pickedObject == null && Input.GetKeyDown(KeyCode.E))
        {
            NetworkIdentity netId = objectInRange.GetComponent<NetworkIdentity>();
            
            // Solo intentamos cogerlo si nadie más tiene la autoridad (nadie más lo tiene en la mano)
            if (netId != null && netId.connectionToClient == null)
            {
                CmdPickUpObject(netId);
            }
        }
    }

    [Command]
    private void CmdPickUpObject(NetworkIdentity objNetId)
    {
        // 1. Le damos AUTORIDAD de red a este jugador para que sus físicas manden
        objNetId.AssignClientAuthority(connectionToClient);
        
        // 2. Avisamos a todos para que lo emparenten a la mano
        RpcPickUpObject(objNetId);
    }

    [ClientRpc]
    private void RpcPickUpObject(NetworkIdentity objNetId)
    {
        pickedObject = objNetId.gameObject;
        Rigidbody rb = pickedObject.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;  // Apagamos físicas en la mano
            rb.useGravity = false;
        }

        pickedObject.transform.SetParent(handPoint.transform);
        pickedObject.transform.localPosition = Vector3.zero;
        pickedObject.transform.localRotation = Quaternion.identity;
    }

    [Command]
    private void CmdDropObject(NetworkIdentity objNetId)
    {
        // 1. Avisamos a todos de que lo suelten y enciendan las físicas
        RpcDropObject(objNetId);

        // 2. Le QUITAMOS la autoridad al jugador para que el Servidor vuelva a calcular la caída
        objNetId.RemoveClientAuthority();
    }

    [ClientRpc]
    private void RpcDropObject(NetworkIdentity objNetId)
    {
        if (objNetId == null) return;

        GameObject objToDrop = objNetId.gameObject;
        Rigidbody rb = objToDrop.GetComponent<Rigidbody>();
        
        // ¡Aquí recupera las físicas en todas las pantallas!
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        objToDrop.transform.SetParent(null);

        // Limpiamos la variable solo para el jugador que lo tenía agarrado
        if (pickedObject == objToDrop)
        {
            pickedObject = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isLocalPlayer) return;

        if (other.CompareTag("Objets"))
        {
            objectInRange = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isLocalPlayer) return;

        if (other.CompareTag("Objets") && objectInRange == other.gameObject)
        {
            objectInRange = null;
        }
    }
}