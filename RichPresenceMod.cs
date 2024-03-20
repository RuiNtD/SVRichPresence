﻿using DiscordRPC;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using Constants = StardewModdingAPI.Constants;
using LogLevel = StardewModdingAPI.LogLevel;
using Utility = StardewValley.Utility;

namespace SVRichPresence
{
    public class RichPresenceMod : Mod
    {
        private static readonly string clientId = "444517509148966923";
        private static readonly string steamId = "413150";
        private ModConfig config = new();
        private IRichPresenceAPI api;
        private DiscordRpcClient client;

        public override void Entry(IModHelper helper)
        {
            if (Constants.TargetPlatform == GamePlatform.Android)
            {
                Monitor.Log("Discord RPC is not supported on Android.", LogLevel.Error);
                Monitor.Log("Aborting mod initialization.", LogLevel.Error);
                Dispose();
                return;
            }

            api = new RichPresenceAPI(this);
            client = new DiscordRpcClient(clientId,
                autoEvents: false,
                logger: new MonitorLogger(Monitor));
            client.SetSubscription(EventType.Join);
            client.RegisterUriScheme(steamId);
            client.OnReady += (sender, e) =>
            {
                Monitor.Log("Connected to Discord: " + e.User.ToString(), LogLevel.Info);
            };
            client.Initialize();

            #region Console Commands
            Helper.ConsoleCommands.Add("DiscordReload",
                "Reloads the config for Discord Rich Presence.",
                (string command, string[] args) =>
                {
                    LoadConfig();
                    Monitor.Log("Config reloaded.", LogLevel.Info);
                }
            );
            Helper.ConsoleCommands.Add("DiscordFormat",
                "Formats and prints a provided configuration string.",
                (string command, string[] args) =>
                {
                    string text = api.FormatText(string.Join(" ", args));
                    Monitor.Log("Result: " + text, LogLevel.Info);
                }
            );
            Helper.ConsoleCommands.Add("DiscordTags",
                "Lists tags usable for configuration strings.",
                (string command, string[] args) =>
                {
                    IDictionary<string, string> tags =
                        string.Join("", args).ToLower().StartsWith("all") ?
                        api.ListTags("[NULL]", "[ERROR]") : api.ListTags(removeNull: false);
                    IDictionary<string, IDictionary<string, string>> groups =
                        new Dictionary<string, IDictionary<string, string>>();
                    foreach (KeyValuePair<string, string> tag in tags)
                    {
                        string owner = api.GetTagOwner(tag.Key) ?? "Unknown-Mod";
                        if (!groups.ContainsKey(owner))
                            groups[owner] = new Dictionary<string, string>();
                        groups[owner][tag.Key] = tag.Value;
                    }
                    IList<string> output = new List<string>(tags.Count + groups.Count) {
                        "Available Tags:"
                    };
                    int longest = 0;
                    foreach (KeyValuePair<string, string> tag in groups[ModManifest.UniqueID])
                        if (tag.Value != null)
                            longest = Math.Max(longest, tag.Key.Length);
                    int nulls = 0;
                    foreach (KeyValuePair<string, string> tag in groups[ModManifest.UniqueID])
                        if (tag.Value is null) nulls++;
                        else output.Add("  {{ " + tag.Key.PadLeft(longest) + " }}: " + tag.Value);
                    foreach (KeyValuePair<string, IDictionary<string, string>> group in groups)
                    {
                        if (group.Key == ModManifest.UniqueID)
                            continue;
                        string head = group.Value.Count + " tag";
                        if (group.Value.Count != 1)
                            head += "s";
                        head += " from " + (Helper.ModRegistry.Get(group.Key)?.Manifest.Name ?? "an unknown mod");
                        output.Add(head);
                        longest = 0;
                        foreach (KeyValuePair<string, string> tag in group.Value)
                            if (tag.Value != null)
                                longest = Math.Max(longest, tag.Key.Length);
                        foreach (KeyValuePair<string, string> tag in group.Value)
                            if (tag.Value == null) nulls++;
                            else output.Add("  {{ " + tag.Key.PadLeft(longest) + " }}: " + tag.Value);
                    }
                    if (nulls > 0)
                        output.Add(nulls + " tag" + (nulls != 1 ? "s" : "") + " unavailable; type `DiscordTags all` to show all");
                    Monitor.Log(string.Join(Environment.NewLine, output), LogLevel.Info);
                }
            );
            #endregion
            LoadConfig();

            Helper.Events.Input.ButtonReleased += HandleButton;
            Helper.Events.GameLoop.UpdateTicked += DoUpdate;
            Helper.Events.GameLoop.SaveLoaded += SetTimestamp;
            Helper.Events.GameLoop.ReturnedToTitle += SetTimestamp;
            Helper.Events.GameLoop.SaveLoaded += (object sender, SaveLoadedEventArgs e) =>
                api.GamePresence = "Getting Started";
            Helper.Events.GameLoop.SaveCreated += (object sender, SaveCreatedEventArgs e) =>
                api.GamePresence = "Starting a New Game";
            Helper.Events.GameLoop.GameLaunched += (object sender, GameLaunchedEventArgs e) =>
            {
                SetTimestamp();
                timestampSession = Timestamps.Now;
            };

            ITagRegister tagReg = api.GetTagRegister(this);

            #region Default Tags

            tagReg.SetTag("Activity", () => api.GamePresence);
            tagReg.SetTag("ModCount", () => Helper.ModRegistry.GetAll().Count());
            tagReg.SetTag("SMAPIVersion", () => Constants.ApiVersion.ToString());
            tagReg.SetTag("StardewVersion", () => Game1.version);
            tagReg.SetTag("Song", () => Utility.getSongTitleFromCueName(Game1.currentSong?.Name ?? api.None));

            tagReg.SetWorldTag("Name", () => Game1.player.Name);
            tagReg.SetWorldTag("Farm", () => Game1.content.LoadString("Strings\\UI:Inventory_FarmName", api.GetTag("FarmName")));
            tagReg.SetWorldTag("FarmName", () => Game1.player.farmName);
            tagReg.SetWorldTag("PetName", () => Game1.player.hasPet() ? Game1.player.getPetDisplayName() : api.None);
            tagReg.SetWorldTag("Location", () => Game1.currentLocation.Name);
            tagReg.SetWorldTag("RomanticInterest", () => Utility.getTopRomanticInterest(Game1.player)?.getName() ?? api.None);
            tagReg.SetWorldTag("NonRomanticInterest", () => Utility.getTopNonRomanticInterest(Game1.player)?.getName() ?? api.None);

            tagReg.SetWorldTag("Money", () =>
            {
                // Copied from LoadGameMenu.drawSlotMoney
                string cashText = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020", Utility.getNumberWithCommas(Game1.player.Money));
                if (Game1.player.Money == 1 && LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.pt)
                    cashText = cashText.Substring(0, cashText.Length - 1);
                return cashText;
            });
            tagReg.SetWorldTag("MoneyCommas", () => Utility.getNumberWithCommas(Game1.player.Money));
            tagReg.SetWorldTag("MoneyNumber", () => Game1.player.Money);
            tagReg.SetWorldTag("Level", () => Game1.content.LoadString("Strings\\UI:Inventory_PortraitHover_Level", Game1.player.Level.ToString()));
            tagReg.SetWorldTag("LevelNumber", () => Game1.player.Level);
            tagReg.SetWorldTag("Title", () => Game1.player.getTitle());
            tagReg.SetWorldTag("TotalTime", () => Utility.getHoursMinutesStringFromMilliseconds(Game1.player.millisecondsPlayed));
            tagReg.SetWorldTag("TotalTimeLetterFormat", () => (Utility.getHoursMinutesStringFromMilliseconds(Game1.player.millisecondsPlayed)).replace(":", "h"));

            tagReg.SetWorldTag("Health", () => Game1.player.health);
            tagReg.SetWorldTag("HealthMax", () => Game1.player.maxHealth);
            tagReg.SetWorldTag("HealthPercent", () => (double)Game1.player.health / Game1.player.maxHealth * 100, 2);
            tagReg.SetWorldTag("Energy", () => Game1.player.Stamina.ToString());
            tagReg.SetWorldTag("EnergyMax", () => Game1.player.MaxStamina);
            tagReg.SetWorldTag("EnergyPercent", () => (double)Game1.player.Stamina / Game1.player.MaxStamina * 100, 2);

            tagReg.SetWorldTag("Time", () => Game1.getTimeOfDayString(Game1.timeOfDay));
            tagReg.SetWorldTag("Date", () => Utility.getDateString());
            tagReg.SetWorldTag("Season", () => Utility.getSeasonNameFromNumber(SDate.Now().SeasonIndex));
            tagReg.SetWorldTag("DayOfWeek", () => Game1.shortDayDisplayNameFromDayOfSeason(SDate.Now().Day));

            tagReg.SetWorldTag("Day", () => SDate.Now().Day);
            tagReg.SetWorldTag("DayPad", () => $"{SDate.Now().Day:00}");
            tagReg.SetWorldTag("DaySuffix", () => Utility.getNumberEnding(SDate.Now().Day));
            tagReg.SetWorldTag("Year", () => SDate.Now().Year);
            tagReg.SetWorldTag("YearSuffix", () => Utility.getNumberEnding(SDate.Now().Year));

            tagReg.SetWorldTag("GameVerb", () => Context.IsMultiplayer && Context.IsMainPlayer ? "Hosting" : "Playing");
            tagReg.SetWorldTag("GameNoun", () => Context.IsMultiplayer ? "Co-op" : "Solo");
            tagReg.SetWorldTag("GameInfo", () => api.GetTag("GameVerb") + " " + api.GetTag("GameNoun"));
            #endregion
        }

