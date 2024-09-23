using Exiled.API.Enums;
using Exiled.API.Interfaces;
using System.ComponentModel;
using UnityEngine;

namespace LobbySystem
{
     public sealed class Config : IConfig
     {
          public bool IsEnabled { get; set; } = true;
          public bool Debug { get; set; } = false;
          public double LobbyTime { get; set; } = 30;
          public short MinimumPlayers { get; set; } = 2;

          [Description("Edit the text shown")]
          public bool UseHints { get; set; } = false;
          public string TextShown { get; set; } = "<size=27>│ %status% │ <b>SERVER NAME</b>  │ <color=red>%playercount%/%maxplayers%</color> Inmates Waiting │</size>";
          public string PausedStatus { get; set; } = @"<color=red>🟥</color> Lobby Paused";
          public string WaitingStatus { get; set; } = @"<color=yellow>🟨</color> Waiting for Players";
          public string StartingStatus { get; set; } = @"<color=green>🟩</color> Starting in %countdown% Seconds";

          [Description("Spawn configs")]
          public bool UseRoomsToSpawnAt { get; set; } = true;
          public List<RoomType> SpawnRooms { get; set; } = new List<RoomType>
          {
               RoomType.LczArmory,
               RoomType.Lcz914,
               RoomType.LczClassDSpawn,
               RoomType.LczGlassBox,
               RoomType.LczToilets,
               RoomType.Hcz106,
               RoomType.Hcz079,
               RoomType.HczHid,
               RoomType.EzPcs,
               RoomType.EzDownstairsPcs,
               RoomType.EzUpstairsPcs,
               RoomType.EzGateA,
               RoomType.EzGateB
          };
          public List<Vector3> DefinedSpawnPosition { get; set; } = new()
          {
               new Vector3(0, 995.6f, -8),
               new Vector3(40, 1040.080f, -32.600f)
          };
          [Description("Can players pick up items in lobby?")]
          public bool AllowItemPickup { get; set; } = false;

          [Description("Can players die from enviromental hazards? (I.E. the death pit in 106s room)")]
          public bool PlayersDieFromEnviromentalHazards { get; set; } = false;

          [Description("Open some doors for more room to move around before round start")]
          public bool OpenDoorsForMoreRoom { get; set; } = true;

          [Description("Should the doors lock while waiting for the round to start")]
          public bool LockDoorsBeforeRoundStart { get; set; } = true;
     }
}