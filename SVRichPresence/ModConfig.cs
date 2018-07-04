using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace SVRichPresence {
	class ModConfig {
		public SButton ReloadConfigButton = SButton.F5;
		public Boolean ShowGlobalPlayTime = false;
		public List<string> HideFarmNames = new List<string>();
		public MenuPresence MenuPresence = new MenuPresence();
		public GamePresence GamePresence = new GamePresence();
	}
	class MenuPresence {
		public string Details = "";
		public string State = "In Menus";
		public string LargeImageText = "{{ Activity }}";
		public string SmallImageText = "";
		public Boolean ForceSmallImage = false;
	}
	class GamePresence : MenuPresence {
		public Boolean ShowSeason = true;
		public Boolean ShowFarmType = true;
		public Boolean ShowWeather = true;
		public Boolean ShowPlayerCount = true;
		public Boolean ShowPlayTime = true;

		public GamePresence() {
			Details = "{{ FarmName }} ({{ MoneyCommas }}g)";
			State = "{{ GameVerb }} {{ GameNoun }}";
			SmallImageText = "{{ Date }}";
		}
	}
}
