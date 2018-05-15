using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace SVRichPresence {
	public class ModEntry : Mod {
		public override void Entry(IModHelper helper) {
			GameEvents.UpdateTick += this.DoUpdate;
			DiscordRpc.EventHandlers handlers = new DiscordRpc.EventHandlers();
			DiscordRpc.Initialize("444517509148966923", ref handlers, false, "413150");
		}

		protected override void Dispose(bool disposing) {
			DiscordRpc.Shutdown();
		}

		private void DoUpdate(object sender, EventArgs e) {
			string gamePresence = Helper.Reflection.GetField<string>(typeof(Game1), "debugPresenceString").GetValue();
			DiscordRpc.RichPresence presence = new DiscordRpc.RichPresence();
			presence.largeImageText = gamePresence;
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
				presence.largeImageKey = Game1.currentSeason + "_" + FarmTypeKey();
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
			string season = date.Season.Substring(0, 1).ToUpper() + date.Season.Substring(1);
			return "Day " + date.Day + " of " + season + ", Year " + date.Year;
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

		private string WeatherName() {
			if (Game1.isRaining) {
				if (Game1.isLightning)
					return "Stormy";
				else
					return "Rainy";
			}
			if (Game1.isDebrisWeather)
				return "Windy";
			if (Game1.isSnowing)
				return "Snowy";
			if (Game1.weddingToday)
				return "Wedding Day";
			if (Game1.isFestival())
				return "Festival";
			return "Sunny";
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