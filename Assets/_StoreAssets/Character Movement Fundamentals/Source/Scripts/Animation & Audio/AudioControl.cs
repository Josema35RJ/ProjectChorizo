using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace CMF
{
    [RequireComponent(typeof(Controller))]
    [RequireComponent(typeof(NetworkIdentity))]
    // Quitamos el RequireComponent de AudioSource para manejarlo nosotros manualmente y evitar errores
    public class AudioControl : NetworkBehaviour
    {
        private Controller controller;
        private AudioSource audioSource;
        private Transform tr;

        [Header("Configuración de Pasos")]
        public float velocityThreshold = 0.1f;
        public float footstepDistance = 2.5f;
        private float distanceTraveled = 0f;

        [Header("Clips de Audio")]
        public AudioClip[] footstepClips;
        public AudioClip jumpClip;
        public AudioClip landClip;

        [Header("Ajustes de Red")]
        [Range(0, 1)] public float spatialBlend3D = 1.0f;

        private void Awake()
        {
            controller = GetComponent<Controller>();
            tr = transform;

            // Intentamos obtener el AudioSource
            audioSource = GetComponent<AudioSource>();

            // Si no existe, lo añadimos dinámicamente para evitar el MissingComponentException
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.LogWarning($"[AudioControl] No se encontró AudioSource en {gameObject.name}. Se ha añadido uno automáticamente.");
            }

            // Configuración segura
            if (audioSource != null)
            {
                audioSource.spatialBlend = spatialBlend3D;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 20f;
                audioSource.playOnAwake = false;
            }
        }

        private void OnEnable()
        {
            // Verificación de nulidad para evitar errores si el inicio falló
            if (controller != null)
            {
                controller.OnLand += HandleOnLand;
                controller.OnJump += HandleOnJump;
            }
        }

        private void OnDisable()
        {
            // Verificación de nulidad CRÍTICA para evitar el segundo error que mencionaste
            if (controller != null)
            {
                controller.OnLand -= HandleOnLand;
                controller.OnJump -= HandleOnJump;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer || !NetworkClient.ready || controller == null) return;

            Vector3 velocity = controller.GetVelocity();
            Vector3 horizontalVelocity = VectorMath.RemoveDotVector(velocity, tr.up);

            if (controller.IsGrounded() && horizontalVelocity.magnitude > velocityThreshold)
            {
                distanceTraveled += horizontalVelocity.magnitude * Time.deltaTime;
                if (distanceTraveled > footstepDistance)
                {
                    distanceTraveled = 0f;
                    RequestFootstepSound();
                }
            }
        }

        private void RequestFootstepSound()
        {
            if (footstepClips.Length == 0) return;
            int index = Random.Range(0, footstepClips.Length);

            PlayLocalSound(footstepClips[index], 0.8f);
            CmdPlayFootstep(index);
        }

        [Command]
        private void CmdPlayFootstep(int clipIndex) => RpcPlayFootstep(clipIndex);

        [ClientRpc(includeOwner = false)]
        private void RpcPlayFootstep(int clipIndex)
        {
            if (clipIndex >= 0 && clipIndex < footstepClips.Length)
                PlayLocalSound(footstepClips[clipIndex], 0.6f);
        }

        private void HandleOnJump(Vector3 velocity)
        {
            if (!isLocalPlayer) return;
            PlayLocalSound(jumpClip, 1.0f);
            CmdPlayActionSound("jump");
        }

        private void HandleOnLand(Vector3 velocity)
        {
            if (!isLocalPlayer) return;
            if (VectorMath.GetDotProduct(velocity, tr.up) > -5f) return;

            PlayLocalSound(landClip, 1.0f);
            CmdPlayActionSound("land");
        }

        [Command]
        private void CmdPlayActionSound(string type) => RpcPlayActionSound(type);

        [ClientRpc(includeOwner = false)]
        private void RpcPlayActionSound(string type)
        {
            switch (type)
            {
                case "jump": PlayLocalSound(jumpClip, 0.8f); break;
                case "land": PlayLocalSound(landClip, 0.8f); break;
            }
        }

        private void PlayLocalSound(AudioClip clip, float volume)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, volume);
        }
    }
}