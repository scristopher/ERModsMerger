using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;

namespace ERModsMerger.Core
{
    public class ModsMergerConfig
    {
        public static ModsMergerConfig? LoadedConfig { get; set; }

        

        /// <summary>
        /// Load the config from the specified path
        /// </summary>
        /// <param name="pathConfigFile">Where the config.json file is located</param>
        /// <returns></returns>
        public static ModsMergerConfig? LoadConfig(string pathConfigFile = "ERModsMergerConfig\\config.json")
        {
            
            if (File.Exists(pathConfigFile))
            {
                try
                {
                    LoadedConfig = (ModsMergerConfig?)JsonSerializer.Deserialize(File.ReadAllText(pathConfigFile), typeof(ModsMergerConfig));

                    LoadedConfig.ConfigPath = pathConfigFile;


                    //check if main profile exist
                    if(LoadedConfig.Profiles.Count == 0)
                    {
                        var mainProfile = new ProfileConfig("Main Profile", LoadedConfig.AppDataFolderPath);
                        LoadedConfig.Profiles.Add(mainProfile);
                        LoadedConfig.CurrentProfile = mainProfile;
                    }

                    CheckAndAddEnvVars();
                    CheckVersionAndEmbeddedExtraction();
                    return LoadedConfig;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;
        }


        /// <summary>
        /// Check the current version of assembly, if different (updated) extract (and overwrite) embedded resource to appData path
        /// </summary>
        private static void CheckVersionAndEmbeddedExtraction()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            if (LoadedConfig.ToolVersion != version)
            {
                EmbeddedResourcesExtractor.ExtractAssets();

                LoadedConfig.ToolVersion = version;
            }
        }

        /// <summary>
        /// If valid, add the game path to environment variable "PATH" so DLL IMPORTS works for soulsformats // Oodle
        /// </summary>
        private static void CheckAndAddEnvVars()
        {
            var env = Environment.GetEnvironmentVariable("PATH");
            if (env != null &&
                ModsMergerConfig.LoadedConfig != null &&
                File.Exists(ModsMergerConfig.LoadedConfig.GamePath + "\\regulation.bin") &&
                !env.Split(';').Contains(ModsMergerConfig.LoadedConfig.GamePath))
            {
                Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + ModsMergerConfig.LoadedConfig.GamePath);
            }
        }

