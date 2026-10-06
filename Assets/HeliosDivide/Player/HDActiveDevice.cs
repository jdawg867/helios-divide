using UnityEngine;
using UnityEngine.InputSystem;

namespace HeliosDivide.Player
{
    public enum HDInputDeviceCategory { KeyboardMouse, Gamepad }

    public sealed class HDActiveDevice : MonoBehaviour
    {
        public HDInputDeviceCategory Category { get; private set; } = HDInputDeviceCategory.KeyboardMouse;

        public void Observe(InputDevice device)
        {
            if (device is Gamepad) Category = HDInputDeviceCategory.Gamepad;
            else if (device is Keyboard || device is Mouse) Category = HDInputDeviceCategory.KeyboardMouse;
        }
    }
}
