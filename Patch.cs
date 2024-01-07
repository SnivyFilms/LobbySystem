namespace LobbySystem;

using HarmonyLib;
using Exiled.API.Features;
using PlayerRoles;

[HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.ForceRoundStart))]
internal static class RoundStartPatch
{
    private static void Prefix()
    {
        foreach (Player player in Player.List.Where(p => p.Role == RoleTypeId.Tutorial))
            player.Role.Set(RoleTypeId.Spectator);
    }
}