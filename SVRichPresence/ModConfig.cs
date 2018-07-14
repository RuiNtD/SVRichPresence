using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace SVRichPresence {
	class ModConfig {
		public SButton ReloadConfigButton = SButton.F5;
		public Boolean ShowGlobalPlayTime = false;
		public MenuPresence MenuPresence = new MenuPresence();
		public GamePresence GamePresence = new GamePresence();
	}
	class MenuPresence {
		public Boolean ForceSmallImage = false;
		public string Details = "";
		public string State = "In Menus";
		public string LargeImageText = "{{ Activity }}";
		public string SmallImageText = "";
	}
	class GamePresence : MenuPresence {
		public Boolean ShowSeason = true;
		public Boolean ShowFarmType = true;
		public Boolean ShowWeather = true;
		public Boolean ShowPlayerCount = true;
		public Boolean ShowPlayTime = true;

		public GamePresence() {
			Details = "{{ FarmName }} | {{ Money }}";
			State = "{{ GameInfo }}";
			SmallImageText = "{{ Date }}";
		}
	}
}
