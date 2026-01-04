using HarmonyLib;
using LevelGeneration;
using System.Collections.Generic;
using static LevelGeneration.LG_ComputerTerminalManager;

namespace NoInterruptions.Patches
{
    [HarmonyPatch]
    internal static class CommandPatch
    {
        public static void OnCleanup()
        {
            _queuedCommands.Clear();
        }

        private readonly static Dictionary<uint, Queue<pTerminalCommand>> _queuedCommands = new();

        [HarmonyPatch(typeof(LG_ComputerTerminalManager), nameof(LG_ComputerTerminalManager.DoTerminalCommandValidation))]
        [HarmonyPrefix]
        private static bool Pre_Validation(LG_ComputerTerminalCommandInterpreter __instance, pTerminalCommand data)
        {
            if (!Current.m_terminals.ContainsKey(data.ID)) return false;

            var id = data.ID;
            if (!_queuedCommands.TryGetValue(id, out var queue))
                _queuedCommands.Add(id, queue = new());

            queue.Enqueue(data);
            return false;
        }

        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.UpdateTerminalScreen))]
        [HarmonyPostfix]
        private static void Post_Update(LG_ComputerTerminalCommandInterpreter __instance)
        {
            if (!SNetwork.SNet.IsMaster || __instance.OnEndOfQueue != null) return;

            var id = __instance.m_terminal.SyncID;
            if (!_queuedCommands.TryGetValue(id, out var queue)) return;

            var cmd = queue.Dequeue();
            if (queue.Count == 0)
                _queuedCommands.Remove(id);

            Current.m_sendTerminalCommand.Do(cmd);
        }

        [HarmonyPatch(typeof(LG_TERM_Ping), nameof(LG_TERM_Ping.Ping))]
        [HarmonyPostfix]
        private static void Post_Ping(LG_TERM_Ping __instance)
        {
            var terminal = __instance.m_terminal;
            var player = terminal.m_localInteractionSource ?? terminal.m_syncedInteractionSource;
            if (player == null)
                terminal.ChangeState(TERM_State.Awake);
        }
    }
}
