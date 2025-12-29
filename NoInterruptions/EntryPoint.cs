using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace NoInterruptions
{
    [BepInPlugin(MODNAME, MODNAME, "0.1.7")]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "NoInterruptions";

        public override void Load()
        {
            new Harmony(MODNAME).PatchAll();
            Log.LogMessage("Loaded " + MODNAME);
        }
    }
}