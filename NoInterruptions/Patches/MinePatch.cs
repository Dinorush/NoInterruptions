using HarmonyLib;

namespace NoInterruptions.Patches
{
    [HarmonyPatch(typeof(MineDeployerFirstPerson))]
    internal static class MinePatch
    {
        [HarmonyPatch(nameof(MineDeployerFirstPerson.OnStickyMineSpawned))]
        [HarmonyPostfix]
        private static void OnStickyMineSpawned(ISyncedItem item)
        {
            MineDeployerInstance mineDeployerInstance = item.Cast<MineDeployerInstance>();
            if (mineDeployerInstance != null)
                mineDeployerInstance.PickupInteraction.transform.Translate(0, 0, -0.01f);
        }
    }
}
