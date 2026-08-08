using BepInEx;
using SextantHorison.Utils;
using HarmonyLib;
using System.Reflection;

namespace OpenLockerLib
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public new static Mercury Logger { get; private set; }

        private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        private void Awake()
        {
            // plugin startup logic
            Logger = new Mercury(PluginInfo.PLUGIN_NAME);
            Resources.ResourceCount = Compatibility.ResourceCount;
            // register harmony patches, if there are any
            Harmony.CreateAndPatchAll(Assembly, $"{PluginInfo.PLUGIN_GUID}");
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
