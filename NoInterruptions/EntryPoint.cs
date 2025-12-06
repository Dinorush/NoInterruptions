using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace NoInterruptions
{
    [BepInPlugin("hirnukuono." + MODNAME, MODNAME, "0.1.5")]
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