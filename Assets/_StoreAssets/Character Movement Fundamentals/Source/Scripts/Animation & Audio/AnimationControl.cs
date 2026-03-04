using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace CMF
{
    [RequireComponent(typeof(Controller))]
    [RequireComponent(typeof(NetworkIdentity))]
    public class AnimationControl : NetworkBehaviour
    {
        private Controller controller;
        private Transform tr;
        private Transform animatorTransform;

        [Header("Componentes")]
        public Animator animator;
        public NetworkAnimator nAnimator;

        [Header("Configuración")]
        public bool useStrafeAnimations = false;
        public float landVelocityThreshold = 5f;
        [Range(10f, 60f)] public float smoothingFactor = 40f;

        [Header("Habilidades")]
        public float dashCooldown = 1.2f;
        private float lastDashTime;

        private Vector3 oldMovementVelocity = Vector3.zero;

        // --- Cache de existencia de parámetros ---
        private bool hasVerticalSpeed, hasHorizontalSpeed, hasIsGrounded, hasIsRunning, hasDash, hasJump, hasForward, hasStrafe;

        // --- Hashes de los parámetros ---
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
        private static readonly int HorizontalSpeedHash = Animator.StringToHash("HorizontalSpeed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
        private static readonly int ForwardSpeedHash = Animator.StringToHash("ForwardSpeed");
        private static readonly int StrafeSpeedHash = Animator.StringToHash("StrafeSpeed");
        private static readonly int DashTriggerHash = Animator.StringToHash("Dash");
        private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");

        private void Awake()
        {
            controller = GetComponent<Controller>();
            tr = transform;
            if (animator != null)
            {
                animatorTransform = animator.transform;
                CheckParameters(); // Validamos qué parámetros existen realmente
            }
        }

        private void CheckParameters()
        {
            if (animator == null) return;

            // Revisamos el Animator una sola vez al inicio
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == "VerticalSpeed") hasVerticalSpeed = true;
                if (p.name == "HorizontalSpeed") hasHorizontalSpeed = true;
                if (p.name == "IsGrounded") hasIsGrounded = true;
                if (p.name == "IsRunning") hasIsRunning = true;
                if (p.name == "Dash") hasDash = true;
                if (p.name == "Jump") hasJump = true;
                if (p.name == "ForwardSpeed") hasForward = true;
                if (p.name == "StrafeSpeed") hasStrafe = true;
            }
        }

        private void OnEnable()
        {
            controller.OnLand += HandleOnLand;
            controller.OnJump += HandleOnJump;
        }

        private void OnDisable()
        {
            controller.OnLand -= HandleOnLand;
            controller.OnJump -= HandleOnJump;
        }

        private void Update()
        {
            if (!isLocalPlayer || !NetworkClient.ready || animator == null) return;

            // 1. INPUT
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            if (Input.GetKeyDown(KeyCode.E) && Time.time >= lastDashTime + dashCooldown) PerformDash();

            // 2. CÁLCULOS FÍSICOS
            Vector3 currentVelocity = controller.GetVelocity();
            Vector3 horizontalVelocity = VectorMath.RemoveDotVector(currentVelocity, tr.up);
            Vector3 verticalVelocity = currentVelocity - horizontalVelocity;

            horizontalVelocity = Vector3.Lerp(oldMovementVelocity, horizontalVelocity, smoothingFactor * Time.deltaTime);
            oldMovementVelocity = horizontalVelocity;

            float vSpeed = verticalVelocity.magnitude * VectorMath.GetDotProduct(verticalVelocity.normalized, tr.up);
            float hSpeed = horizontalVelocity.magnitude;
            bool grounded = controller.IsGrounded();

            // 3. ACTUALIZACIÓN LOCAL Y RED
            ApplyAnimationState(vSpeed, hSpeed, grounded, isRunning, horizontalVelocity);
            CmdSyncState(vSpeed, hSpeed, grounded, isRunning);
        }

        private void ApplyAnimationState(float v, float h, bool g, bool r, Vector3 hVel)
        {
            // Solo aplicamos si el parámetro existe (evita el error de Hash)
            if (hasVerticalSpeed) animator.SetFloat(VerticalSpeedHash, v);
            if (hasHorizontalSpeed) animator.SetFloat(HorizontalSpeedHash, h);
            if (hasIsGrounded) animator.SetBool(IsGroundedHash, g);
            if (hasIsRunning) animator.SetBool(IsRunningHash, r);

            if (useStrafeAnimations)
            {
                Vector3 localVel = animatorTransform.InverseTransformVector(hVel);
                if (hasForward) animator.SetFloat(ForwardSpeedHash, localVel.z);
                if (hasStrafe) animator.SetFloat(StrafeSpeedHash, localVel.x);
            }
        }

        [Command]
        private void CmdSyncState(float v, float h, bool g, bool r)
        {
            // El servidor replica a los demás
            if (hasVerticalSpeed) animator.SetFloat(VerticalSpeedHash, v);
            if (hasHorizontalSpeed) animator.SetFloat(HorizontalSpeedHash, h);
            if (hasIsGrounded) animator.SetBool(IsGroundedHash, g);
            if (hasIsRunning) animator.SetBool(IsRunningHash, r);
        }

        private void PerformDash()
        {
            lastDashTime = Time.time;
            if (hasDash) animator.SetTrigger(DashTriggerHash);
            if (nAnimator != null) nAnimator.SetTrigger("Dash");
        }

        private void HandleOnJump(Vector3 v)
        {
            if (!isLocalPlayer) return;
            if (hasJump) animator.SetTrigger(JumpTriggerHash);
            if (nAnimator != null) nAnimator.SetTrigger("Jump");
        }

        private void HandleOnLand(Vector3 v)
        {
            if (!isLocalPlayer) return;
            if (VectorMath.GetDotProduct(v, tr.up) > -landVelocityThreshold) return;

            // Trigger por defecto de CMF
            animator.SetTrigger("OnLand");
        }
    }
}