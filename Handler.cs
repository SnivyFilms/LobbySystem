namespace LobbySystem
{
     using Exiled.API.Features;
     using Exiled.Events.EventArgs.Player;
     using UnityEngine;
     using MEC;
     using PlayerRoles;
     using Exiled.API.Enums;

     public sealed class Handler
     {
          private static readonly Config config = Plugin.Instance.Config;
          
          private static Vector3 SpawnPosition => 
               Plugin.Instance.Config.SpawnRoom == RoomType.Unknown ? Plugin.Instance.Config.SpawnPosition : Room.Get(Plugin.Instance.Config.SpawnRoom).Position + Vector3.up;
          
          public static bool IsLocked;
          public static CoroutineHandle LobbyTimer;
          public void OnWaitingForPlayers()
          {
               IsLocked = false;
               Round.IsLobbyLocked = true;
               GameObject.Find("StartRound").transform.localScale = Vector3.zero;
               LobbyTimer = Timing.RunCoroutine(Lobby());
          }
          public void OnVerified(VerifiedEventArgs ev)
          {
               if (!Round.IsLobby) return;
               ev.Player.Role.Set(RoleTypeId.Tutorial);
               ev.Player.Teleport(SpawnPosition);
          }
          private IEnumerator<float> Lobby()
          {
               double countdown = Plugin.Instance.Config.LobbyTime;
               int requiredPlayers = Plugin.Instance.Config.MinimumPlayers;
               while (Round.IsLobby)
               {
                    string status;
                    if (IsLocked)
                    {
                         status = config.PausedStatus;
                    }
                    else if (Player.List.Count(p => p.IsTutorial) < requiredPlayers)
                    {
                         countdown = Plugin.Instance.Config.LobbyTime;
                         status = config.WaitingStatus;
                    }
                    else
                    {
                         status = config.StartingStatus.Replace("%countdown%", countdown.ToString());
                         countdown--;
                    }
                    string text = config.TextShown
                        .Replace("%status%", status)
                        .Replace("%playercount%", Player.List.Count(p => p.IsTutorial).ToString())
                        .Replace("%maxplayers%", Server.MaxPlayerCount.ToString());

                    Map.Broadcast(1, text, default, true);

                    if (countdown == 0)
                    {
                         foreach (Player player in Player.List.Where(p => p.Role.Type == RoleTypeId.Tutorial))
                              player.Role.Set(RoleTypeId.Spectator);
                         Timing.CallDelayed(0.1f, () => Round.Start());
                         yield break;
                    }

                    yield return Timing.WaitForSeconds(1);
               }
          }
     }
}