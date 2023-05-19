using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Utility;

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
			Services.RequestManager.Dispose();
		}

		private void PrintPlayerBalance(string full_name) {
			int? balance;
			bool isPlayer = false;

			// if an argument isn't specified, they wanna know about themself
			if (full_name.IsNullOrEmpty() || full_name.IsNullOrWhitespace()) {
				isPlayer = true;
				// the player exists because i said it does!
				full_name = Services.ClientState.LocalPlayer!.Name.TextValue;
			}

			balance = Accounts.GetBalance(full_name);

			if (balance.HasValue) {
				string plural = balance * balance == 1 ? "" : "s";
				if (isPlayer)
					Services.ChatGui.Print($"You have {balance} Lu Token{plural}.");
				else
					Services.ChatGui.Print($"{full_name} has {balance} Lu Token{plural}.");
			} else {
				if (isPlayer)
					Services.ChatGui.PrintError("Unable to retreive your Lu Token balance.");
				else
					Services.ChatGui.PrintError($"Unable to retreive {full_name}'s Lu Token balance");
			}
		}

		#region Command
		/// <summary>
		/// Executes when the player enters the command.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="args"></param>
		private void OnCommand(string command, string args) {
			if (command == CommandName) {
				PrintPlayerBalance(args);
			}
		}
		#endregion
	}
}
