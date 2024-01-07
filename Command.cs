namespace LobbySystem;

using CommandSystem;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(ClientCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class LockCommand : ICommand
{
     public string Command => "lock";
     public string Description => "Locks/unlocks the lobby!";
     public string[] Aliases => new string[] {};
     public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
     {
          if(!sender.CheckPermission(PlayerPermissions.RoundEvents))
          {
               response = "You do not have <color=yellow>RoundEvents</color> permission!";
               return false;
          }

          Handler.IsLocked = (Handler.IsLocked ? false : true);
          response = Handler.IsLocked ? "Lobby has been <color=red>locked</color>!" : "Lobby has been <color=green>unlocked</color>!";
          return true;
     }
}