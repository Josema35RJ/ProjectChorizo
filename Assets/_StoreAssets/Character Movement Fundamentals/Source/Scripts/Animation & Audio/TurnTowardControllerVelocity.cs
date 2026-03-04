using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMF
{
    // Este script mantiene el movimiento original de CMF pero previene el error NullReferenceException
    public class TurnTowardControllerVelocity : MonoBehaviour
    {

        // Target controller;
        public Controller controller;

        // Speed at which this gameobject turns toward the controller's velocity;
        public float turnSpeed = 500f;

        Transform parentTransform;
        Transform tr;

        // Current (local) rotation around the (local) y axis of this gameobject;
        float currentYRotation = 0f;

        // Smoothing effect;
        float fallOffAngle = 90f;

        // Whether the current controller momentum should be ignored;
        public bool ignoreControllerMomentum = false;

        // Setup;
        void Start()
        {
            tr = transform;
            parentTransform = tr.parent;

            // INTENTO DE AUTO-ASIGNACIÓN:
            // Si el controlador es nulo, intenta buscarlo en el objeto o en el padre
            // Esto evita que tengas que asignarlo a mano tras borrar componentes
            if (controller == null)
            {
                controller = GetComponentInParent<Controller>();
            }

            if (controller == null)
            {
                Debug.LogWarning("No se ha asignado un 'Controller' a " + gameObject.name + ". El script se desactivará para evitar errores.", this);
                this.enabled = false;
            }
        }

        void LateUpdate()
        {

            // ESCUDO DE SEGURIDAD:
            // Si el controlador es nulo (por red o carga de escena), salimos antes de que ocurra el error
            if (controller == null) return;

            // Get controller velocity;
            Vector3 _velocity;
            if (ignoreControllerMomentum)
                _velocity = controller.GetMovementVelocity();
            else
                _velocity = controller.GetVelocity();

            // Project velocity onto a plane defined by the 'up' direction of the parent transform;
            // Usamos Vector3.up como respaldo si no hay padre
            Vector3 _upVector = (parentTransform != null) ? parentTransform.up : Vector3.up;
            _velocity = Vector3.ProjectOnPlane(_velocity, _upVector);

            float _magnitudeThreshold = 0.001f;

            // If the velocity's magnitude is smaller than the threshold, return;
            if (_velocity.magnitude < _magnitudeThreshold)
                return;

            // Normalize velocity direction;
            _velocity.Normalize();

            // Get current 'forward' vector;
            Vector3 _currentForward = tr.forward;

            // Calculate (signed) angle between velocity and forward direction;
            float _angleDifference = VectorMath.GetAngle(_currentForward, _velocity, _upVector);

            // Calculate angle factor (Smoothing original);
            float _factor = Mathf.InverseLerp(0f, fallOffAngle, Mathf.Abs(_angleDifference));

            // Calculate this frame's step;
            float _step = Mathf.Sign(_angleDifference) * _factor * Time.deltaTime * turnSpeed;

            // Clamp step;
            if (_angleDifference < 0f && _step < _angleDifference)
                _step = _angleDifference;
            else if (_angleDifference > 0f && _step > _angleDifference)
                _step = _angleDifference;

            // Add step to current y angle;
            currentYRotation += _step;

            // Clamp y angle;
            if (currentYRotation > 360f)
                currentYRotation -= 360f;
            if (currentYRotation < -360f)
                currentYRotation += 360f;

            // Set transform rotation using Quaternion.Euler;
            tr.localRotation = Quaternion.Euler(0f, currentYRotation, 0f);

        }

        void OnDisable()
        {
        }

        void OnEnable()
        {
            currentYRotation = transform.localEulerAngles.y;
        }
    }
}