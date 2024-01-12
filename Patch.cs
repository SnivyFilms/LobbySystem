using PluginAPI.Events;
using HarmonyLib;
using Exiled.API.Features;
using PlayerRoles;

namespace LobbySystem;

[HarmonyPatch(typeof(CharacterClassManager))]
internal static class RoundStartPatch
{
    [HarmonyPatch(nameof(CharacterClassManager.Start))]
    private static void Prefix() => SetRole();
    
    [HarmonyPatch(nameof(CharacterClassManager.ForceRoundStart))]
    private static void Postfix(CharacterClassManager __instance)
    {
        SetRole();
        
        EventManager.ExecuteEvent(new RoundStartEvent());
        __instance.NetworkRoundStarted = true;
        __instance.RpcRoundStarted();
    }

    private static void SetRole()
    {
        foreach (Player player in Player.List.Where(p => p.Role == RoleTypeId.Tutorial))
            player.Role.Set(RoleTypeId.Spectator);
    }
}

[HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.Init))]
internal static class InitPatch
{
    private static bool Prefix() => false;
}