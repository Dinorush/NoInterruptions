using HarmonyLib;
using LevelGeneration;
using System;
using System.Collections.Generic;

namespace NoInterruptions
{
    [HarmonyPatch]
    internal static class CommandPatch
    {
        struct TermCommand
        {
            public TERM_Command command;
            public string inputLine;
            public string param1;
            public string param2;
        }

        private static Dictionary<IntPtr, Queue<TermCommand>> _queuedCommands = new();
        private static bool _runOriginal = false;

        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReceiveCommand))]
        [HarmonyPrefix]
        private static bool Pre_ReceiveCommand(LG_ComputerTerminalCommandInterpreter __instance, TERM_Command cmd, string inputLine, string param1, string param2)
        {
            if (_runOriginal)
            {
                _runOriginal = false;
                return true;
            }

            var ptr = __instance.Pointer;
            if (!_queuedCommands.TryGetValue(ptr, out var queue))
                _queuedCommands.Add(ptr, queue = new());

            queue.Enqueue(new()
            {
                command = cmd,
                inputLine = inputLine,
                param1 = param1,
                param2 = param2
            });
            return false;
        }

        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.UpdateTerminalScreen))]
        [HarmonyPostfix]
        private static void Post_FireCallbacks(LG_ComputerTerminalCommandInterpreter __instance)
        {
            var ptr = __instance.Pointer;
            if (!_queuedCommands.TryGetValue(ptr, out var queue)) return;

            if (__instance.OnEndOfQueue != null) return;

            _runOriginal = true;
            var cmd = queue.Dequeue();
            if (queue.Count == 0)
                _queuedCommands.Remove(ptr);

            __instance.ReceiveCommand(cmd.command, cmd.inputLine, cmd.param1, cmd.param2);
        }
    }
}
