namespace AdamStudio.Core.Services
{

    public interface IAppPathService
    {
        string AppDataDirectory { get; }

        #region Setings 

        string SettingsFileName { get; } 
        string SettingsFilePath { get; }

        #endregion

        #region Workspace

        string WorkspaceFileName { get;  }
        string WorkspaceFilePath { get; }

        #endregion

        #region Layout

        string LayoutFileName { get; }
        string LayoutFilePath { get; }

        #endregion

    }

    public class AppPathService : IAppPathService
    {
        private string mAppDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdamStudio");

        public AppPathService()
        {
            InitAppDataDirectory();
        }

        private void InitAppDataDirectory()
        {
            try
            {
                Directory.CreateDirectory(AppDataDirectory);
            }
            catch (Exception)
            {
                // лог ещё не инициализирован — просто переходим на запасной путь
                mAppDataDirectory = Path.Combine(Path.GetTempPath(), "AdamStudio");
                Directory.CreateDirectory(mAppDataDirectory);
            }
        }

        
        public string AppDataDirectory => mAppDataDirectory;

        public string SettingsFileName => "appsettings.json";

        public string SettingsFilePath => Path.Combine(AppDataDirectory, SettingsFileName);

        public string WorkspaceFileName => "workspace.json";

        public string WorkspaceFilePath => Path.Combine(mAppDataDirectory, WorkspaceFileName);

        public string LayoutFileName => "layout.xml";

        public string LayoutFilePath => Path.Combine(mAppDataDirectory, LayoutFileName);
    }
}
