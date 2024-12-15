using DotNext;
using ERModsMerger.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERModsMerger.Core
{
    public static class FIlesTreeAligner
    {
        public static ModConfig? TryAlignAndCopyToModsToMerge(string path, string modName)
        {
            ModConfig? modConfig = null;

            string pathTemp = ModsMergerConfig.LoadedConfig.AppDataFolderPath + "\\temp";
            if(Directory.Exists(pathTemp))
                Directory.Delete(pathTemp, true);
            Directory.CreateDirectory(pathTemp);

            string pathDLLs = ModsMergerConfig.LoadedConfig.AppDataFolderPath + "\\DLLMods";
            Directory.CreateDirectory(pathDLLs);


            List<string> files = new List<string>();

            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))//is directory
                Utils.FindAllFiles(path, ref files, true);
            else if (Path.GetExtension(path) == ".zip")
            {
                ZipFile.ExtractToDirectory(path, pathTemp);
                Utils.FindAllFiles(pathTemp, ref files, true);
            }


            if (files.Count > 0)
            {
                var dictionary = File.ReadAllLines(ModsMergerConfig.LoadedConfig.AppDataFolderPath + "\\Dictionaries\\EldenRingDictionary.txt").ToList();
                //reformat
                for (int i = 0; i< dictionary.Count(); i++)
                {
                    dictionary[i] = dictionary[i].Replace("/", "\\").Replace("\r", "").Replace("\n", "");
                    if(dictionary[i].StartsWith("\\"))
                        dictionary[i] = dictionary[i].Substring(1);
                }
                    
                //magic is below
                foreach (string file in files)
                {
                    var foundIndex = dictionary.FindIndex(x => x != "" && file.Contains(x));
                    if (foundIndex != -1)
                    {
                        int toSkip = dictionary[foundIndex].Split("\\").Length;

                        string parent = file;
                        for (int i = 0; i < toSkip; i++)
                            parent = Directory.GetParent(parent).FullName;

                        Utils.CopyDirectory(parent, ModsMergerConfig.LoadedConfig.CurrentProfile.ModsToMergeFolderPath + "\\" + modName, true);

                        modConfig = new ModConfig(modName, ModsMergerConfig.LoadedConfig.CurrentProfile.ModsToMergeFolderPath + "\\" + modName, true);

                        break;
                    }
                    else if(Path.GetExtension(file) == ".dll")
                    {
                        string parent = Directory.GetParent(file).FullName;
                        Utils.CopyDirectory(parent, pathDLLs + "\\" + modName, true);

                        string pathDllMod = pathDLLs + "\\" + modName + "\\" + Path.GetFileName(file);

                        modConfig = new ModConfig(modName, Directory.GetParent(pathDllMod).FullName, true);
                        modConfig.IsDllMod = true;
                        modConfig.FilePath = pathDllMod;


                        break;
                    }
                }

                if(modConfig != null)
                {
                    if(!modConfig.IsDllMod)
                    {
                        // delete all files that are not present in dictionary
                        List<string> ToMergefiles = new List<string>();
                        Utils.FindAllFiles(ModsMergerConfig.LoadedConfig.CurrentProfile.ModsToMergeFolderPath + "\\" + modName, ref ToMergefiles, true);
                        ToMergefiles.ForEach(x => {
                            if (!dictionary.Contains(x.Replace(ModsMergerConfig.LoadedConfig.CurrentProfile.ModsToMergeFolderPath + "\\" + modName + "\\", "")))
                            {
                                File.Delete(x);
                            }

                        });
                    }
                   
                }
                
            }

            return modConfig;
        }


    }
}
