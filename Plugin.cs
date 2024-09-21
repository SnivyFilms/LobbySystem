namespace LobbySystem
{
     using Exiled.API.Features;
     using HarmonyLib;

     public class Plugin : Plugin<Config>
     {
          public override string Name => "LobbySystem";
          public override string Prefix => Name;
          public override string Author => "@misfiy, updated by Vicious Vikki";
          public override Version Version => new(1, 1, 1);
          public static Plugin Instance { get; private set; } = null!;

          public Handler eventHandler { get; set; } = null!;
          private Harmony harmony { get; set; } = new("LobbySystem");

          public override void OnEnabled()
          {
               Instance = this;
               eventHandler = new Handler();
               harmony.PatchAll();
               Exiled.Events.Handlers.Player.InteractingLocker += eventHandler.OnInteractingLocker;
               Exiled.Events.Handlers.Player.Hurting += eventHandler.OnPlayerHurt;
               Exiled.Events.Handlers.Player.PickingUpItem += eventHandler.OnPickingUpItem;
               Exiled.Events.Handlers.Server.WaitingForPlayers += eventHandler.OnWaitingForPlayers;
               Exiled.Events.Handlers.Server.ChoosingStartTeamQueue += eventHandler.OnChoosingStartTeamQueue;
               Exiled.Events.Handlers.Player.Verified += eventHandler.OnVerified;

               base.OnEnabled();
          }

          public override void OnDisabled()
          {
               Exiled.Events.Handlers.Player.InteractingLocker -= eventHandler.OnInteractingLocker;
               Exiled.Events.Handlers.Player.Hurting -= eventHandler.OnPlayerHurt;
               Exiled.Events.Handlers.Player.PickingUpItem -= eventHandler.OnPickingUpItem;
               Exiled.Events.Handlers.Server.WaitingForPlayers -= eventHandler.OnWaitingForPlayers;
               Exiled.Events.Handlers.Server.ChoosingStartTeamQueue -= eventHandler.OnChoosingStartTeamQueue;
               Exiled.Events.Handlers.Player.Verified -= eventHandler.OnVerified;

               harmony.UnpatchAll();
               eventHandler = null!;
               
               Instance = null!;
               base.OnDisabled();
          }
     }
}