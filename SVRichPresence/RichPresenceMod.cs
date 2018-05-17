using System;
using DiscordRPC;
using DiscordRPC.Message;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace SVRichPresence {
	public class RichPresenceMod : Mod {
		private const string clientId = "444517509148966923";
		private DiscordRpcClient client;
		private ModConfig config;

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
			client = new DiscordRpcClient(clientId, "413150", false);
			client.OnReady += OnReady;
			client.OnError += OnError;
			client.OnClose += OnDisconnect;
			client.Initialize();
			config = Helper.ReadConfig<ModConfig>();
			GameEvents.UpdateTick += DoHandle;
			GameEvents.HalfSecondTick += DoUpdate;
			SaveEvents.AfterLoad += SetTimestamp;
			SaveEvents.AfterReturnToTitle += ResetTimestamp;
		}

		private DateTime? timestamp;

		private void OnReady(object sender, ReadyMessage args) {
			User user = args.User;
			Monitor.Log($"Connected to: {user.Username}#{user.Discriminator} ({user.ID})", LogLevel.Info);
		}

		private void OnError(object sender, ErrorMessage args) {
			Monitor.Log($"Error ({args.Code}) : {args.Message}", LogLevel.Error);
		}

		private void OnDisconnect(object sender, CloseMessage args) {
			Monitor.Log($"Disconnected: {args.Reason}", LogLevel.Warn);
		}

		private void SetTimestamp(object sender, EventArgs e) {
			timestamp = DateTime.UtcNow;
		}

		private void ResetTimestamp(object sender, EventArgs e) {
			timestamp = null;
		}

		private void DoHandle(object sender, EventArgs e) {
			client.Invoke();
		}

		private void DoUpdate(object sender, EventArgs e) {
			client.SetPresence(GetPresence());
		}

		private RichPresence GetPresence() {
			string gamePresence = Helper.Reflection.GetField<string>
						(typeof(Game1), "debugPresenceString").GetValue();
			if (Context.IsWorldReady)
				return new RichPresence {
					Details = $"{FarmName()} ({Game1.player.Money}g)",
					State =
						!Context.IsMultiplayer ? "Playing Solo" :
						Context.IsMainPlayer ? "Hosting Co-op" :
						"Playing Co-op",
					Assets = new Assets {
						LargeImageKey = $"{Game1.currentSeason}_{FarmTypeKey()}",
						SmallImageKey = "weather_" + WeatherKey(),
						LargeImageText = gamePresence,
						SmallImageText = Game1.Date.Localize()
					},
					Timestamps = new Timestamps {
						Start = timestamp
					},
					Party = !Context.IsMultiplayer ? null : new Party {
						ID = Game1.MasterPlayer.UniqueMultiplayerID.ToString(),
						Size = Game1.numberOfPlayers(),
						Max = Game1.getFarm()
							.getNumberBuildingsConstructed("Cabin") + 1
					}
				};
			else
				return new RichPresence {
					State = "In Menus",
					Assets = new Assets {
						SmallImageKey = "default_small",
						LargeImageKey = "default_large",
						LargeImageText = gamePresence
					}
				};
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