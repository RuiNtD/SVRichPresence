using StardewModdingAPI;

namespace SVRichPresence {
  internal class ModConfig {
    public SButton ReloadConfigButton = SButton.F5;
    public bool ShowGlobalPlayTime = false;
    public MenuPresence MenuPresence = new();
    public GamePresence GamePresence = new();
    public bool AddGetModButton = true;
  }

  internal class MenuPresence {
    public bool ForceSmallImage = false;
    public string State = "In Menus";
    public string Details = "";
    public string LargeImageText = "{{ Activity }}";
    public string SmallImageText = "";
  }

  internal class GamePresence : MenuPresence {
    public bool ShowSeason = true;
    public bool ShowFarmType = true;
    public bool ShowWeather = true;
    public bool ShowPlayTime = true;

    public GamePresence() {
      State = "{{ GameInfo }}";
      Details = "{{ Farm }} | {{ Money }}";
      SmallImageText = "{{ Date }}";
    }
  }
}
