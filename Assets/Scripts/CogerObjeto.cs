using UnityEngine;

public class CogerObjeto : MonoBehaviour
{
    public GameObject handPoint;
    private GameObject pickedObject = null;
    private GameObject objectInRange = null; // Guardamos el objeto que estamos tocando

    void Update()
    {
        // SOLTAR OBJETO
        if (pickedObject != null && Input.GetKeyDown(KeyCode.Q))
        {
            DropObject();
        }

        // COGER OBJETO
        // Usamos GetKeyDown para que solo detecte un pulso al presionar
        if (objectInRange != null && pickedObject == null && Input.GetKeyDown(KeyCode.E))
        {
            PickUpObject(objectInRange);
        }
    }

    private void PickUpObject(GameObject obj)
    {
        pickedObject = obj;
        Rigidbody rb = pickedObject.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        pickedObject.transform.SetParent(handPoint.transform);
        pickedObject.transform.localPosition = Vector3.zero;
        pickedObject.transform.localRotation = Quaternion.identity;
    }

    private void DropObject()
    {
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
        if (other.CompareTag("Objets"))
        {
            objectInRange = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Objets"))
        {
            objectInRange = null;
        }
    }
}