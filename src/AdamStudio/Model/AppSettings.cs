using CommunityToolkit.Mvvm.ComponentModel;

namespace AdamStudio.Model
{
    public sealed partial class AppSettings : ObservableObject
    {
        [ObservableProperty]
        public partial string ThemeName { get; set; } = "VS2013Light";
        [ObservableProperty]
        public partial double WindowWidth { get; set; } = 1100;
        [ObservableProperty]
        public partial double WindowHeight { get; set; } = 700;
        [ObservableProperty]
        public partial double WindowX { get; set; } = 100;
        [ObservableProperty]
        public partial double WindowY { get; set; } = 100;
        [ObservableProperty]
        public partial bool IsMaximized { get; set; }
        [ObservableProperty]
        public partial string EditorFontFamily { get; set; } = "Consolas";
        [ObservableProperty]
        public partial int EditorFontSize { get; set; } = 14;
        [ObservableProperty]
        public partial bool ShowLineNumbers { get; set; } = true;
        [ObservableProperty]
        public partial bool WordWrap { get; set; }
        [ObservableProperty]
        public partial bool EnableOutputPanel { get; set; } = true;
        [ObservableProperty]
        public partial bool EnableFile { get; set; } = true;
        [ObservableProperty]
        public partial int RetainedFileCountLimit { get; set; } = 14;
        [ObservableProperty]
        public partial string OutputLogLevel { get; set; } = "Information";
        [ObservableProperty]
        public partial string GlobalLogLevel { get; set; } = "Debug";
    }
}
