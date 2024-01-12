namespace LobbySystem
{
     using Exiled.API.Features;
     using HarmonyLib;

     public class Plugin : Plugin<Config>
     {
          public override string Name => "LobbySystem";
          public override string Prefix => Name;
          public override string Author => "@misfiy";
          public override Version Version => new(1, 0, 3);
          public static Plugin Instance;
          private Handler eventHandler;
          private Harmony harmony { get; set; } = new("LobbySystem");

          public override void OnEnabled()
          {
               Instance = this;
               eventHandler = new Handler();
               harmony.PatchAll();

               Exiled.Events.Handlers.Server.WaitingForPlayers += eventHandler.OnWaitingForPlayers;
               Exiled.Events.Handlers.Player.Verified += eventHandler.OnVerified;

               base.OnEnabled();
          }

          public override void OnDisabled()
          {
               Exiled.Events.Handlers.Server.WaitingForPlayers -= eventHandler.OnWaitingForPlayers;
               Exiled.Events.Handlers.Player.Verified -= eventHandler.OnVerified;

               harmony.UnpatchAll();
               eventHandler = null!;
               
               Instance = null!;
               base.OnDisabled();
          }
     }
}