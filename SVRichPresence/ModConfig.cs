using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVRichPresence {
	class ModConfig {
		public Boolean ShowCoop = true;
		public Boolean ShowDate = true;
		public Boolean ShowActivity = true;
		public Boolean ShowPlayTime = true;
		public Boolean ShowWeather = true;
		public Boolean ShowFarmName = true;
		public Boolean ShowSeason = true;
		public Boolean ShowFarmType = true;
		public Boolean ShowMoney = true;
		public Boolean PlayTimeEntireSession = false;

		public List<string> HideFarmNames = new List<string>();
	}
}
