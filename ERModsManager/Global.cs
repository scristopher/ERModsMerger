using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERModsManager
{
    public static class Global
    {
        public static string ERMMAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ERModsManager";
        public static string ConfigFilePath = ERMMAppDataFolder + "\\config.json";
    }
}
