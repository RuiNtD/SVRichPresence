using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace SVRichPresence {
	public class ModEntry : Mod {
		public override void Entry(IModHelper helper) {
			GameEvents.HalfSecondTick += this.DoUpdate;
			DiscordRpc.EventHandlers handlers = new DiscordRpc.EventHandlers {
				readyCallback = HandleDiscordReady,
				errorCallback = HandleDiscordError,
				disconnectedCallback = HandleDiscordDisconnected
			};
			DiscordRpc.Initialize("444517509148966923", ref handlers, false, "413150");
		}

		private void HandleDiscordReady(ref DiscordRpc.DiscordUser user) {
			Monitor.Log("Discord Rich Presence Ready");
			Monitor.Log("Logged in as " + user.username + "#" + user.discriminator + " (" + user.userId + ")");
		}

		private void HandleDiscordDisconnected(int errorCode, string message) {
			Monitor.Log("Discord RP Disconnected (" + errorCode + ")");
			Monitor.Log(message);
		}

		private void HandleDiscordError(int errorCode, string message) {
			Monitor.Log("Discord RP Error (" + errorCode + ")");
			Monitor.Log(message);
		}

		private void DoUpdate(object sender, EventArgs e) {
			DiscordRpc.RichPresence presence = new DiscordRpc.RichPresence();
			if (Context.IsWorldReady) {
				if (!Context.IsMultiplayer)
					presence.state = "Playing Solo";
				else if (Context.IsMainPlayer)
					presence.state = "Hosting Co-op";
				else
					presence.state = "Playing Co-op";
				presence.details = Game1.player.farmName.ToString() + " Farm (" + Game1.player.Money + "G)";
				if (Context.IsMultiplayer) {
					presence.partySize = Game1.numberOfPlayers();
					presence.partyMax = Game1.getFarm().getNumberBuildingsConstructed("Cabin") + 1;
					presence.partyId = Constants.SaveFolderName;
				}
				presence.smallImageKey = "weather_" + WeatherKey();
				presence.smallImageText = WeatherName() + " (" + Game1.getTimeOfDayString(Game1.timeOfDay) + ")";
				presence.largeImageKey = SeasonKey() + "_" + FarmTypeKey();
				var date = SDate.Now();
				var season = date.Season.Substring(0, 1).ToUpper() + date.Season.Substring(1);
				presence.largeImageText = "Day " + date.Day + " of " + season + ", Year " + date.Year;
			} else {
				presence.state = "On the Title Screen";
				presence.smallImageKey = "default_small";
				presence.largeImageKey = "default_large";
			}
			DiscordRpc.UpdatePresence(presence);
		}

		private string FarmTypeKey() {
			return "default";
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

		private string WeatherName() {
			if (Game1.isRaining) {
				if (Game1.isLightning)
					return "Storm";
				else
					return "Rain";
			}
			if (Game1.isDebrisWeather)
				return "Wind";
			if (Game1.isSnowing)
				return "Snow";
			if (Game1.weddingToday)
				return "Wedding Day";
			if (Game1.isFestival())
				return "Festival";
			return "Sun";
		}

		private string WeatherKey() {
			if (Game1.isRaining)
				return "rainy";
			if (Game1.isDebrisWeather)
				return "windy_" + Game1.currentSeason;
			if (Game1.isSnowing)
				return "snowy";
			return "sunny";
		}

		private string SeasonKey() {
			switch (Game1.currentSeason) {
				case "spring":
					return "spring";
				case "summer":
					return "summer";
				case "fall":
					return "fall";
				case "winter":
					return "winter";
				default:
					return "spring";
			}
		}
	}
}