using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using LevelGeneration;
using Player;
using System.Collections;

namespace NoInterruptions
{
    [HarmonyPatch(typeof(LG_ComputerTerminal))]
    internal static class TerminalPatch
    {
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
                if (terminal.CurrentStateName != TERM_State.PlayerInteracting)
                    yield break;
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
                    terminal.ChangeState(TERM_State.Awake);
            }
        }

        [HarmonyPatch(nameof(LG_ComputerTerminal.ExitFPSView))]
        [HarmonyPostfix]
        private static void Postfix_ExitInteracting(LG_ComputerTerminal __instance)
        {
            if (__instance.m_localInteractionSource != null)
            {
                var state = __instance.m_currentState.Cast<LG_TERM_PlayerInteracting>();
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
