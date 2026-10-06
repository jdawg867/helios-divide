using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeliosDivide.Player
{
    [RequireComponent(typeof(HDActiveDevice))]
    public sealed class HDPlayerInput : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActions;
        InputActionAsset runtimeActions;
        InputAction move, look, jump, sprint, crouch;
        HDActiveDevice activeDevice;
        bool jumpQueued;

        public InputActionAsset SourceAsset => inputActions;
        public Vector2 Move => move == null ? Vector2.zero : Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        public Vector2 Look => look == null ? Vector2.zero : look.ReadValue<Vector2>();
        public bool LookIsRate => look != null && look.activeControl != null && look.activeControl.device is Gamepad;
        public bool SprintHeld => sprint != null && sprint.IsPressed();
        public bool CrouchHeld => crouch != null && crouch.IsPressed();
        public HDActiveDevice ActiveDevice => activeDevice;

        void Awake()
        {
            if (inputActions == null) throw new InvalidOperationException("HDPlayerInput requires HeliosDivideInput.");
            activeDevice = GetComponent<HDActiveDevice>();
            runtimeActions = Instantiate(inputActions);
            var map = runtimeActions.FindActionMap("Player", true);
            move = map.FindAction("Move", true);
            look = map.FindAction("Look", true);
            jump = map.FindAction("Jump", true);
            sprint = map.FindAction("Sprint", true);
            crouch = map.FindAction("Crouch", true);
            map.actionTriggered += OnAction;
        }

        void OnEnable()
        {
            move.Enable(); look.Enable(); jump.Enable(); sprint.Enable(); crouch.Enable();
        }

        void OnDisable()
        {
            runtimeActions.Disable();
            jumpQueued = false;
        }

        void OnDestroy()
        {
            if (runtimeActions != null) Destroy(runtimeActions);
        }

        void OnAction(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            activeDevice.Observe(context.control.device);
            if (context.action == jump) jumpQueued = true;
        }

        public bool ConsumeJump()
        {
            var pressed = jumpQueued;
            jumpQueued = false;
            return pressed;
        }
    }
}
