using HarmonyLib;
using System.Collections.Generic;

namespace OpenLockerAPI
{
    [HarmonyPatch]
    internal class Hooks
    {
        public static List<StorageContainer> containers = new();

        [HarmonyPatch(typeof(StorageContainer))]
        [HarmonyPatch(nameof(StorageContainer.Awake))]
        [HarmonyPostfix]
        public static void Awake(StorageContainer __instance)
        {
            containers.Add(__instance);
        }
    }
}
