using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace SVRichPresence {
	public class RichPresenceMod : Mod {
		private const string applicationId = "444517509148966923";
		private ModConfig config = new ModConfig();

		public override void Entry(IModHelper helper) {
#if DEBUG
			Monitor.Log("THIS IS A DEBUG BUILD...", LogLevel.Alert);
			Monitor.Log("...FOR DEBUGGING...", LogLevel.Alert);
			Monitor.Log("...AND STUFF...", LogLevel.Alert);
			if (!ModManifest.Version.IsPrerelease()) {
				Monitor.Log("If you're Fayne, keep up the good work. :)", LogLevel.Alert);
				Monitor.Log("If you're not Fayne...", LogLevel.Alert);
				Monitor.Log("...please go yell at Fayne...", LogLevel.Alert);
				Monitor.Log("...because you shouldn't have this...", LogLevel.Alert);
				Monitor.Log("...it's for debugging. (:", LogLevel.Alert);
			}
#endif
			var handlers = new DiscordRpc.EventHandlers();
			DiscordRpc.Initialize(applicationId, ref handlers, false, "413150");
			Helper.ConsoleCommands.Add("DiscordRP_Reload",
				"Reloads the config for Discord Rich Presence.",
				(string command, string[] args) => {
					LoadConfig();
					Monitor.Log("Config reloaded.", LogLevel.Info);
				}
			);
			LoadConfig();
			GameEvents.HalfSecondTick += DoUpdate;
			SaveEvents.AfterLoad += SetTimestamp;
			SaveEvents.AfterReturnToTitle += SetTimestamp;
			GameEvents.FirstUpdateTick += (object sender, EventArgs e) => {
				SetTimestamp();
				timestampSession = GetTimestamp();
			};
		}
		
		private void LoadConfig() =>
			config = Helper.ReadConfig<ModConfig>();

		private void SaveConfig() =>
			Helper.WriteConfig<ModConfig>(config);

		private string GamePresence {
			get => Helper.Reflection.GetField<string>
				(typeof(Game1), "debugPresenceString").GetValue();
			set => Helper.Reflection.GetField<string>
				(typeof(Game1), "debugPresenceString").SetValue(value);
		}

		private long timestampSession = 0;
		private long timestampFarm = 0;

		private void SetTimestamp(object sender, EventArgs e) =>
				SetTimestamp();
		private void SetTimestamp() =>
			timestampFarm = GetTimestamp();

		private long GetTimestamp() {
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return Convert.ToInt64((DateTime.UtcNow - epoch).TotalSeconds);
		}

		private void DoUpdate(object sender, EventArgs e) =>
			DiscordRpc.UpdatePresence(GetPresence());
		
		private DiscordRpc.RichPresence GetPresence() {
			var presence = new DiscordRpc.RichPresence();
			if (Context.IsWorldReady) {

				if (config.ShowMoney)
					presence.details = $"{FarmName()} ({Game1.player.Money}g)";
				else if (config.ShowFarmName)
					presence.details = FarmName();

				// Limitation: Can't hide season and show farm type.
				// In that scenario, both are hidden.

				if (config.ShowSeason)
					presence.largeImageKey = $"{Game1.currentSeason}_{FarmTypeKey()}";
				else if (config.ShowActivity)
					// Can't show activity without large image. Use default.
					presence.largeImageKey = "default_large";

				if (config.ShowWeather)
					presence.smallImageKey = "weather_" + WeatherKey();
				else if (config.ShowDate)
					// Can't show date without small image. Use default.
					presence.smallImageKey = "default_small";

				if (config.ShowDate)
					presence.smallImageText = Utility.getDateString();

				if (config.ShowPlayTime)
					presence.startTimestamp = timestampFarm;

				if (!config.ShowCoop)
					presence.state = "Playing";
				else if (Context.IsMultiplayer) {
					presence.partyId = Game1.MasterPlayer.UniqueMultiplayerID.ToString();
					presence.partySize = Game1.numberOfPlayers();
					presence.partyMax = Game1.getFarm().getNumberBuildingsConstructed("Cabin") + 1;
					presence.state = Context.IsMainPlayer ? "Hosting Co-op" : "Playing Co-op";
				} else
					presence.state = "Playing Solo";

			} else {
				presence.state = "In Menus";
				presence.smallImageKey = "default_small";
				presence.largeImageKey = "default_large";
			}

			if (config.ShowPlayTime && config.PlayTimeEntireSession)
				presence.startTimestamp = timestampSession;

			if (config.ShowActivity)
				presence.largeImageText = GamePresence;

			return presence;
		}

		private string FarmName() {
			if (ShowFarmName())
				return Game1.player.farmName.ToString() + " Farm";
			else if (Context.IsMainPlayer && config.ShowCoop)
				return "My Farm";
			else
				return "Someone's Farm";
		}

		private Boolean ShowFarmName() {
			if (!config.ShowFarmName)
				return false;
			if (config.HideFarmNames.Contains("*"))
				return false;
			string name = Game1.player.farmName.ToString().ToLower() + " farm";
			foreach (string entry in config.HideFarmNames)
				if (name.Contains(entry.ToLower()))
					return false;
			return true;
		}

		private string FarmTypeKey() {
			if (!config.ShowFarmType)
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

		private string WeatherKey() {
			if (Game1.isRaining)
				return Game1.isLightning ? "stormy" : "rainy";
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
