using UnityEngine;

namespace HeliosDivide.Player
{
    [RequireComponent(typeof(CharacterController), typeof(HDPlayerInput), typeof(HDGroundedState))]
    public sealed class HDPlayerLocomotion : MonoBehaviour
    {
        [Header("Movement (metres / seconds)")]
        [SerializeField, Min(0f)] float walkSpeed = 4.5f;
        [SerializeField, Min(0f)] float sprintSpeed = 7f;
        [SerializeField, Min(0f)] float crouchSpeed = 2.5f;
        [SerializeField, Min(0.1f)] float acceleration = 22f;
        [SerializeField, Min(0.1f)] float deceleration = 26f;
        [SerializeField, Min(0f)] float jumpHeight = 1.2f;
        [SerializeField] float gravity = -20f;
        [SerializeField, Min(1f)] float terminalFallSpeed = 40f;
        [SerializeField] float groundedVerticalSpeed = -2f;
        [Header("Hold crouch (feet stay fixed)")]
        [SerializeField, Min(0.7f)] float standingHeight = 1.8f;
        [SerializeField, Min(0.7f)] float crouchingHeight = 1.1f;
        [SerializeField] LayerMask obstructionLayers = Physics.DefaultRaycastLayers;
        readonly Collider[] obstructions = new Collider[32];
        CharacterController controller;
        HDPlayerInput input;
        HDGroundedState ground;
        Vector3 horizontalVelocity;
        float verticalSpeed;

        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsGrounded => ground.IsGrounded;
        public Vector3 HorizontalVelocity => horizontalVelocity;
        public float VerticalSpeed => verticalSpeed;
        public float StandingHeight => standingHeight;
        public float CrouchingHeight => crouchingHeight;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<HDPlayerInput>();
            ground = GetComponent<HDGroundedState>();
        }

        void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            var wantsJump = input.ConsumeJump();
            // Short controller steps keep collision and grounding stable during slow frames.
            var remaining = Mathf.Min(deltaTime, 0.1f);
            while (remaining > 0.00001f)
            {
                var step = Mathf.Min(remaining, 0.02f);
                Step(step, wantsJump);
                wantsJump = false;
                remaining -= step;
            }
        }

        void Step(float deltaTime, bool wantsJump)
        {
            UpdateCrouch();
            ground.Refresh(verticalSpeed);
            var move = input.Move;
            IsSprinting = input.SprintHeld && !IsCrouching && move.sqrMagnitude > 0.01f;
            var speed = IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
            var desired = (transform.right * move.x + transform.forward * move.y) * speed;
            var rate = desired.sqrMagnitude < horizontalVelocity.sqrMagnitude ? deceleration : acceleration;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, rate * deltaTime);
            if (ground.IsGrounded && verticalSpeed < 0f) verticalSpeed = groundedVerticalSpeed;
            if (wantsJump && ground.IsGrounded && !IsCrouching)
                verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalSpeed = Mathf.Max(verticalSpeed + gravity * deltaTime, -terminalFallSpeed);
            var displacement = horizontalVelocity;
            if (ground.IsGrounded && verticalSpeed <= 0f)
            {
                // Follow walkable slopes without changing horizontal input magnitude.
                var normal = ground.Normal;
                if (normal.y > 0.01f)
                    displacement.y = -(normal.x * displacement.x + normal.z * displacement.z) / normal.y;
            }
            displacement.y += verticalSpeed;
            var flags = controller.Move(displacement * deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
            if ((flags & CollisionFlags.Below) != 0 && verticalSpeed < 0f) verticalSpeed = groundedVerticalSpeed;
            ground.Refresh(verticalSpeed);
        }

        void UpdateCrouch()
        {
            var crouch = input.CrouchHeld || (IsCrouching && !CanStand());
            if (crouch == IsCrouching) return;
            var height = crouch ? crouchingHeight : standingHeight;
            var feetOffset = controller.center.y - controller.height * 0.5f;
            controller.height = height;
            controller.center = new Vector3(controller.center.x, feetOffset + height * 0.5f, controller.center.z);
            IsCrouching = crouch;
        }

        public bool CanStand()
        {
            var feet = transform.position + controller.center - Vector3.up * (controller.height * 0.5f);
            var radius = Mathf.Max(0.05f, controller.radius - controller.skinWidth);
            var count = Physics.OverlapCapsuleNonAlloc(
                feet + Vector3.up * (radius + 0.06f),
                feet + Vector3.up * (standingHeight - radius), radius,
                obstructions, obstructionLayers, QueryTriggerInteraction.Ignore);
            if (count == obstructions.Length) return false;
            for (var i = 0; i < count; i++)
                if (obstructions[i] != controller && !obstructions[i].transform.IsChildOf(transform)) return false;
            return true;
        }

        void OnValidate()
        {
            gravity = Mathf.Min(-0.1f, gravity);
            groundedVerticalSpeed = Mathf.Min(-0.1f, groundedVerticalSpeed);
            crouchingHeight = Mathf.Min(crouchingHeight, standingHeight);
        }
    }
}
