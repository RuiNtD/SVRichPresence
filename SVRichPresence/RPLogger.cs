using DiscordRPC.Logging;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogLevel = DiscordRPC.Logging.LogLevel;

namespace SVRichPresence {
	class RPLogger : ILogger {
		public LogLevel Level { get; set; }

		private readonly IMonitor Monitor;

		public RPLogger(IMonitor monitor) {
			Level = LogLevel.Info;
			Monitor = monitor;
		}

		public void Trace(string message, params object[] args) {
			if (Level > LogLevel.Trace) return;
			Monitor.Log("[RPC] " + string.Format(message, args), StardewModdingAPI.LogLevel.Trace);
		}

		public void Info(string message, params object[] args) {
			if (Level > LogLevel.Info) return;
			Monitor.Log("[RPC] " + string.Format(message, args), StardewModdingAPI.LogLevel.Info);
		}

		public void Warning(string message, params object[] args) {
			if (Level > LogLevel.Warning) return;
			Monitor.Log("[RPC] " + string.Format(message, args), StardewModdingAPI.LogLevel.Warn);
		}

		public void Error(string message, params object[] args) {
			if (Level > LogLevel.Error) return;
			Monitor.Log("[RPC] " + string.Format(message, args), StardewModdingAPI.LogLevel.Error);
		}
	}
}
