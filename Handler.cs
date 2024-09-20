﻿using Exiled.API.Features;
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
        private Dictionary<Door, bool> _doorLockStates = new Dictionary<Door, bool>();
        private List<Door> doorsOpened = new List<Door>();

        private void SelectRandomSpawnPoint()
        {
            if (config.UseRoomsToSpawnAt)
            {
                Room? selectedRoom = Room.List.FirstOrDefault(room =>
                    room.Type == config.SpawnRooms[random.Next(1, config.SpawnRooms.Count)]);
                if (selectedRoom != null)
                    _selectedSpawnPoint = selectedRoom.Position + Vector3.up;
                Log.Debug($"Selected Room is {selectedRoom}");
            }
            else
            {
                _selectedSpawnPoint = config.DefinedSpawnPosition[random.Next(1,config.DefinedSpawnPosition.Count)];
                Log.Debug($"Selected Spawn Point is {_selectedSpawnPoint}");
            }
        }

        private void LockAllDoors()
        {
            _doorLockStates.Clear();
            doorsOpened.Clear();

            foreach (Door door in Door.List)
            {
                _doorLockStates[door] = door.IsLocked;

                if (!door.IsLocked)
                    door.ChangeLock(DoorLockType.AdminCommand);
            }

            List<DoorType> doorsToOpen = new List<DoorType>
            {
                DoorType.GateA,
                DoorType.GateB,
                DoorType.Scp914Gate,
                DoorType.GR18Gate,
                DoorType.GR18Inner,
                DoorType.Scp330,
                DoorType.Scp330Chamber,
                DoorType.Scp079First,
                DoorType.Scp079Second,
                DoorType.Scp079Armory,
                DoorType.LczArmory,
                DoorType.HczArmory,
                DoorType.HID,
                DoorType.HIDLeft,
                DoorType.HIDRight,
                DoorType.LczWc
            };
            
            if (config.OpenDoorsForMoreRoom)
            {
                // Open doors and store references in the list
                foreach (DoorType doorType in doorsToOpen)
                {
                    Door door = Door.Get(doorType);
                    if (door != null && !door.IsOpen)
                    {
                        door.IsOpen = true;
                        doorsOpened.Add(door);
                        Log.Debug($"Opened door: {door.Name} ({doorType})");
                    }
                }
                foreach (Door door in Door.List)
                {
                    if (door.Room == Room.Get(RoomType.LczClassDSpawn) && !door.IsOpen)
                    {
                        door.IsOpen = true;
                        doorsOpened.Add(door); // Log the door as opened
                        Log.Debug($"Opened door: {door.Name} (LczClassDSpawn)");
                    }
                }

                // Explicitly close the LightContainmentDoor
                Door lightContainmentDoor = Door.Get(DoorType.LightContainmentDoor);
                if (lightContainmentDoor != null && lightContainmentDoor.IsOpen)
                {
                    lightContainmentDoor.IsOpen = false;
                    doorsOpened.Remove(lightContainmentDoor); // Ensure it's not marked as opened
                }
            }
        }

        public void RestoreDoorLockStates()
        {
            foreach (var doorEntry in _doorLockStates)
            {
                Door door = doorEntry.Key;
                bool wasLocked = doorEntry.Value;

                if (!wasLocked && door.IsLocked)
                {
                    door.ChangeLock(DoorLockType.None);
                }
            }
            foreach (Door door in doorsOpened)
            {
                if (door.IsOpen)
                {
                    door.IsOpen = false;
                    Log.Debug($"Closed door: {door.Name}");
                }
            }

            doorsOpened.Clear();
        }

        public void OnWaitingForPlayers()
        {
            SelectRandomSpawnPoint();
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
                    .Replace("%playercount%", Player.List.Count(p => !p.IsOverwatchEnabled).ToString())
                    .Replace("%maxplayers%", Server.MaxPlayerCount.ToString());

                if (config.UseHints)
                    Map.ShowHint(text, 1);
                else
                    Map.Broadcast(1, text, default, true);

                if (countdown == 0)
                {
                    foreach (Player player in Player.List.Where(p => p.IsAlive))
                        player.Role.Set(RoleTypeId.Spectator);

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
                case DamageType.Hypothermia:
                    ev.IsAllowed = false;
                    break;
            }
        }

        public void OnInteractingLocker(InteractingLockerEventArgs ev)
        {
            if (Round.IsLobby && !config.AllowItemPickup)
                ev.IsAllowed = false;
        }
    }
}
