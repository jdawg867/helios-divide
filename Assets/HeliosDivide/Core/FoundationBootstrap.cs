using UnityEngine;

namespace HeliosDivide
{
    /// <summary>
    /// Scene-level startup probe for the foundation test map.
    /// Does not implement gameplay systems.
    /// </summary>
    public sealed class FoundationBootstrap : MonoBehaviour
    {
        [SerializeField] Transform playerSpawn;

        void Awake()
        {
            if (playerSpawn == null)
            {
                HDLog.Error("FoundationTest is missing a marked player spawn.", this);
                return;
            }

            HDLog.Info($"FoundationTest ready. Spawn at {playerSpawn.position}.", this);
        }
    }
}
