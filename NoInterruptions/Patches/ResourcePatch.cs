using HarmonyLib;
using LevelGeneration;

namespace NoInterruptions.Patches
{
    [HarmonyPatch(typeof(Gear.ResourcePackPickup))]
    internal static class PickupPatches
    {
        [HarmonyPatch(nameof(Gear.ResourcePackPickup.OnSyncStateChange))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void FixDroppedPackArea(Gear.ResourcePackPickup __instance, ePickupItemStatus status, ref pPickupPlacement placement)
        {
            if (status == ePickupItemStatus.PlacedInLevel && placement.node.TryGet(out var courseNode))
            {
                __instance.m_terminalItem.SpawnNode = courseNode;
                __instance.m_terminalItem.FloorItemLocation = courseNode.m_zone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore);
            }
        }
    }
}
