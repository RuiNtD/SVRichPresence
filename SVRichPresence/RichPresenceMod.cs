using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SVRichPresence {
	public class RichPresenceMod : Mod {
		private const string applicationId = "444517509148966923";
		private ModConfig config = new ModConfig();

		public override void Entry(IModHelper helper) {
#if DEBUG
			Monitor.Log("THIS IS A DEBUG BUILD...", LogLevel.Alert);
			Monitor.Log("...FOR DEBUGGING...", LogLevel.Alert);
			Monitor.Log("...AND STUFF...", LogLevel.Alert);
			if (ModManifest.Version.IsPrerelease()) {
				Monitor.Log("oh wait this is a dev build.", LogLevel.Info);
				Monitor.Log("carry on.", LogLevel.Info);
			} else {
				Monitor.Log("If you're Fayne, keep up the good work. :)", LogLevel.Alert);
				Monitor.Log("If you're not Fayne...", LogLevel.Alert);
				Monitor.Log("...please go yell at Fayne...", LogLevel.Alert);
				Monitor.Log("...because you shouldn't have this...", LogLevel.Alert);
				Monitor.Log("...it's for debugging. (:", LogLevel.Alert);
			}
#else
			if (ModManifest.Version.IsPrerelease()) {
				Monitor.Log("WAIT A MINUTE.", LogLevel.Alert);
				Monitor.Log("FAYNE.", LogLevel.Alert);
				Monitor.Log("WHY DID YOU RELEASE A NON-DEBUG DEV BUILD?!", LogLevel.Alert);
				Monitor.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", LogLevel.Alert);
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
			Helper.ConsoleCommands.Add("DiscordRP_Format",
				"Formats and prints a provided configuration string.",
				(string command, string[] args) => {
					var text = FormatText(String.Join(" ", args));
					Monitor.Log("Result: " + text, LogLevel.Info);
				}
			);
			Helper.ConsoleCommands.Add("DiscordRP_ListTags",
				"List tags usable for config strings.",
				(string command, string[] args) => {
					var tags = GetTags();
					int longest = 0;
					foreach (var key in tags.Keys)
						longest = Math.Max(longest, key.Length);
					IList<string> output = new List<String>(tags.Count);
					foreach (var pair in tags) {
						var key = pair.Key;
						var value = pair.Value;
						var keyPad = key.PadLeft(longest);
						output.Add("{{ " + keyPad + " }}: " + value);
					}
					Monitor.Log("Available Tags:\n" + String.Join("\n", output), LogLevel.Info);
				}
			);
			LoadConfig();
			InputEvents.ButtonReleased += HandleButton;
			GameEvents.HalfSecondTick += DoUpdate;
			SaveEvents.AfterLoad += SetTimestamp;
			SaveEvents.AfterReturnToTitle += SetTimestamp;
			SaveEvents.AfterLoad += (object sender, EventArgs e) =>
				GamePresence = "Getting Started";
			GameEvents.FirstUpdateTick += (object sender, EventArgs e) => {
				SetTimestamp();
				timestampSession = GetTimestamp();
			};
		}

		private void HandleButton(object sender, EventArgsInput e) {
			if (e.Button != config.ReloadConfigButton)
				return;
			try {
				LoadConfig();
				Game1.addHUDMessage(new HUDMessage("DiscordRP config reloaded."));
			} catch (Exception err) {
				Game1.addHUDMessage(new HUDMessage("Failed to reload DiscordRP config. Check console.", 3));
				Monitor.Log(err.ToString(), LogLevel.Error);
			}
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
		
		private MenuPresence Conf {
			get => !Context.IsWorldReady ? config.MenuPresence :
				config.GamePresence;
		}

		private DiscordRpc.RichPresence GetPresence() {
			var presence = new DiscordRpc.RichPresence {
				largeImageKey = "default_large",
				details = FormatText(Conf.Details),
				state = FormatText(Conf.State),
				largeImageText = FormatText(Conf.LargeImageText),
				smallImageText = FormatText(Conf.SmallImageText)
			};
			if (Conf.ForceSmallImage)
				presence.smallImageKey = "default_small";

			if (Context.IsWorldReady) {
				var conf = (GamePresence) Conf;
				if (conf.ShowSeason)
					presence.largeImageKey = $"{Game1.currentSeason}_{FarmTypeKey()}";
				if (conf.ShowWeather)
					presence.smallImageKey = "weather_" + WeatherKey();
				if (conf.ShowPlayTime)
					presence.startTimestamp = timestampFarm;
				if (Context.IsMultiplayer && conf.ShowPlayerCount) {
					presence.partyId = Game1.MasterPlayer.UniqueMultiplayerID.ToString();
					presence.partySize = Game1.numberOfPlayers();
					presence.partyMax = Game1.getFarm().getNumberBuildingsConstructed("Cabin") + 1;
				}
			}
			
			if (presence.smallImageText != null)
				presence.smallImageKey = presence.smallImageKey ?? "default_small";
			if (config.ShowGlobalPlayTime)
				presence.startTimestamp = timestampSession;

			return presence;
		}

		private IDictionary<string, string> GetTags() {
			int modCount = 0;
			foreach (IManifest manifest in Helper.ModRegistry.GetAll())
				modCount++;

			IEqualityComparer<string> comp = StringComparer.InvariantCultureIgnoreCase;
			IDictionary<string, string> tags = new Dictionary<string, string>(comp) {
				["Activity"] = GamePresence,
				["ModCount"] = modCount.ToString(),
				["SMAPIVersion"] = Constants.ApiVersion.ToString(),
				["StardewVersion"] = Game1.version
			};

			// All the tags below are only available while in a farm.
			if (Context.IsWorldReady) {
				var now = SDate.Now();
				tags["FarmName"] = FarmName();
				tags["PlayerName"] = Game1.player.Name;
				tags["Location"] = Game1.currentLocation.Name;

				tags["Money"] = Game1.player.Money.ToString();
				tags["MoneyCommas"] = Utility.getNumberWithCommas(Game1.player.Money);
				tags["Level"] = Game1.player.Level.ToString();
				tags["Title"] = Game1.player.getTitle();

				{
					var totalMinutes = Math.Floor(Game1.player.millisecondsPlayed / 60000.0);
					var hours = Math.Floor(totalMinutes / 60);
					var minutes = totalMinutes - hours * 60;
					tags["TotalTime"] = $"{hours}:{minutes:00}";
				}

				tags["Health"] = Game1.player.health.ToString();
				tags["HealthMax"] = Game1.player.maxHealth.ToString();
				tags["HealthPercent"] = Percent(Game1.player.health, Game1.player.maxHealth).ToString();
				tags["Energy"] = Game1.player.Stamina.ToString();
				tags["EnergyMax"] = Game1.player.MaxStamina.ToString();
				tags["EnergyPercent"] = Percent(Game1.player.Stamina, Game1.player.MaxStamina).ToString();

				tags["Time"] = Game1.getTimeOfDayString(Game1.timeOfDay);
				tags["Date"] = Utility.getDateString();
				var season = now.Season.Substring(0, 1).ToUpper() + now.Season.Substring(1);
				tags["Season"] = season;
				tags["DayOfWeek"] = now.DayOfWeek.ToString();
				tags["DayOfWeekShort"] = now.DayOfWeek.ToString().Substring(0, 3);
				tags["Day"] = now.Day.ToString();
				tags["DayPad"] = $"{now.Day:00}";
				tags["Year"] = now.Year.ToString();

				tags["Weather"] = WeatherName();
				// Condition gives the same result as Weather,
				// but will also give "Wedding Day" and "Festival"
				tags["Condition"] = ConditionName();
				tags["FarmType"] = FarmTypeName();

				// GameVerb and GameNoun are meant to be used together
				tags["GameVerb"] = "Playing";
				tags["GameNoun"] = "Co-op";
				if (!Context.IsMultiplayer)
					tags["GameNoun"] = "Solo";
				else if (Context.IsMainPlayer)
					tags["GameVerb"] = "Hosting";
			}

			return tags;
		}

		private string FormatText(string text) {
			if (text.Length == 0)
				return null;

			// Code is copied and modified from SMAPI.
			var tags = GetTags();
			return Regex.Replace(text, @"{{([ \w\.\-]+)}}", match => {
				string key = match.Groups[1].Value.Trim();
				return tags.TryGetValue(key, out string value)
					? value : match.Value;
			});
		}

		private double Percent(double current, double max) {
			return Math.Round(current / max * 100, 2);
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
			if (config.HideFarmNames.Contains("*"))
				return false;
			string name = Game1.player.farmName.ToString().ToLower() + " farm";
			foreach (string entry in config.HideFarmNames)
				if (name.Contains(entry.ToLower()))
					return false;
			return true;
		}

		private string FarmTypeKey() {
			if (!((GamePresence) Conf).ShowFarmType)
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

		private string FarmTypeName() {
			switch (Game1.whichFarm) {
				case Farm.default_layout:
					return "Standard";
				case Farm.riverlands_layout:
					return "Riverland";
				case Farm.forest_layout:
					return "Forest";
				case Farm.mountains_layout:
					return "Hilltop";
				case Farm.combat_layout:
					return "Wilderness";
				default:
					return "Unknown";
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

		private string WeatherName() {
			if (Game1.isRaining)
				return Game1.isLightning ? "Stormy" : "Rainy";
			if (Game1.isDebrisWeather)
				return "Windy";
			if (Game1.isSnowing)
				return "Snowy";
			return "Sunny";
		}

		private string ConditionName() {
			var weather = WeatherName();
			if (weather != "Sunny")
				return weather;
			if (Game1.weddingToday)
				return "Wedding Day";
			if (Game1.isFestival())
				return "Festival";
			return "Sunny";
		}
	}
}
