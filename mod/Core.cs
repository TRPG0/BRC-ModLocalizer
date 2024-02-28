using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Reptile;
using System.Collections.Generic;

namespace ModLocalizer
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Core : BaseUnityPlugin
    {
        public const string PluginGUID = "trpg.brc.modlocalizer";
        public const string PluginName = "ModLocalizer";
        public const string PluginVersion = "1.0.1";

        internal static new ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ModLocalizer");

        internal static Core Instance { get; private set; }
        internal List<PluginLocalizer> Localizers = new List<PluginLocalizer>();

        private void Awake()
        {
            if (Instance != null) Destroy(this);
            Instance = this;

            Harmony Harmony = new Harmony("ModLocalizer");
            Harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(UIManager), "InstantiateMainMenuSceneUI")]
    internal class UIManager_InstantiateMainMenuSceneUI_Patch
    {
        public static void Postfix()
        {
            for (int i = 0; i < Core.Instance.Localizers.Count; i++)
            {
                if (Core.Instance.Localizers[i] == null) Core.Instance.Localizers.RemoveAt(i);
                Core.Instance.Localizers[i].Initialize();
            }
        }
    }

    [HarmonyPatch(typeof(OptionsMenuGameTab), "ApplyLanguage")]
    internal class OptionsMenuGameTab_ApplyLanguage_Patch
    {
        public static void Postfix()
        {
            for (int i = 0; i < Core.Instance.Localizers.Count; i++)
            {
                if (Core.Instance.Localizers[i] == null) Core.Instance.Localizers.RemoveAt(i);
                Core.Instance.Localizers[i].UpdateLocalization(Reptile.Core.Instance.Localizer.Language);
            }
        }
    }
}
