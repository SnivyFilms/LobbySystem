﻿using CommandSystem.Commands.RemoteAdmin;
using CommandSystem.Commands.Shared;
using Exiled.API.Features;
using HarmonyLib;
using LabApi.Events;
using PlayerRoles;
using Round = LabApi.Features.Wrappers.Round;

namespace LobbySystem;

[HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.ForceRoundStart))]
internal static class RoundStartPatch
{
    private static void Postfix()
    {
        //EventManager.InvokeEvent(Round.Start);
        //Round.Start();
        Server.Host.ReferenceHub.characterClassManager.NetworkRoundStarted = true;
        Server.Host.ReferenceHub.characterClassManager.RpcRoundStarted();
    }
}

[HarmonyPatch(typeof(ForceStartCommand), nameof(ForceStartCommand.Execute))]
internal static class CommandPatch
{
    private static void Postfix(bool __result)
    {
        if (__result)
        {
            Plugin.Instance.eventHandler.RestoreDoorLockStates();
            Plugin.Instance.eventHandler.CleanUpRagdolls();
            Plugin.Instance.eventHandler.ClearGlobalIntercom();
            foreach (Player player in Player.List)
                player.Role.Set(RoleTypeId.Spectator);
        }
        //Round.Start();
    }
}

[HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.Init))]
internal static class InitPatch
{
    private static bool Prefix() => false;
}