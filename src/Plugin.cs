using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace BetterCrafting
{
    [BepInPlugin(PluginGuid, "Better Crafting", "1.1.1")]
    [BepInProcess("SurvivalLog.exe")]
    [BepInDependency(AnyStorageFeature.BaseButlerGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BasePlugin
    {
        public const string PluginGuid = "com.ivmakk.survivallog.bettercrafting";

        internal static new ManualLogSource Log;
        internal static Harmony Harmony;
        internal static ConfigEntry<bool> Verbose;
        internal static ConfigEntry<bool> CountCache;

        private static readonly HashSet<string> warned = new HashSet<string>();

        public override void Load()
        {
            Log = base.Log;
            Verbose = Config.Bind(
                "General", "Verbose", false,
                "Log each scan of the home storage and each count of the recipe list of Better Crafting at Debug level. Keep off in normal play.");
            CountCache = Config.Bind(
                "Performance", "CountCache", true,
                "Count the materials of the workbench places once for each refresh of the recipe list, so a craft stays fast with much home storage. " +
                "Set to false to count as Better Crafting 1.1.0 does, if a count of the recipe list looks wrong. A change applies at the next start of the game.");
            Harmony = new Harmony(PluginGuid);
            AnyStorageFeature.Patch(Harmony);
            Log.LogInfo("Better Crafting loaded.");
        }

        // One warning for each distinct error, so an error on each click does not flood the log.
        internal static void WarnOnce(string where, Exception e, string fallback = "The game's own fill stays.")
        {
            string text = where + ": " + e.GetType().Name + ": " + e.Message;
            if (warned.Add(text)) Log.LogWarning(text + ". " + fallback + "\n" + e);
        }

        internal static void Debug(string line)
        {
            if (Verbose.Value) Log.LogDebug(line);
        }
    }
}
