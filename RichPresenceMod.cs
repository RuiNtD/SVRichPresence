using DiscordRPC;
using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using static System.Net.Mime.MediaTypeNames;
using Constants = StardewModdingAPI.Constants;
using LogLevel = StardewModdingAPI.LogLevel;
using Utility = StardewValley.Utility;

namespace SVRichPresence {
  public class RichPresenceMod : Mod {
    private static readonly string clientId = "444517509148966923";
    private static readonly string steamId = "413150";
    private ModConfig Config = new();
    private IRichPresenceAPI api;
    private DiscordRpcClient client;

    public override void Entry(IModHelper helper) {
      if (Constants.TargetPlatform == GamePlatform.Android) {
        Monitor.Log(Helper.Translation.Get("console.androidNotSupported"), LogLevel.Error);
        Monitor.Log(Helper.Translation.Get("console.modInitialisationAbort"), LogLevel.Error);
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
        Monitor.Log($"{Helper.Translation.Get("console.connectedToDiscord")}: {e.User}", LogLevel.Info);
      client.Initialize();

      #region Console Commands
      Helper.ConsoleCommands.Add("DiscordReload",
        Helper.Translation.Get("command.discordreload.desc"),
        (string command, string[] args) => {
          LoadConfig();
        }
      );
      Helper.ConsoleCommands.Add("DiscordFormat",
        Helper.Translation.Get("command.discordformat.desc"),
        (string command, string[] args) => {
          string text = this.api.FormatText(string.Join(" ", args));
          Monitor.Log($"{Helper.Translation.Get("command.discordformat.result")}: {text}", LogLevel.Info);
        }
      );
      Helper.ConsoleCommands.Add("DiscordTags",
        Helper.Translation.Get("command.discordtags.desc"),
        (string command, string[] args) => {
          bool all = string.Join("", args).ToLower().StartsWith("all");
          string output = $"{Helper.Translation.Get("command.discordtags.availableTags")}:\n";
          output += FormatTags(out _, out int nulls, format: "  {{{0}}}: {1}", pad: true, all: all);
          if (nulls > 0)
            output += $"\n\n{Helper.Translation.Get((nulls > 1 ? "command.discordtags.unavailableTags" : "command.discordtags.unavailableTag"), new {count = nulls})}";
          Monitor.Log(output, LogLevel.Info);
        }
      );
      #endregion
      LoadConfig();

      Helper.Events.GameLoop.GameLaunched += RegisterConfigMenu;
      Helper.Events.Input.ButtonReleased += HandleButton;
      Helper.Events.GameLoop.UpdateTicked += DoUpdate;
      Helper.Events.GameLoop.SaveLoaded += SetTimestamp;
      Helper.Events.GameLoop.ReturnedToTitle += SetTimestamp;
      Helper.Events.GameLoop.SaveLoaded += (object sender, SaveLoadedEventArgs e) =>
          api.GamePresence = Helper.Translation.Get("gamePresence.gettingStarted");
      Helper.Events.GameLoop.SaveCreated += (object sender, SaveCreatedEventArgs e) =>
          api.GamePresence = Helper.Translation.Get("gamePresence.startingNewGame");
      Helper.Events.GameLoop.GameLaunched += (object sender, GameLaunchedEventArgs e) => {
        SetTimestamp();
        timestampSession = Timestamps.Now;
      };

      #region Default Tags
      var mod = ModManifest;
      var ReqWorld = api.ReqWorld;
      var None = api.None;
      var SetTag = api.SetTag;

      SetTag(mod, "Activity", () => api.GamePresence);
      SetTag(mod, "ModCount", () => Helper.ModRegistry.GetAll().Count().ToString());
      SetTag(mod, "SMAPIVersion", () => Constants.ApiVersion.ToString());
      SetTag(mod, "StardewVersion", () => Game1.version);
      SetTag(mod, "RPCModVersion", () => ModManifest.Version.ToString());
      SetTag(mod, "Song", () => Utility.getSongTitleFromCueName(Game1.currentSong?.Name ?? None));

      SetTag(mod, "Name", ReqWorld(() => Game1.player.Name));
      SetTag(mod, "Farm", ReqWorld(() => Game1.content.LoadString("Strings\\UI:Inventory_FarmName", api.FormatTag("FarmName"))));
      SetTag(mod, "FarmName", ReqWorld(() => Game1.player.farmName.ToString()));
      SetTag(mod, "PetName", ReqWorld(() => Game1.player.hasPet() ? Game1.player.getPetDisplayName() : None));
      SetTag(mod, "Location", ReqWorld(() => Game1.currentLocation.Name));
      SetTag(mod, "RomanticInterest", ReqWorld(() => Utility.getTopRomanticInterest(Game1.player)?.getName() ?? None));
      SetTag(mod, "NonRomanticInterest", ReqWorld(() => Utility.getTopNonRomanticInterest(Game1.player)?.getName() ?? None));

      SetTag(mod, "Money", ReqWorld(() => {
        // Copied from LoadGameMenu.drawSlotMoney
        string cashText = Game1.content.LoadString("Strings\\StringsFromCSFiles:LoadGameMenu.cs.11020", Utility.getNumberWithCommas(Game1.player.Money));
        if (Game1.player.Money == 1 && LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.pt)
          cashText = cashText[..^1];
        return cashText;
      }));
      SetTag(mod, "MoneyCommas", ReqWorld(() => Utility.getNumberWithCommas(Game1.player.Money)));
      SetTag(mod, "MoneyNumber", ReqWorld(() => Game1.player.Money.ToString()));
      SetTag(mod, "Level", ReqWorld(() => Game1.content.LoadString("Strings\\UI:Inventory_PortraitHover_Level", Game1.player.Level.ToString())));
      SetTag(mod, "LevelNumber", ReqWorld(() => Game1.player.Level.ToString()));
      SetTag(mod, "Title", ReqWorld(() => Game1.player.getTitle().ToString()));
      SetTag(mod, "TotalTime", ReqWorld(() => Utility.getHoursMinutesStringFromMilliseconds(Game1.player.millisecondsPlayed)));

      SetTag(mod, "Health", ReqWorld(() => Game1.player.health.ToString()));
      SetTag(mod, "HealthMax", ReqWorld(() => Game1.player.maxHealth.ToString()));
      SetTag(mod, "HealthPercent", ReqWorld(() => Math.Round((double)Game1.player.health / Game1.player.maxHealth * 100, 2).ToString()));
      SetTag(mod, "Energy", ReqWorld(() => Game1.player.Stamina.ToString()));
      SetTag(mod, "EnergyMax", ReqWorld(() => Game1.player.MaxStamina.ToString()));
      SetTag(mod, "EnergyPercent", ReqWorld(() => Math.Round((double)Game1.player.Stamina / Game1.player.MaxStamina * 100, 2).ToString()));

      SetTag(mod, "Time", ReqWorld(() => Game1.getTimeOfDayString(Game1.timeOfDay)));
      SetTag(mod, "Date", ReqWorld(() => Utility.getDateString()));
      SetTag(mod, "Season", ReqWorld(() => Utility.getSeasonNameFromNumber(SDate.Now().SeasonIndex)));
      SetTag(mod, "DayOfWeek", ReqWorld(() => Game1.shortDayDisplayNameFromDayOfSeason(SDate.Now().Day)));

      SetTag(mod, "Day", ReqWorld(() => SDate.Now().Day.ToString()));
      SetTag(mod, "DayPad", ReqWorld(() => $"{SDate.Now().Day:00}"));
      SetTag(mod, "DaySuffix", ReqWorld(() => Utility.getNumberEnding(SDate.Now().Day)));
      SetTag(mod, "Year", ReqWorld(() => SDate.Now().Year.ToString()));
      SetTag(mod, "YearSuffix", ReqWorld(() => Utility.getNumberEnding(SDate.Now().Year)));

      SetTag(mod, "GameVerb", ReqWorld(() => Context.IsMultiplayer && Context.IsMainPlayer ? Helper.Translation.Get("hosting") : Helper.Translation.Get("playing")));
      SetTag(mod, "GameNoun", ReqWorld(() => Context.IsMultiplayer ? Helper.Translation.Get("co-op") : Helper.Translation.Get("solo")));
      SetTag(mod, "GameInfo", ReqWorld(() => api.ResolveTag("GameVerb") + " " + api.ResolveTag("GameNoun")));
      #endregion
    }

    private string FormatTags(out int count, out int nulls, string format = "{{{0}}}: {1}", bool pad = false, bool all = false) {
      var tags = api.ResolveAllTags();
      nulls = 0;
      count = 0;

      Dictionary<string, Dictionary<string, string>> groups = new();
      foreach (var tag in tags) {
        string owner = api.GetTagOwner(tag.Key) ?? "";
        owner = Helper.ModRegistry.Get(owner)?.Manifest.Name ?? "";

        if (!groups.ContainsKey(owner))
          groups[owner] = new();

        var val = tag.Value.Value;
        if (!all && val is null) nulls++;
        else {
          val ??= "[NULL]";
          if (!tag.Value.Success) val = "[ERROR]";
          groups[owner][tag.Key] = val;
          count++;
        }
      }

      List<string> output = new(tags.Count + groups.Count);
      void list(Dictionary<string, string> group) {
        int longest = 0;
        if (pad)
          foreach (var tag in group)
            longest = Math.Max(longest, tag.Key.Length);
        foreach (var tag in group)
          output.Add(String.Format(format, tag.Key.PadLeft(longest), tag.Value));
      }
      void section(Dictionary<string, string> group, string name) {
        var count = group.Count;
        if (count == 0) return;

        output.Add("");
        output.Add(Helper.Translation.Get(count > 1 ? "options.tagsFrom" : "options.tagFrom", new { count, name }));

        list(group);
      }
      list(groups[ModManifest.Name]);

      foreach (var group in groups) {
        if (group.Key == ModManifest.Name) continue;
        if (group.Key == "") continue;
        section(group.Value, group.Key);
      }
      section(groups[""], Helper.Translation.Get("options.unknownMods"));

      return string.Join("\n", output);
  }

    public override object GetApi() => api;

    private void RegisterConfigMenu(object sender, GameLaunchedEventArgs e) {
      // get Generic Mod Config Menu's API (if it's installed)
      var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
      if (configMenu is null) return;
      var mod = ModManifest;

      configMenu.Register(mod,
        reset: () => Config = new ModConfig(),
        save: () => SaveConfig()
      );

      configMenu.AddBoolOption(mod,
          name: () => Helper.Translation.Get("options.showGlobalPlaytime"),
          getValue: () => Config.ShowGlobalPlayTime,
          setValue: value => Config.ShowGlobalPlayTime = value
      );
      configMenu.AddBoolOption(mod,
          name: () => Helper.Translation.Get("options.addGetModButton"),
          tooltip: () => Helper.Translation.Get("options.addGetModButton.desc"),
          getValue: () => Config.AddGetModButton,
          setValue: value => Config.AddGetModButton = value
      );

      configMenu.AddSectionTitle(mod, () => Helper.Translation.Get("options.preview"));
      configMenu.AddParagraph(mod, () => {
        var text = api.FormatText(Conf.State) + "\n";
        text += api.FormatText(Conf.Details) + "\n";
        var large = api.FormatText(Conf.LargeImageText);
        if (large.Length > 0) text += $"{Helper.Translation.Get("options.largeImageText")}: {large}\n";
        var small = api.FormatText(Conf.SmallImageText);
        if (small.Length > 0) text += $"{Helper.Translation.Get("options.smallImageText")}: {small}\n";
        return text;
      });

      configMenu.AddSectionTitle(mod, () => Helper.Translation.Get("options.customizePresenceInMenus"));
      RPCModMenuSection(configMenu, Config.MenuPresence);

      configMenu.AddSectionTitle(mod, () => Helper.Translation.Get("options.customizePresenceInGame"));
      RPCModMenuSection(configMenu, Config.GamePresence);
      configMenu.AddBoolOption(mod,
        name: () => Helper.Translation.Get("options.showSeason"),
        tooltip: () => Helper.Translation.Get("options.showSeason.desc"),
        getValue: () => Config.GamePresence.ShowSeason,
        setValue: value => Config.GamePresence.ShowSeason = value
       );
      configMenu.AddBoolOption(mod,
        name: () => Helper.Translation.Get("options.showFarmType"),
        tooltip: () => Helper.Translation.Get("options.showFarmType.desc"),
        getValue: () => Config.GamePresence.ShowFarmType,
        setValue: value => Config.GamePresence.ShowFarmType = value
      );
      configMenu.AddBoolOption(mod,
        name: () => Helper.Translation.Get("options.showWeather"),
        tooltip: () => Helper.Translation.Get("options.showWeather.desc"),
        getValue: () => Config.GamePresence.ShowWeather,
        setValue: value => Config.GamePresence.ShowWeather = value
      );
      configMenu.AddBoolOption(mod,
        name: () => Helper.Translation.Get("options.showPlaytime"),
        tooltip: () => Helper.Translation.Get("options.showPlaytime.desc"),
        getValue: () => Config.GamePresence.ShowPlayTime,
        setValue: value => Config.GamePresence.ShowPlayTime = value
      );

      configMenu.AddPage(mod, "tags", () => "Tags");
      configMenu.AddParagraph(mod, () => {
        string output = FormatTags(out _, out int nulls, pad: false);
        output += $"\n\n{nulls} tag{(nulls != 1 ? "s" : "")} unavailable.";
        return output;
      });
      configMenu.AddPageLink(mod, "alltags", () => Helper.Translation.Get("options.showAllTags"));

      configMenu.AddPage(mod, "alltags", () => Helper.Translation.Get("options.allTags"));
      configMenu.AddParagraph(mod, () =>
        FormatTags(out _, out _, format: "{{{0}}}: {1}", pad: false, all: true)
      );
    }

    private void RPCModMenuSection(IGenericModConfigMenuApi api, MenuPresence conf) {
      var mod = ModManifest;
      api.AddPageLink(mod, "tags", () => Helper.Translation.Get("options.showAvailableTags"));
      api.AddTextOption(mod,
        name: () => Helper.Translation.Get("options.line1"),
        getValue: () => conf.State,
        setValue: value => conf.State = value
      );
      api.AddTextOption(mod,
        name: () => Helper.Translation.Get("options.line2"),
        getValue: () => conf.Details,
        setValue: value => conf.Details = value
      );
      api.AddTextOption(mod,
        name: () => Helper.Translation.Get("options.largeImageText"),
        getValue: () => conf.LargeImageText,
        setValue: value => conf.LargeImageText = value
      );
      api.AddTextOption(mod,
        name: () => Helper.Translation.Get("options.smallImageText"),
        getValue: () => conf.SmallImageText,
        setValue: value => conf.SmallImageText = value
      );
      api.AddBoolOption(mod,
        name: () => Helper.Translation.Get("options.forceSmallImage"),
        tooltip: () => Helper.Translation.Get("options.forceSmallImage.desc"),
        getValue: () => conf.ForceSmallImage,
        setValue: value => conf.ForceSmallImage = value
      );
    }

    private void HandleButton(object sender, ButtonReleasedEventArgs e) {
      if (e.Button != Config.ReloadConfigButton)
        return;
      try {
        LoadConfig();
        Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("console.reloadConfig"), HUDMessage.newQuest_type));
      } catch (Exception err) {
        Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("console.reloadConfig.failed"), HUDMessage.error_type));
        Monitor.Log(err.ToString(), LogLevel.Error);
      }
    }

    private void LoadConfig() => Config = Helper.ReadConfig<ModConfig>();

    private void SaveConfig() => Helper.WriteConfig(Config);

    private Timestamps timestampSession;
    private Timestamps timestampFarm;
    private void SetTimestamp(object sender, EventArgs e) => SetTimestamp();
    private void SetTimestamp() => timestampFarm = Timestamps.Now;

    private void DoUpdate(object sender, UpdateTickedEventArgs e) {
      client.Invoke();
      if (e.IsMultipleOf(30))
        client.SetPresence(GetPresence());
    }

    private MenuPresence Conf => !Context.IsWorldReady ?
        Config.MenuPresence : Config.GamePresence;

    private RichPresence GetPresence() {
      var presence = new RichPresence {
        Details = api.FormatText(Conf.Details),
        State = api.FormatText(Conf.State)
      };
      var smallImageText = api.FormatText(Conf.SmallImageText);
      var assets = new Assets {
        LargeImageKey = "default_large",
        LargeImageText = api.FormatText(Conf.LargeImageText),
        SmallImageText = smallImageText,
      };
      if (Conf.ForceSmallImage || smallImageText.Length > 0)
        assets.SmallImageKey = "default_small";

      if (Context.IsWorldReady) {
        var conf = (GamePresence)Conf;
        if (conf.ShowSeason)
          assets.LargeImageKey = $"{Game1.currentSeason}_{FarmTypeKey()}";
        if (conf.ShowWeather)
          assets.SmallImageKey = "weather_" + WeatherKey();
        if (conf.ShowPlayTime)
          presence.Timestamps = timestampFarm;
        if (Context.IsMultiplayer)
          try {
            presence.Party = new Party {
              ID = Game1.MasterPlayer.UniqueMultiplayerID.ToString(),
              Size = Game1.numberOfPlayers(),
              Max = Game1.getFarm().getNumberBuildingsConstructed("Cabin") + 1
            };
          } catch { }
      }

      if (Config.ShowGlobalPlayTime)
        presence.Timestamps = timestampSession;
      if (Config.AddGetModButton)
        presence.Buttons = new Button[] {
          new() { Label = Helper.Translation.Get("getModButton"), Url = "https://ruintd.github.io/SVRichPresence/" }
        };

      presence.Assets = assets;
      return presence;
    }

    private string FarmTypeKey() {
      if (!Config.GamePresence.ShowFarmType)
        return "default";
      return Game1.whichFarm switch {
        Farm.default_layout => "standard",
        Farm.riverlands_layout => "riverland",
        Farm.forest_layout => "forest",
        Farm.mountains_layout => "hilltop",
        Farm.combat_layout => "wilderness",
        _ => "default",
      };
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
