using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuCoin {
	internal static class Accounts {
		public static int? GetBalance(string full_name) {
			if (Services.RequestManager.Accounts.TryGetValue(full_name.ToLower(), out var balance))
				return balance;
			else return null;
		}
	}
}