        /// <summary>
        /// Save the current loaded config to the specified path
        /// </summary>
        /// <returns></returns>
        public static bool SaveConfig(string pathConfigFile = "ERModsMergerConfig\\config.json")
        {
            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.WriteIndented = true;
                File.WriteAllText(pathConfigFile, JsonSerializer.Serialize(LoadedConfig, typeof(ModsMergerConfig), options));

                LoadedConfig.SaveModEngineConfig();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public static ProfileConfig CreateAndLoadProfile()
        {
            if (!Directory.Exists(LoadedConfig.AppDataFolderPath + "\\Profiles"))
                Directory.CreateDirectory(LoadedConfig.AppDataFolderPath + "\\Profiles");

            int customConfigNameCounter = 1;
            string dirProfile = LoadedConfig.AppDataFolderPath + "\\Profiles\\Custom Profile ";
            while (Directory.Exists(dirProfile + customConfigNameCounter.ToString()))
                customConfigNameCounter++;

            dirProfile = dirProfile + customConfigNameCounter.ToString();


            var profile = new ProfileConfig("Custom Profile " + customConfigNameCounter.ToString(), dirProfile);
            LoadedConfig.Profiles.Add(profile);
            LoadedConfig.CurrentProfile = profile;

            LoadedConfig.Save();

            return profile;
        }



        string _gamePath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\ELDEN RING\\Game";
        public string GamePath { get { return _gamePath; } set { _gamePath = value; } }


        string _appDataFolderPath = "ERModsMergerConfig";
        public string AppDataFolderPath { get { return _appDataFolderPath; } set { _appDataFolderPath = value; } }


        public string ConfigPath { get; set; }

        int _consolePrintDelay = 15;
        public int ConsolePrintDelay { get { return _consolePrintDelay; } set { _consolePrintDelay = value; } }

        string _toolVersion = "";
        public string ToolVersion { get { return _toolVersion; } set { _toolVersion = value; } }

        public ProfileConfig? CurrentProfile { get; set; }

        public List<ProfileConfig> Profiles { get; set; }

        //public List<ModConfig> Mods { get; set; }


        public ModsMergerConfig()
        {
            //Mods = new List<ModConfig>();
            ConfigPath = "";
            Profiles = new List<ProfileConfig>();
        }

        public bool Save()
        {
            try
            {
                if(ConfigPath != "")
                {
                    JsonSerializerOptions options = new JsonSerializerOptions();
                    options.WriteIndented = true;

                    File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, typeof(ModsMergerConfig), options));


                    SaveModEngineConfig();

                    return true;
                }
               
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
        private void SaveModEngineConfig()
        {
            // there is dll mods
            if (LoadedConfig.CurrentProfile != null && LoadedConfig.CurrentProfile.Mods.Count(x => x.IsDllMod && x.Enabled) > 0)
            {
                string modEngineConfig = File.ReadAllText(AppDataFolderPath + "\\ModEngine2\\config_eldenring_prog.toml");
                var dllModsPaths = LoadedConfig.CurrentProfile.Mods.FindAll(x => x.IsDllMod && x.Enabled).Select(x => x.FilePath).ToList();

                string confDlls = "\"" + string.Join("\",\"", dllModsPaths) + "\"";

                modEngineConfig = modEngineConfig.Replace("$DLL_MODS_PATHS", confDlls).Replace("$MOD_DIR", CurrentProfile.MergedModsFolderPath).Replace("\\", "\\\\");
                File.WriteAllText(AppDataFolderPath + "\\ModEngine2\\config_eldenring.toml", modEngineConfig);
            }
            else //set normal modengine config
            {
                string modEngineConfig = File.ReadAllText(AppDataFolderPath + "\\ModEngine2\\config_eldenring_prog.toml");

                modEngineConfig = modEngineConfig.Replace("$DLL_MODS_PATHS", "").Replace("$MOD_DIR", CurrentProfile.MergedModsFolderPath).Replace("\\", "\\\\");
                File.WriteAllText(AppDataFolderPath + "\\ModEngine2\\config_eldenring.toml", modEngineConfig);
            }
        }

    }

    public class ProfileConfig
    {
        public string ProfileName { get; set; }
        public string ProfileDir { get; set; }

        public string ModsToMergeFolderPath { get; set; }

        public string MergedModsFolderPath { get; set; }

        public List<ModConfig> Mods { get; set; }

        public bool Modified { get; set; }

        public ProfileConfig(string profileName, string profileDir)
        {
            ProfileName = profileName;
            ProfileDir = profileDir;

            ModsToMergeFolderPath = profileDir + "\\ModsToMerge";
            MergedModsFolderPath = profileDir + "\\MergedMods";

            if (!Directory.Exists(ProfileDir))
                Directory.CreateDirectory(ProfileDir);

            if (!Directory.Exists(ModsToMergeFolderPath))
                Directory.CreateDirectory(ModsToMergeFolderPath);

            if (!Directory.Exists(MergedModsFolderPath))
                Directory.CreateDirectory(MergedModsFolderPath);


            Mods = new List<ModConfig>();
            Modified = true;
        }

       

        public void Delete()
        {
            if (Directory.Exists(ProfileDir))
                Directory.Delete(ProfileDir, true);

            ModsMergerConfig.LoadedConfig.Profiles.Remove(this);
        }
    }

    public class ModConfig
    {
        public string Name { get; set; }
        public string DirPath { get; set; }
        public bool Enabled { get; set; }
        public string Note { get; set; }

        public bool ImportedFromAnotherProfile { get; set; }

        public bool IsDllMod { get; set; }
        public string FilePath { get; set; }

        public List<FileToMerge> ModFiles { get; set; }

        public ModConfig(string name, string dirPath, bool enabled)
        {
            Name = name;
            DirPath = dirPath;
            Enabled = enabled;
            Note = "";
            ModFiles = new List<FileToMerge>();
            IsDllMod = false;
            FilePath = "";
            ImportedFromAnotherProfile = false;
        }
    }
}
