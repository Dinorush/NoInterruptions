using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using LevelGeneration;
using Player;
using System.Collections;
using static LevelGeneration.LG_ComputerTerminalManager;

namespace NoInterruptions.Patches
{
    [HarmonyPatch(typeof(LG_ComputerTerminal))]
    internal static class TerminalPatch
    {
        [HarmonyPatch(typeof(LG_ComputerTerminalManager), nameof(LG_ComputerTerminalManager.DoChangeTerminalStateValidation))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Postfix_DoValidation(LG_ComputerTerminalManager __instance, pTerminalState data)
        {
            if (!__instance.m_terminals.ContainsKey(data.ID)) return false;

            return __instance.m_terminals[data.ID].CurrentStateName != TERM_State.PlayerInteracting || (TERM_State)data.state != TERM_State.Sleeping;
        }

        [HarmonyPatch(nameof(LG_ComputerTerminal.SyncChangeState))]
        [HarmonyPostfix]
        private static void Postfix_EnterInteracting(LG_ComputerTerminal __instance)
        {
            if (__instance.CurrentStateName == TERM_State.PlayerInteracting)
            {
                CoroutineManager.StartCoroutine(DelayedFixState(__instance).WrapToIl2Cpp());
            }
        }

        private static IEnumerator DelayedFixState(LG_ComputerTerminal terminal)
        {
            float endTime = Clock.Time + 0.5f;
            // Wait one frame so we don't interrupt the ChangeState call
            yield return null;

            // JFS - Delay checking locomotion in case packet was delayed
            while (Clock.Time < endTime)
            {
                var state = terminal.CurrentStateName;
                if (state != TERM_State.PlayerInteracting)
                {
                    if (state == TERM_State.Sleeping && terminal.m_localInteractionSource != null)
                        terminal.ChangeState(TERM_State.PlayerInteracting);
                    yield break;
                }
                yield return null;
            }

            while (terminal.CurrentStateName == TERM_State.PlayerInteracting)
            {
                AttemptFixState(terminal);
                yield return null;
            }
        }

        private static void AttemptFixState(LG_ComputerTerminal terminal)
        {
            var player = terminal.m_localInteractionSource ?? terminal.m_syncedInteractionSource;

            if (player == null)
            {
                terminal.ChangeState(TERM_State.Awake);
                return;
            }

            if (player.Locomotion.m_currentStateEnum != PlayerLocomotion.PLOC_State.OnTerminal)
            {
                if (player.IsLocallyOwned || (player.transform.position - player.Sync.m_locomotionData.Pos).sqrMagnitude > 0.0001f)
                {
                    terminal.m_localInteractionSource = terminal.m_syncedInteractionSource = null;
                    terminal.ChangeState(TERM_State.Awake);
                }
            }
        }

        private static float _inputTime;
        [HarmonyPatch(nameof(LG_ComputerTerminal.EnterFPSView))]
        [HarmonyPrefix]
        private static void Prefix_EnterFPSView(LG_ComputerTerminal __instance)
        {
            if (__instance.m_localInteractionSource != null)
                _inputTime = Clock.Time + 0.5f;
        }

        [HarmonyPatch(typeof(LG_TERM_PlayerInteracting), nameof(LG_TERM_PlayerInteracting.Enter))]
        [HarmonyPostfix]
        private static void Postfix_EnterPlayerInteracting(LG_TERM_PlayerInteracting __instance)
        {
            __instance.m_inputTimer = _inputTime;
        }

        [HarmonyPatch(nameof(LG_ComputerTerminal.ExitFPSView))]
        [HarmonyPostfix]
        private static void Postfix_ExitFPSView(LG_ComputerTerminal __instance)
        {
            if (__instance.m_localInteractionSource != null)
            {
                var state = __instance.GetState((int)TERM_State.PlayerInteracting).Cast<LG_TERM_PlayerInteracting>();
                state.m_inputTimer = Clock.Time + 0.5f;
                if (state.m_lastSyncString != __instance.m_currentLine)
                {
                    LG_ComputerTerminalManager.WantToSendTerminalString(__instance.SyncID, __instance.m_currentLine);
                    state.m_lastSyncString = __instance.m_currentLine;
                }
            }
        }
    }
}
