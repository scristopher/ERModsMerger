using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace ERModsMerger.Core
{
    public static class EmbeddedResourcesExtractor
    {

        /// <summary>
        /// Extract and UnZip embedded Assets to the appData folder (path retrived in ERModsMergerConfig)
        /// </summary>
        public static void ExtractAssets()
        {
            string folderPath = ModsMergerConfig.LoadedConfig.AppDataFolderPath;
            string filePath = folderPath + "\\Assets.zip";


            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string[] resNames = Assembly.GetAssembly(typeof(ERModsMerger.Core.ModsMerger)).GetManifestResourceNames();
            foreach (string resName in resNames)
            {
                if (resName.Contains("ERModsMerger.Core.ERModsMergerAssets.Assets.zip"))
                {
                    using (var stream = Assembly.GetAssembly(typeof(ERModsMerger.Core.ModsMerger)).GetManifestResourceStream(resName))
                    {
                        ZipFile.ExtractToDirectory(stream, folderPath, true);
                    }
                }
            }

        }

    }
}
