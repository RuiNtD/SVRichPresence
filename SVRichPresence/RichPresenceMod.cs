using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

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
			handlers.readyCallback = OnReady;
			handlers.disconnectedCallback += OnDisconnect;
			handlers.errorCallback += OnError;
			DiscordRpc.Initialize(applicationId, ref handlers, false, "413150");
			Helper.ConsoleCommands.Add("DiscordRP_Reload",
				"Reloads the config for Discord Rich Presence.",
				(string command, string[] args) => {
					LoadConfig();
					Monitor.Log("Config reloaded.", LogLevel.Info);
				}
			);
			LoadConfig();
			GameEvents.UpdateTick += (object sender, EventArgs e) =>
				DiscordRpc.RunCallbacks();
			GameEvents.HalfSecondTick += (object sender, EventArgs e) =>
				DiscordRpc.UpdatePresence(GetPresence());
			SaveEvents.AfterLoad += (object sender, EventArgs e) =>
				SetTimestamp();
			SaveEvents.AfterReturnToTitle += (object sender, EventArgs e) =>
				SetTimestamp();
			SetTimestamp();
		}

		private void LoadConfig() {
			config = Helper.ReadConfig<ModConfig>();
		}

		private string GamePresence {
			get => Helper.Reflection.GetField<string>
				(typeof(Game1), "debugPresenceString").GetValue();
			set => Helper.Reflection.GetField<string>
				(typeof(Game1), "debugPresenceString").SetValue(value);
		}

		private long timestamp;

		private void OnReady(ref DiscordRpc.DiscordUser user) {
			Monitor.Log($"Connected to {user.username}#{user.discriminator} ({user.userId})", LogLevel.Info);
		}

		private void OnDisconnect(int errorCode, string message) {
			Monitor.Log($"Disconnect {errorCode}: {message}", LogLevel.Warn);
		}

		private void OnError(int errorCode, string message) {
			Monitor.Log($"Error ({errorCode}) : {message}", LogLevel.Error);
		}

		private void SetTimestamp() {
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			timestamp = Convert.ToInt64((DateTime.UtcNow - epoch).TotalSeconds);
		}
		
		private DiscordRpc.RichPresence GetPresence() {
			var presence = new DiscordRpc.RichPresence();
			if (Context.IsWorldReady) {
				presence.details = $"{FarmName()} ({Game1.player.Money}g)";
				presence.largeImageKey = $"{Game1.currentSeason}_{FarmTypeKey()}";
				presence.smallImageKey = "weather_" + WeatherKey();
				presence.largeImageText = GamePresence;
				presence.smallImageText = Utility.getDateString();
				presence.startTimestamp = timestamp;
				if (Context.IsMultiplayer) {
					presence.partyId = Game1.MasterPlayer.UniqueMultiplayerID.ToString();
					presence.partySize = Game1.numberOfPlayers();
					presence.partyMax = Game1.getFarm().getNumberBuildingsConstructed("Cabin") + 1;
					presence.state = Context.IsMainPlayer ? "Hosting Co-op" : "Playing Co-op";
				} else {
					presence.state = "Playing Solo";
				}
			} else {
				presence.state = "In Menus";
				presence.smallImageKey = "default_small";
				presence.largeImageKey = "default_large";
				presence.largeImageText = GamePresence;
			}
			return presence;
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
			string name = Game1.player.farmName.ToString().ToLower() + " farm";
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
