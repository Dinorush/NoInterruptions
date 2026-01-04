using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using NoInterruptions.Patches;

namespace NoInterruptions
{
    [BepInPlugin(MODNAME, MODNAME, "0.1.9")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "NoInterruptions";

        public override void Load()
        {
            GTFO.API.LevelAPI.OnLevelCleanup += CommandPatch.OnCleanup;
            new Harmony(MODNAME).PatchAll();
            Log.LogMessage("Loaded " + MODNAME);
        }
    }
}