using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.Weather
{
    public class Settings
    {
        public bool useFahrenheit {  get; set; } = false;
        public bool useBlackIcons { get; set; } = false;
        public string defaultLocation { get; set; } = null;

    }
}

