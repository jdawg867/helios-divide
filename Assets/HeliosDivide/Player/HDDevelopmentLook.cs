using System;
using UnityEngine;

namespace HeliosDivide.Player
{
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(HDPlayerInput), typeof(HDPlayerLocomotion))]
    public sealed class HDDevelopmentLook : MonoBehaviour
    {
        [SerializeField] Transform cameraPivot;
        [SerializeField, Min(0f)] float mouseSensitivity = 0.12f;
        [SerializeField, Min(0f)] float controllerSensitivity = 150f;
        [SerializeField, Range(1f, 89f)] float pitchLimit = 85f;
        [SerializeField] float standingEyeHeight = 1.62f;
        [SerializeField] float crouchingEyeHeight = 0.92f;
        HDPlayerInput input;
        HDPlayerLocomotion locomotion;
        float pitch;
        CursorLockMode previousLock;
        bool previousVisible;
        public float Pitch => pitch;
        public Transform CameraPivot => cameraPivot;

        void Awake()
        {
            input = GetComponent<HDPlayerInput>();
            locomotion = GetComponent<HDPlayerLocomotion>();
            if (cameraPivot == null) throw new InvalidOperationException("HDDevelopmentLook requires a camera pivot.");
        }

        void OnEnable()
        {
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            CaptureCursor();
        }

        void OnDisable()
        {
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
        }

        void OnApplicationFocus(bool focused)
        {
            if (focused && enabled) CaptureCursor();
        }

        public void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            var look = input.Look;
            var factor = input.LookIsRate ? controllerSensitivity * deltaTime : mouseSensitivity;
            transform.Rotate(0f, look.x * factor, 0f, Space.World);
            pitch = Mathf.Clamp(pitch - look.y * factor, -pitchLimit, pitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void LateUpdate()
        {
            var local = cameraPivot.localPosition;
            local.y = locomotion.IsCrouching ? crouchingEyeHeight : standingEyeHeight;
            cameraPivot.localPosition = local;
        }
    }
}
