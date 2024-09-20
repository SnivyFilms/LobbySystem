using Exiled.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using PlayerRoles;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.API.Features.Doors;
using Random = System.Random;

namespace LobbySystem
{
    public class Handler
    {
        private Random random = new Random();
        private Config config = Plugin.Instance.Config;
        private Vector3 _selectedSpawnPoint;
        
        // Dictionary to track the original lock state of each door
        private Dictionary<Door, bool> _doorLockStates = new Dictionary<Door, bool>();

        // Randomly selects a spawn point from the configured spawn locations.
        private void SelectRandomSpawnPoint()
        {
            
            if (config.UseRoomsToSpawnAt)
            {
                Room? selectedRoom = Room.List.FirstOrDefault(room => room.Type == config.SpawnRooms[random.Next(0, config.SpawnRooms.Count)]);
                if (selectedRoom != null)
                    _selectedSpawnPoint = selectedRoom.Position + Vector3.up;
            }
            else
            {
                _selectedSpawnPoint = config.DefinedSpawnPosition[random.Next(0, config.DefinedSpawnPosition.Count)];
            }
        }

        // Store the lock state of all doors and lock them
        private void LockAllDoors()
        {
            _doorLockStates.Clear(); // Clear previous states

            foreach (Door door in Door.List)
            {
                // Save the original lock state (true if locked, false if unlocked)
                _doorLockStates[door] = door.IsLocked;
                
                // Lock the door if it's not already locked
                if (!door.IsLocked)
                {
                    door.ChangeLock(DoorLockType.AdminCommand);
                }
            }
        }

        // Restore the lock state of all doors based on their original status
        public void RestoreDoorLockStates()
        {
            foreach (var doorEntry in _doorLockStates)
            {
                Door door = doorEntry.Key;
                bool wasLocked = doorEntry.Value;

                // If the door was unlocked originally and is now locked, unlock it
                if (!wasLocked && door.IsLocked)
                {
                    door.ChangeLock(DoorLockType.None); // Unlock the door (toggle from locked to unlocked)
                }
            }
        }

        public void OnWaitingForPlayers()
        {
            // Select the spawn point once the server is waiting for players.
            SelectRandomSpawnPoint();

            // Lock all doors when the lobby starts
            LockAllDoors();

            GameObject.Find("StartRound").transform.localScale = Vector3.zero;
            Timing.RunCoroutine(Lobby());
        }

        public void OnVerified(VerifiedEventArgs ev)
        {
            if (!Round.IsLobby) return;

            // Set all players to the same spawn point.
            ev.Player.Role.Set(RoleTypeId.Tutorial);
            ev.Player.Teleport(_selectedSpawnPoint);
        }

        public void OnChoosingStartTeamQueue(ChoosingStartTeamQueueEventArgs ev)
        {
            foreach (Player player in Player.List.Where(p => p.IsAlive))
                player.Role.Set(RoleTypeId.Spectator);
        }

        private IEnumerator<float> Lobby()
        {
            double countdown = Plugin.Instance.Config.LobbyTime;

            while (Round.IsLobby)
            {
                string status;
                if (Round.IsLobbyLocked)
                {
                    status = config.PausedStatus;
                }
                else if (Player.List.Count(p => !p.IsOverwatchEnabled) < Plugin.Instance.Config.MinimumPlayers)
                {
                    countdown = Plugin.Instance.Config.LobbyTime;
                    status = config.WaitingStatus;
                }
                else
                {
                    countdown--;
                    status = config.StartingStatus.Replace("%countdown%", countdown.ToString());
                }

                string text = config.TextShown
                    .Replace("%status%", status)
                    .Replace("%playercount%", Player.List.Count(p => p.IsTutorial).ToString())
                    .Replace("%maxplayers%", Server.MaxPlayerCount.ToString());

                if (config.UseHints)
                    Map.ShowHint(text, 1);
                else
                    Map.Broadcast(1, text, default, true);

                if (countdown == 0)
                {
                    foreach (Player player in Player.List.Where(p => p.IsAlive))
                        player.Role.Set(RoleTypeId.Spectator);

                    // Restore the original lock states of the doors
                    RestoreDoorLockStates();

                    Round.Start();
                    yield break;
                }

                yield return Timing.WaitForSeconds(1);
            }
        }
        public void OnPickingUpItem(PickingUpItemEventArgs ev)
        {
            if (Round.IsLobby && !config.AllowItemPickup)
                ev.IsAllowed = false;
        }
        
        public void OnPlayerHurt(HurtingEventArgs ev)
        {
            if (config.PlayersDieFromEnviromentalHazards)
                return;

            if (!Round.IsLobby)
                return;
            
            if (ev.Player == null)
                return;

            switch (ev.DamageHandler.Type)
            {
                case DamageType.Tesla:
                    ev.IsAllowed = false;
                    break;
                case DamageType.Crushed:
                
                    ev.IsAllowed = false;
                    var roomcoord = ev.Player.CurrentRoom.Doors
                        .Where(d => d.RequiredPermissions.RequiredPermissions ==
                                    Interactables.Interobjects.DoorUtils.KeycardPermissions.None).ElementAt(
                            new Random().Next(ev.Player.CurrentRoom.Doors.Where(d =>
                                d.RequiredPermissions.RequiredPermissions ==
                                Interactables.Interobjects.DoorUtils.KeycardPermissions.None).ToList().Count)).Position;
                    roomcoord += Vector3.forward * 0.4f;
                    roomcoord += Vector3.up;
                    ev.Player.Position = roomcoord;
                    break;
                case DamageType.Falldown:
                    ev.IsAllowed = false;
                    break;
            }
        }
    }
}
