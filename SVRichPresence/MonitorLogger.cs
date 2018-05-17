using DiscordRPC.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SVRichPresence {
	public class MonitorLogger : ILogger {
		private StardewModdingAPI.IMonitor Monitor;

		public MonitorLogger(StardewModdingAPI.IMonitor monitor) {
			this.Monitor = monitor;
		}

		public LogLevel Level { get; set; }

		public void Info(string message, params object[] args) {
			if (Level != LogLevel.Info) return;
			Monitor.Log(String.Format(message, args), StardewModdingAPI.LogLevel.Info);
		}

		public void Warning(string message, params object[] args) {
			if (Level != LogLevel.Info && Level != LogLevel.Warning) return;
			Monitor.Log(String.Format(message, args), StardewModdingAPI.LogLevel.Warn);
		}

		public void Error(string message, params object[] args) {
			if (Level != LogLevel.Info && Level != LogLevel.Warning && Level != LogLevel.Error) return;
			Monitor.Log(String.Format(message, args), StardewModdingAPI.LogLevel.Error);
		}
	}
}
