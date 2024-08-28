namespace Flow.Launcher.Plugin.Weather
{
    public partial class SettingsControl 
    {
        public Settings _settings { get; }

        public SettingsControl(Settings settings)
        {
            _settings = settings;
            InitializeComponent();
        }
    }
}