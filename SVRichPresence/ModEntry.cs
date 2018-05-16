using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using static DiscordRpc;

namespace SVRichPresence {
	public class ModEntry : Mod {
		private const string clientId = "444517509148966923";
		ModConfig config;

		public override void Entry(IModHelper helper) {
			config = Helper.ReadConfig<ModConfig>();
			EventHandlers handlers = new EventHandlers();
			DiscordRpc.Initialize(clientId, ref handlers, false, "413150");
			GameEvents.UpdateTick += DoUpdate;
			SaveEvents.AfterLoad += SetTimestamp;
			SaveEvents.AfterReturnToTitle += ResetTimestamp;
		}

		private long timestamp = 0;

		protected override void Dispose(bool disposing) {
			DiscordRpc.Shutdown();
		}

		private void SetTimestamp(object sender, EventArgs e) {
			timestamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).Ticks /
				TimeSpan.TicksPerSecond;
		}

		private void ResetTimestamp(object sender, EventArgs e) {
			timestamp = -1;
		}

		private void DoUpdate(object sender, EventArgs e) {
			RichPresence presence = new RichPresence();
			presence.largeImageText = Helper.Reflection.GetField<string>
				(typeof(Game1), "debugPresenceString").GetValue();
			if (Context.IsWorldReady) {
				if (!Context.IsMultiplayer)
					presence.state = "Playing Solo";
				else if (Context.IsMainPlayer)
					presence.state = "Hosting Co-op";
				else
					presence.state = "Playing Co-op";
				presence.details = $"{FarmName()} ({Game1.player.Money}g)";
				if (timestamp >= 0)
					presence.startTimestamp = timestamp;
				if (Context.IsMultiplayer) {
					presence.partySize = Game1.numberOfPlayers();
					presence.partyMax = Game1.getFarm()
						.getNumberBuildingsConstructed("Cabin") + 1;
					presence.partyId = Constants.SaveFolderName;
				}
				presence.smallImageKey = "weather_" + WeatherKey();
				presence.largeImageKey = $"{Game1.currentSeason}_{FarmTypeKey()}";
				presence.smallImageText = Date();
			} else {
				presence.state = "In Menus";
				presence.smallImageKey = "default_small";
				presence.largeImageKey = "default_large";
			}
			DiscordRpc.UpdatePresence(presence);
		}

		private string Date() {
			SDate date = SDate.Now();
			string season = char.ToUpper(date.Season[0]) +
				date.Season.Substring(1);
			return $"Day {date.Day} of {season}, Year {date.Year}";
		}

		private string FarmName() {
			if (ShowFarmName())
				return Game1.player.farmName.ToString() + " Farm";
			else if (Context.IsMainPlayer)
				return "My Farm";
			else
				return "Someone's Farm";
		}

		private Boolean ShowFarmName() {
			if (!config.ShowFarmName)
				return false;
			string name = Game1.player.farmName.ToString().ToLower();
			foreach (string entry in config.HideFarmNames)
				if (name.Contains(entry.ToLower()))
					return false;
			return true;
		}

		private string FarmTypeKey() {
			switch (Game1.whichFarm) {
				case Farm.default_layout:
					return "standard";
				case Farm.riverlands_layout:
					return "riverland";
				case Farm.forest_layout:
					return "forest";
				case Farm.mountains_layout:
					return "hilltop";
				case Farm.combat_layout:
					return "wilderness";
				default:
					return "default";
			}
		}

		private string WeatherKey() {
			if (Game1.isRaining) {
				if (Game1.isLightning)
					return "stormy";
				else
					return "rainy";
			}
			if (Game1.isDebrisWeather)
				return "windy_" + Game1.currentSeason;
			if (Game1.isSnowing)
				return "snowy";
			if (Game1.weddingToday)
				return "wedding";
			if (Game1.isFestival())
				return "festival";
			return "sunny";
		}
	}
}