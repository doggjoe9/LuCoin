using Dalamud.Game.Command;
using Dalamud.Plugin;

namespace LuCoin {

	/// <summary>
	/// For anyone who stumbled on this, don't try to make sense of it. It makes no sense.
	/// </summary>
	public sealed class LuCoin : IDalamudPlugin {
		#region Fields
		public string Name => "Lu Tokens";
		private const string CommandName = "/lubucks";
		#endregion

		public LuCoin(DalamudPluginInterface pluginInterface) {
			#region Service Init
			Services.Initialize(pluginInterface);
			#endregion
			#region Command Init
			Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "View Lu Token balance."
			});
			#endregion
		}

		public void Dispose() {
			Services.CommandManager.RemoveHandler(CommandName);
			Services.RequestManager.Dispose();
		}

		#region Command
		/// <summary>
		/// Executes when the player enters the command.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="args"></param>
		private void OnCommand(string command, string args) {
			if (command == CommandName) {
				Accounts.PrintBalance(args);
			}
		}
		#endregion
	}
}