        public override object GetApi() => api;

        private void HandleButton(object sender, ButtonReleasedEventArgs e)
        {
            if (e.Button != config.ReloadConfigButton)
                return;
            try
            {
                LoadConfig();
                Game1.addHUDMessage(new HUDMessage("DiscordRP config reloaded.", HUDMessage.newQuest_type));
            }
            catch (Exception err)
            {
                Game1.addHUDMessage(new HUDMessage("Failed to reload DiscordRP config. Check console.", HUDMessage.error_type));
                Monitor.Log(err.ToString(), LogLevel.Error);
            }
        }

        private void LoadConfig() => config = Helper.ReadConfig<ModConfig>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members")]
        private void SaveConfig() => Helper.WriteConfig(config);

        private Timestamps timestampSession;
        private Timestamps timestampFarm;
        private void SetTimestamp(object sender, EventArgs e) => SetTimestamp();
        private void SetTimestamp() => timestampFarm = Timestamps.Now;

        private void DoUpdate(object sender, UpdateTickedEventArgs e)
        {
            client.Invoke();
            if (e.IsMultipleOf(30))
                client.SetPresence(GetPresence());
        }

        private MenuPresence Conf => !Context.IsWorldReady ?
            config.MenuPresence : config.GamePresence;

        private RichPresence GetPresence()
        {
            var presence = new RichPresence
            {
                Details = api.FormatText(Conf.Details),
                State = api.FormatText(Conf.State)
            };
            var assets = new Assets
            {
                LargeImageKey = "default_large",
                LargeImageText = api.FormatText(Conf.LargeImageText),
                SmallImageText = api.FormatText(Conf.SmallImageText)
            };
            if (Conf.ForceSmallImage || assets.SmallImageText?.Length > 0)
                assets.SmallImageKey = "default_small";

            if (Context.IsWorldReady)
            {
                var conf = (GamePresence)Conf;
                if (conf.ShowSeason)
                    assets.LargeImageKey = $"{Game1.currentSeason}_{FarmTypeKey()}";
                if (conf.ShowWeather)
                    assets.SmallImageKey = "weather_" + WeatherKey();
                if (conf.ShowPlayTime)
                    presence.Timestamps = timestampFarm;
                if (Context.IsMultiplayer)
                    try
                    {
                        presence.Party = new Party
                        {
                            ID = Game1.MasterPlayer.UniqueMultiplayerID.ToString(),
                            Size = Game1.numberOfPlayers(),
                            Max = Game1.getFarm().getNumberBuildingsConstructed("Cabin") + 1
                        };
                    }
                    catch { }
            }

            if (config.ShowGlobalPlayTime)
                presence.Timestamps = timestampSession;
            if (config.AddGetModButton)
                presence.Buttons = new Button[]
                {
                    new Button() { Label = "Get SDV Rich Presence Mod", Url = "https://ruintd.github.io/SVRichPresence/" }
                };

            presence.Assets = assets;
            return presence;
        }

        private string FarmTypeKey()
        {
            if (!((GamePresence)Conf).ShowFarmType)
                return "default";
            switch (Game1.whichFarm)
            {
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

        private string WeatherKey()
        {
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
