using Gear;
using HarmonyLib;
using Player;
using UnityEngine;

namespace NoInterruptions
{
    [HarmonyPatch]
    internal static class InteractionPatch
    {
        [HarmonyPatch(typeof(Interact_Timed), nameof(Interact_Timed.CheckSoundPlayer))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Post_CheckSoundPlayer(Interact_Timed __instance)
        {
            if (__instance.m_sound == null && __instance.transform == null)
            {
                __instance.m_sound = new(PlayerManager.GetLocalPlayerAgent().transform.position);
                return false;
            }
            return true;
        }

        private static Interact_Timed? _cachedPack;
        private static (Interact_Timed drop, Interact_Timed insert)? _cachedPickup;
        private static Interact_Timed? _cachedInteract;
        private static RaycastHit _rayHitInfo;

        [HarmonyPatch(typeof(PlayerInteraction), nameof(PlayerInteraction.FixedUpdate))]
        [HarmonyPrefix]
        private static void Pre_FixedUpdate(PlayerInteraction __instance)
        {
            var inventory = __instance.m_owner.Inventory;
            if (IsPackOverriding(inventory) || IsPickupOverriding(inventory))
            {
                _cachedInteract = null;
                SetInteractionEnabled(false);
                return;
            }

            _cachedInteract = __instance.m_bestSelectedInteract?.TryCast<Interact_Timed>();

            if (_cachedInteract != null && inventory.WieldedItem?.AllowPlayerInteraction == true && InteractIsActive(__instance))
            {
                SetInteractionEnabled(false);
            }
            else
            {
                _cachedInteract = null;
                SetInteractionEnabled(true);
            }
        }

        private static bool IsPackOverriding(PlayerInventoryBase inventory)
        {
            var wielded = inventory.WieldedSlot;
            if (wielded != InventorySlot.ResourcePack)
            {
                _cachedPack = null;
                return false;
            }
            else if (_cachedPack == null)
                _cachedPack = inventory.WieldedItem.Cast<ResourcePackFirstPerson>().m_interactApplyResource;

            if (_cachedPack != null && _cachedPack.TimerIsActive)
                return true;
            return false;
        }

        private static bool IsPickupOverriding(PlayerInventoryBase inventory)
        {
            var wielded = inventory.WieldedSlot;
            if (wielded != InventorySlot.InLevelCarry)
            {
                _cachedPickup = null;
                return false;
            }
            else if (_cachedPickup == null)
            {
                var item = inventory.WieldedItem.Cast<CarryItemEquippableFirstPerson>();
                _cachedPickup = (item.m_interactDropItem, item.m_interactInsertItem);
            }

            if (_cachedPickup != null)
            {
                (var drop, var insert) = _cachedPickup.Value;
                if (drop.TimerIsActive || insert.TimerIsActive)
                    return true;
            }
            return false;
        }

        private static void SetInteractionEnabled(bool enabled)
        {
            PlayerInteraction.CameraRayInteractionEnabled = enabled;
            PlayerInteraction.SphereCheckInteractionEnabled = enabled;
            PlayerInteraction.LadderInteractionEnabled = enabled;
        }

        [HarmonyPatch(typeof(PlayerInteraction), nameof(PlayerInteraction.FixedUpdate))]
        [HarmonyPostfix]
        private static void Post_FixedUpdate(PlayerInteraction __instance)
        {
            __instance.m_bestInteractInCurrentSearch = _cachedInteract;
        }

        private static bool InteractIsActive(PlayerInteraction __instance)
        {
            if (_cachedInteract == null || !_cachedInteract.TimerIsActive) return false;

            if (!_cachedInteract.IsActive || !_cachedInteract.PlayerCanInteract(__instance.m_owner)) return false;

            Vector3 camPos = __instance.m_owner.CamPos;
            float searchRadius = __instance.m_searchRadius + Mathf.Min(Mathf.Abs(__instance.m_owner.TargetLookDir.y), 0.5f);
            float sqRadius = searchRadius * searchRadius;

            // Make sure the cached interact is still in range
            bool inRange = false;
            foreach(Collider collider in _cachedInteract.gameObject.GetComponentsInChildren<Collider>())
            {
                float dist = Vector3.SqrMagnitude(collider.transform.position - camPos);
                if (dist <= sqRadius)
                {
                    inRange = true;
                    break;
                }
            }

            Vector3 position = _cachedInteract.transform.position;
            Vector3 diff = position - camPos;
            if (diff.y < 2f)
                diff.y = 0;

            // In R6Mono, this only runs if the camera ray failed, but should be safe to put it here
            if (diff.sqrMagnitude <= __instance.m_proximityRadius * __instance.m_proximityRadius)
                __instance.AddToProximity(_cachedInteract);
            else
                __instance.RemoveFromProximity(_cachedInteract);

            if (!inRange) return false;

            FPSCamera camera = __instance.m_owner.FPSCamera;

            // Already looking at it even including blockers
            if (camera.CameraRayObject == _cachedInteract.gameObject) return true;
                
            // Check that looking at the object ignoring blockers
            if (Physics.Raycast(camera.m_camRay, out _rayHitInfo, searchRadius, LayerManager.MASK_PLAYER_INTERACT_SPHERE)
             && _rayHitInfo.collider.gameObject == _cachedInteract.gameObject) return true;

            if (_cachedInteract.OnlyActiveWhenLookingStraightAt) return false;

            // Check that it's on the screen
            Vector3 screenVector = camera.m_camera.WorldToScreenPoint(position);
            if (screenVector.z <= 0f || !GuiManager.IsOnScreen(screenVector)) return false;

            if (_cachedInteract.RequireCollisionCheck && Physics.Raycast(camPos, diff.normalized, out _rayHitInfo, diff.magnitude, LayerManager.MASK_PLAYER_INTERACT_SPHERE))
                return _rayHitInfo.collider.gameObject == _cachedInteract.gameObject;

            return true;
        }
    }
}
