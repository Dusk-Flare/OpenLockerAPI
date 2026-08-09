using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenLockerLib
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
