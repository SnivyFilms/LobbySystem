using Exiled.API.Features;
using MEC;
using PlayerRoles;
using UnityEngine;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.API.Features.Doors;
using VoiceChat;
using Random = System.Random;

namespace LobbySystem
{
    public class Handler
    {
        private Random random = new Random();
        private Config config = Plugin.Instance.Config;
        private Vector3 _selectedSpawnPoint;
        private Dictionary<Door, bool> _doorLockStates = new();
        private List<Door> doorsOpened = new();

        public enum SpawnEnum
        {
            Rooms, RoomsAndCoords, Coords
        }
        private void SelectRandomSpawnPoint()
        {
            switch (config.SpawnType)
            {
                case SpawnEnum.Rooms:
                {
                    List<Room> availableRooms = Room.List.Where(room => config.SpawnRooms.Contains(room.Type)).ToList();
                    if (availableRooms.Count > 0)
                    {
                        Room selectedRoom = availableRooms[random.Next(availableRooms.Count)];
                        _selectedSpawnPoint = selectedRoom.Position + Vector3.up;
                        Log.Debug($"Selected Room is {selectedRoom}");
                    }
                    else
                    {
                        Log.Warn("Couldn't select a valid room. Using a defined spawn point");
                        _selectedSpawnPoint = config.DefinedSpawnPosition[random.Next(config.DefinedSpawnPosition.Count)];
                        Log.Debug($"Spawn point is {_selectedSpawnPoint}");
                    }

                    break;
                }
                case SpawnEnum.Coords:
                {
                    _selectedSpawnPoint = config.DefinedSpawnPosition[random.Next(config.DefinedSpawnPosition.Count)];
                    Log.Debug($"Selected Spawn Point is {_selectedSpawnPoint}");
                    break;
                }
                case SpawnEnum.RoomsAndCoords:
                {
                    List<Room> availableRooms = Room.List.Where(room => config.SpawnRooms.Contains(room.Type)).ToList();
                    if (random.Next(0, 2) == 1)
                    {
                        if (availableRooms.Count > 0)
                        {
                            Room selectedRoom = availableRooms[random.Next(availableRooms.Count)];
                            _selectedSpawnPoint = selectedRoom.Position + Vector3.up;
                            Log.Debug($"Selected Room is {selectedRoom}");
                        }
                        else
                        {
                            Log.Warn("Couldn't select a valid room. Using a defined spawn point");
                            _selectedSpawnPoint = config.DefinedSpawnPosition[random.Next(config.DefinedSpawnPosition.Count)];
                            Log.Debug($"Spawn point is {_selectedSpawnPoint}");
                        }
                    }
                    else
                    {
                        _selectedSpawnPoint = config.DefinedSpawnPosition[random.Next(config.DefinedSpawnPosition.Count)];
                        Log.Debug($"Selected Spawn Point is {_selectedSpawnPoint}");
                    }

                    break;
                }
            }
        }

        private void LockAllDoors()
        {
            if (config.LockDoorsBeforeRoundStart)
            {
                _doorLockStates.Clear();
                doorsOpened.Clear();

                foreach (Door door in Door.List)
                {
                    _doorLockStates[door] = door.IsLocked;

                    if (!door.IsLocked)
                        door.ChangeLock(DoorLockType.AdminCommand);
                }
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
                DoorType.HIDChamber,
                DoorType.HIDUpper,
                DoorType.HIDLower,
                DoorType.LczWc
            };
            
            if (config.OpenDoorsForMoreRoom)
            {
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
                        doorsOpened.Add(door);
                        Log.Debug($"Opened door: {door.Name} (LczClassDSpawn)");
                    }
                }
                Door lightContainmentDoor = Door.Get(DoorType.LightContainmentDoor);
                if (lightContainmentDoor != null && lightContainmentDoor.IsOpen)
                {
                    lightContainmentDoor.IsOpen = false;
                    doorsOpened.Remove(lightContainmentDoor);
                }
            }
        }

        public void RestoreDoorLockStates()
        {
            if (!config.LockDoorsBeforeRoundStart)
                return;
            
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
            RoleTypeId roleToSpawn;
            if (config.LobbyRoles.Count > 0)
            {
                roleToSpawn = config.LobbyRoles[random.Next(config.LobbyRoles.Count)];
            }
            else
            {
                Log.Warn("LobbyRoles config list is empty. Defaulting to Tutorial.");
                roleToSpawn = RoleTypeId.Tutorial;
            }
            ev.Player.Role.Set(roleToSpawn, RoleSpawnFlags.None);
            ev.Player.Teleport(_selectedSpawnPoint);
            if (config.GiveGlobalIntercom)
                ev.Player.VoiceChannel = VoiceChatChannel.PreGameLobby;
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

                    if(config.LockDoorsBeforeRoundStart)
                        RestoreDoorLockStates();
                    if(config.CleanUpRagdollsAtRoundStart)
                        CleanUpRagdolls();
                    if(config.GiveGlobalIntercom)
                        ClearGlobalIntercom();
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
            if (!Round.IsLobby)
                return;
            
            if (config.PlayersDieFromEnviromentalHazards)
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
                    ev.Player.Position = _selectedSpawnPoint;
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

        public void CleanUpRagdolls()
        {
            if (!config.CleanUpRagdollsAtRoundStart)
                return;
            Map.CleanAllRagdolls();
        }

        public void ClearGlobalIntercom()
        {
            if (config.GiveGlobalIntercom)
            {
                foreach (Player player in Player.List)
                    player.VoiceChannel = VoiceChatChannel.None;
            }
        }
    }
}
