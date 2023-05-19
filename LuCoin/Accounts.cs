using Dalamud.Utility;
using System.Threading;

namespace LuCoin {
	internal static class Accounts {
		public static void PrintBalance(string full_name) {
			bool isPlayer = false;

			// if an argument isn't specified, they wanna know about themself
			if (full_name.IsNullOrEmpty() || full_name.IsNullOrWhitespace()) {
				isPlayer = true;
				// the player exists because i said it does! (also because they can't chat if they don't)
				full_name = Services.ClientState.LocalPlayer!.Name.TextValue;
			}

			// thread the query to avoid freezing the game
			// GC will destroy this when it's done
			new Thread(() => {
				if (Services.RequestManager.Accounts.TryGetValue(full_name.ToLower(), out var balance)) {
					// if the balance is 1 or -1 then it shouldn't be plural
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
				Services.ChatGui.UpdateQueue();
			}).Start();
		}
	}
}
