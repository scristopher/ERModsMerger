using DotNext.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using ERModsMerger.Core.Utility;

namespace ERModsMerger.Core
{
    public class ModsMerger
    {
        public static void StartMerge(bool manualConflictResolving = false, bool requestUserConfirmations = true)
        {
            var logconfig = LOG.Log("Retrieve configuration", LOGTYPE.INFO);

            ModsMergerConfig config = ModsMergerConfig.LoadedConfig;

            if (config == null)
            {
                logconfig.AddSubLog("Could not load Config at ERModsMergerConfig\\config.json\n   If you have made modifications, please verify if everything is correct.",
                        LOGTYPE.ERROR);

                OnMergeFinish(false);
                return;
            }

            logconfig.AddSubLog("Configuration loaded", LOGTYPE.SUCCESS);

            string[] dirs = Directory.GetDirectories(config.CurrentProfile.ModsToMergeFolderPath);

            if((dirs != null && dirs.Length > 0) || ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.Count > 0)
            {

                List<string> modsDirectories = dirs.OrderByDescending(q => q).ToList();
                var dispatcher = new MergeableFilesDispatcher();

                //if mods are present in config with special order, change modsDirectories
                List<string> ignoredFiles = new List<string>();
                if (ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.Count > 0)
                {
                    //reverse the mod list to comply with priority order and exclude dll mods for the merge
                    List<ModConfig> mods = new List<ModConfig>();
                    foreach (var mod in ModsMergerConfig.LoadedConfig.CurrentProfile.Mods.FindAll(x=>x.Enabled && !x.IsDllMod))
                        mods.Insert(0, mod);


                    foreach (var mod in mods)
                    {
                        mod.ModFiles.ForEach(x => 
                        { 
                            if (x.Enabled && !x.IsDirectory) 
                                dispatcher.AddFile(x.Path, x.ModRelativePath);
                            else if(!x.Enabled && !x.IsDirectory)
                                ignoredFiles.Add(x.Path);
                        });
                    }
                }
                else // console behavior // no profile set
                {

                    //Search all files in directories, add them to the dispatcher and then search which files are conflicting

                    var allFiles = new List<string>();
                    foreach (string modsDirectory in modsDirectories)
                        Utils.FindAllFiles(modsDirectory, ref allFiles, true);


                    foreach (string file in allFiles)
                        dispatcher.AddFile(file);
                }


               

                dispatcher.SearchForConflicts();

                
                var unsuportedFilesConflicts = dispatcher.Conflicts.FindAll(x => !x.SupportedFormat);
                if (unsuportedFilesConflicts.Count > 0)
                {
                    LOG.Log($"{unsuportedFilesConflicts.Count} unsupported conflict(s) found:\n",
                        LOGTYPE.WARNING);

                    foreach (var conflict in unsuportedFilesConflicts)
                    {
                        LOG.Log("- " + conflict.FilesToMerge[0].ModRelativePath + ":",
                            LOGTYPE.WARNING);
                        conflict.FilesToMerge.ForEach((x) => { Console.WriteLine("      - " + x.Path); });
                        Console.WriteLine();
                    }
                        

                    if(requestUserConfirmations)
                    {
                        var answer = LOG.QueryUserYesNoQuestion("Due to unsuported conflicts, some file(s) will be completely overwriten and this will potentially cause in-game issues. Are you sure you want to continue?");

                        if (!answer)
                        {
                            OnMergeFinish(false);
                            return;
                        } 
                    }
                }

                Console.WriteLine();
                
                LOG.Log("Initial directories merge");
                
                if(Directory.Exists(config.CurrentProfile.MergedModsFolderPath))
                    Directory.Delete(config.CurrentProfile.MergedModsFolderPath, true);

                Directory.CreateDirectory(config.CurrentProfile.MergedModsFolderPath);

                foreach (string modsDirectory in modsDirectories)
                    Utils.CopyDirectory(modsDirectory, config.CurrentProfile.MergedModsFolderPath, true, ignoredFiles);
                


                dispatcher.MergeAllConflicts(manualConflictResolving);

                
            }
            else
            {
                LOG.Log($"No mod folder(s) could be found in {config.CurrentProfile.ModsToMergeFolderPath}\n⚠ Verify if everything is placed well like in example.\n⚠ Relaunch and look at example to see expected folders placement.",
                    LOGTYPE.ERROR);

                OnMergeFinish(false);
            }

            

            OnMergeFinish(true);
        }

        public delegate void MergeFinishEventHandler(bool finished);

        public static event MergeFinishEventHandler MergeFinish;

        protected static void OnMergeFinish(bool finished)
        {
            if (MergeFinish != null)
            {
                if(finished)
                    LOG.Log("Merging Done!", LOGTYPE.SUCCESS);
                else
                    LOG.Log("Merging failed!", LOGTYPE.ERROR);

                MergeFinish(finished);
            }
        }


    }
}
