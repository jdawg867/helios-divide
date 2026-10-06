using UnityEngine;

namespace HeliosDivide.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class HDGroundedState : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] float probeDistance = 0.1f;
        [SerializeField] LayerMask groundLayers = Physics.DefaultRaycastLayers;
        readonly RaycastHit[] hits = new RaycastHit[16];
        CharacterController controller;
        public bool IsGrounded { get; private set; }
        public Vector3 Normal { get; private set; } = Vector3.up;

        void Awake() => controller = GetComponent<CharacterController>();

        public void Refresh(float verticalSpeed)
        {
            IsGrounded = false;
            Normal = Vector3.up;
            if (verticalSpeed > 0f) return;
            var radius = Mathf.Max(0.05f, controller.radius - controller.skinWidth);
            var feet = transform.position + controller.center - Vector3.up * (controller.height * 0.5f);
            var origin = feet + Vector3.up * (radius + 0.12f);
            var count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, hits,
                probeDistance + 0.12f, groundLayers, QueryTriggerInteraction.Ignore);
            var minimumUp = Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);
            for (var i = 0; i < count; i++)
            {
                if (hits[i].collider == controller || hits[i].transform.IsChildOf(transform)) continue;
                if (Vector3.Dot(hits[i].normal, Vector3.up) < minimumUp) continue;
                IsGrounded = true;
                Normal = hits[i].normal;
                return;
            }
            IsGrounded = controller.isGrounded;
        }
    }
}
