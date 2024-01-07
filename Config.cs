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
          public int MinimumPlayers { get; set; } = 2;
          [Description("Edit the text shown")]
          public string TextShown { get; set; } = "<size=27>│ %status% │ <b>SERVER NAME</b>  │ <color=red>%playercount%/%maxplayers%</color> Inmates Waiting │</size>";
          public string PausedStatus { get; set; } = @"<color=red>🟥</color> Lobby Paused";
          public string WaitingStatus { get; set; } = @"<color=yellow>🟨</color> Waiting for Players";
          public string StartingStatus { get; set; } = @"<color=green>🟩</color> Starting in %countdown% Seconds";

          public RoomType SpawnRoom { get; set; } = RoomType.Unknown;
          public Vector3 SpawnPosition { get; set; } = new Vector3(0, 995.6f, -8);
     }
}