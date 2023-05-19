using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;

namespace LuCoin.Network
{
	/// <summary>
	/// This exception is thrown when a thread tries to call <see cref="RequestManager.Update"/> before the
	/// <see cref="RequestManager.ThrottleTimespan"/> has elapsed.
	/// </summary>
	internal class RapidRequestException : Exception {}

    internal class RequestManager : IDisposable
    {
		/// <summary>
		/// Human-readable URI of the ledger data
		/// </summary>
		private const string dataUri = @"https://docs.google.com/spreadsheets/d/e/2PACX-1vQRJdAuhBTOn9FsZfHBeh_JfbUs5xJae7ViwV2rNCBsibYr7WwC_UNvXwN2z4Q8zk7RvrPDKVrqAjax/pub";
		private const string dataUriParam = @"?output=csv";

		// don't use these anymore they are not millisecond accurate
		///// <summary>
		///// The minimum amount of time the RequestManager must wait before creating a REST request.
		///// </summary>
		//public static readonly TimeSpan ThrottleTimespan = TimeSpan.FromSeconds(2);

		///// <summary>
		///// Timestamp marking the moment the last REST request completed.
		///// </summary>
		//private static DateTime lastRequestTimestamp = DateTime.MinValue;

		private static long throttleTimespan = 2000;

		/// <summary>
		/// A stopwatch to prevent rapid requests.
		/// </summary>
		private static Stopwatch stopwatch = new Stopwatch();

		/// <summary>
		/// A lock to avoid duplicate requests putting unecessary strain on the host. Keep this as long as more than
		/// one RequestManager may access the same host (in this case it is guaranteed, as <see cref="dataUri"/> is
		/// const.
		/// </summary>
		private static readonly object _globalUpdateLock = new object();

		/// <summary>
		/// HTTP client for REST calls
		/// DO NOT MUTATE PAST INIT
		/// </summary>
		private readonly HttpClient client = new HttpClient();

		/// <summary>
		/// Map of accounts where each string key is the player's in-game full name (verbatim) and each integer value
		/// is the player's LuBucks balance.
		/// </summary>
		private Dictionary<string, int> accounts = new Dictionary<string, int>();

		// for now, there is no need for this to be retreived through a getter rather than returned by. Update. but if
		// refreshing data was not done on demand, this would be necessary. Since it does not affect runtime behavior,
		// i am leaving this open to that possibility.
		/// <summary>
		/// Gets the map of account balances. Keys are players' full in-game names (verbatim) and values are integers
		/// denoting their current balance.
		/// </summary>
		public Dictionary<string, int> Accounts {
			get {
				// trys to synchronously grab the info fresh from the database every time it is requested. inefficient,
				// but because of the safeguards in the Update method, nothing bad can come of it. if someone queries
				// too rapidly, or if a race condition occurs, the method will simply throw an exception
				try {
					Update();
				} catch(RapidRequestException e) {
					PluginLog.Error("The user tried to query too quickly. Unable to update accounts.");
					Services.ChatGui.PrintError("Slow down. Results may be outdated.");
					PluginLog.LogError(e.Message);
				} catch(HttpRequestException) {
					PluginLog.Error("Unexpected HTTP error. Unable to update accounts.");
					Services.ChatGui.PrintError("An unexpected error has occured. Results may be outdated.");
				}
				

				// return a shallow copy to avoid mutating the original in this case a shallow copy is acceptable. if
				// the accounts dictionary is extended to have reference type keys or values, this must be converted to
				// a deep copy.
				return new Dictionary<string, int>(accounts);

				// sample deep copy. note the key is still shallow copied, since this is not preferable in most cases
				// where a reference type is used as a key.
				// return accounts.ToDictionary(x => x.Key, x => x.Value.Clone());
			}
		}
		
		/// <summary>
		/// Instantiates a new RequestManager.
		/// </summary>
		/// <param name="init">if true, perform an <see cref="Update"/> on creation.</param>
		public RequestManager(bool init = true) {
			client.BaseAddress = new Uri(dataUri);
			stopwatch.Start();
			if (init)
				Update();
		}

		/// <summary>
		/// Updates the data cache for this RequestManager, maintaining throttling and interlocks
		/// to avoid excessive requests and race conditions.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="RapidRequestException">if <see cref="ThrottleTimespan"> has not elapsed since the last invokation.</exception>
		/// <exception cref="HttpRequestException">if an HTTP exception occurs</exception>
		private void Update() {
			// initialize some variables outside of locked scope
			string? payload = null;

			// GET - inside of lock to avoid race conditions and duplicates
			lock (_globalUpdateLock) {
				// gatekeeping
				//if (lastRequestTimestamp - DateTime.Now <= ThrottleTimespan) // old gatekeeping
				//	throw new RapidRequestException();
				if (stopwatch.ElapsedMilliseconds < throttleTimespan)
					throw new RapidRequestException();

				// GET
				HttpResponseMessage response = client.GetAsync(dataUriParam).Result;
				if (response.IsSuccessStatusCode) {
					// success
					payload = response.Content.ReadAsStringAsync().Result;
				} else {
					// rejected/failed

					// log for Dalamud
					PluginLog.Error($"Rejected/failed GET request to LuBucks database.\nReason: {response.ReasonPhrase}\nStatus: {response.StatusCode}");

					// throw an exception
					// inner exception is passed as null hope this doesn't cause any problems :)
					throw new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);
				}

				// finally, update the last request timestamp
				//lastRequestTimestamp = DateTime.Now; // old gatekeeping
				stopwatch.Restart();
			}

			// process
			if (payload != null)
				ProcessCSV(payload);
		}

		/// <summary>
		/// Parse a CSV payload to a balance map.
		/// </summary>
		/// <param name="payload">The payload downloaded from the link.</param>
		private void ProcessCSV(string payload) {
			string[] lines = payload.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
			Dictionary<string, int> tmpAccounts = new Dictionary<string, int>();
			for (int i = 0; i < lines.Length; i++) {
				string[] split = lines[i].Split(',');
				// not user safe, but if lu adds extraneous commas or unparsable values, that's her problem, not mine.
				//tmpAccounts[split[0].Trim()] = int.Parse(split[1].Trim());
				// ... nevermind i just realized she could get up to some real tomfoolery if she did that. i'll add
				// some safeguards
				string name = split[0].Trim().ToLower(); // this is safe. worst case someone's account isn't visible to the program.
				if (int.TryParse(split[1].Trim(), out int balance)) // be carefull reading ints...
					tmpAccounts[name] = balance;
				// if she put a comma in their name, or if their balance is invalid, just ignore the whole account.
			}
			accounts = tmpAccounts;
		}

		public void Dispose() {
			client.Dispose();
		}
	}
}
