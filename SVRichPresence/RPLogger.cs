using DiscordRPC.Logging;
using StardewModdingAPI;
using LogLevel = DiscordRPC.Logging.LogLevel;

namespace SVRichPresence {
	internal class RPLogger : ILogger {
		private readonly IMonitor Monitor;
		public LogLevel Level { get; set; }

		public RPLogger(IMonitor monitor) {
			Monitor = monitor;
			Level = LogLevel.Info;
		}

		public RPLogger(IMonitor monitor, LogLevel level) {
			Monitor = monitor;
			Level = level;
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
