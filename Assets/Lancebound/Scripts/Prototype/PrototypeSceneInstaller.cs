using UnityEngine;

namespace Lancebound.Prototype
{
    /// <summary>Creates the prototype automatically even when the startup scene is empty.</summary>
    public static class PrototypeSceneInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Object.FindFirstObjectByType<PrototypeArenaController>() != null)
                return;

            new GameObject("Lancebound Prototype").AddComponent<PrototypeArenaController>();
        }
    }
}
