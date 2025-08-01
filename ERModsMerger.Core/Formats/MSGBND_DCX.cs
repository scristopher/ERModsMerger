using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ERModsMerger.Core.Utility;
using ERModsMerger.Core.BHD5Handler;

namespace ERModsMerger.Core.Formats
{
    internal class MSGBND_DCX
    {
        List<FMG> FMGs {  get; set; }
        BND4 FmgBinder { get; set; }
        public MSGBND_DCX(string path, string searchVanillaRelativePath = "")
        {
            if (searchVanillaRelativePath != "")
                FmgBinder = BND4.Read(BHD5Reader.Read(searchVanillaRelativePath));
            else
                FmgBinder = BND4.Read(path);


            FMGs = new List<FMG>();
            foreach (BinderFile file in FmgBinder.Files)
            {
                FMGs.Add(FMG.Read(file.Bytes));
            }

        }

        public void Save(string path)
        {
            for (int i = 0; i < FMGs.Count; i++)
            {
                FmgBinder.Files[i].Bytes = FMGs[i].Write();
            }

            FmgBinder.Write(path);
        }

        public static void MergeFiles(List<FileToMerge> files)
        {
            Console.Write("\n");
            string status = "";
            string percent = "0";

            var mainLog = LOG.Log("Merging Messages (MSGBND)");

            List<FMG> vanilla_fmgs = new List<FMG>();
            bool hasVanillaReference = false;
            try
            {
                vanilla_fmgs = new MSGBND_DCX("", files[0].ModRelativePath).FMGs;
                hasVanillaReference = true;
                mainLog.AddSubLog($"Vanilla {files[0].ModRelativePath} - Loaded ✓\n", LOGTYPE.SUCCESS);
            }
            catch (Exception e)
            {
                mainLog.AddSubLog($"Could not load vanilla msgbnd.dcx file - merging without vanilla reference\n", LOGTYPE.WARNING);
                mainLog.AddSubLog($"Vanilla loading error details: {e.GetType().Name}: {e.Message}\n", LOGTYPE.INFO);
                hasVanillaReference = false;
            }

            MSGBND_DCX? base_msgbnd = null;
            List<FMG> base_fmgs = new List<FMG>();
            try
            {
                base_msgbnd = new MSGBND_DCX(files[0].Path);
                base_fmgs = base_msgbnd.FMGs;
                mainLog.AddSubLog($"Initial modded {files[0].ModRelativePath} - Loaded ✓\n", LOGTYPE.SUCCESS);
            }
            catch (Exception e)
            {
                mainLog.AddSubLog($"Could not load {files[0].Path}\n", LOGTYPE.ERROR);
                mainLog.AddSubLog($"Base file loading error details: {e.GetType().Name}: {e.Message}\n", LOGTYPE.ERROR);
                return;
            }

            if (hasVanillaReference)
            {
                mainLog.AddSubLog("Using vanilla file comparison merge strategy", LOGTYPE.INFO);
            }
            else
            {
                mainLog.AddSubLog("Using fallback merge strategy (no vanilla reference)", LOGTYPE.INFO);
            }
            
            for (var f = 1; f < files.Count; f++)
            {
                List<FMG> fmgs_to_merge = new List<FMG>();
                try
                {
                    fmgs_to_merge = new MSGBND_DCX(files[f].Path).FMGs;
                    mainLog.AddSubLog($"Modded {files[f].ModRelativePath} - Loaded ✓", LOGTYPE.SUCCESS);
                    
                    // Validate FMG structure compatibility
                    if (fmgs_to_merge.Count != base_fmgs.Count)
                    {
                        mainLog.AddSubLog($"Warning: FMG count mismatch - Base: {base_fmgs.Count}, Mod: {fmgs_to_merge.Count}. Proceeding with caution.", LOGTYPE.WARNING);
                    }
                }
                catch (Exception e)
                {
                    mainLog.AddSubLog($"Could not load {files[f].Path}", LOGTYPE.ERROR);
                    mainLog.AddSubLog($"Mod file loading error details: {e.GetType().Name}: {e.Message}", LOGTYPE.ERROR);
                    continue; // Continue with next file instead of breaking
                }

                try
                {
                    int maxFmgIndex = Math.Min(fmgs_to_merge.Count, base_fmgs.Count);
                    for (int fmgFileIndex = 0; fmgFileIndex < maxFmgIndex; fmgFileIndex++)
                    {
                        status = "Merging " + files[f].ModRelativePath + " [" + (f+1).ToString() + "/" + files.Count.ToString() + "]";
                        percent = Math.Round(((double)fmgFileIndex / (double)maxFmgIndex) * 100, 0).ToString();

                        Console.Write($"\r{status} - Progress: {percent}%                                     ");

                        foreach (var entry_to_merge in fmgs_to_merge[fmgFileIndex].Entries)
                        {
                            var entry_id_to_merge = entry_to_merge.ID;
                            var entry_found_in_base = base_fmgs[fmgFileIndex].Entries.Find(x => x.ID == entry_id_to_merge);
                            
                            if (hasVanillaReference && fmgFileIndex < vanilla_fmgs.Count)
                            {
                                // Original logic with vanilla file comparison
                                var entry_found_in_vanilla = vanilla_fmgs[fmgFileIndex].Entries.Find(x=>x.ID==entry_id_to_merge);
                                
                                if(entry_found_in_vanilla != null && !Utils.AdvancedEquals(entry_to_merge.Text, entry_found_in_vanilla.Text))
                                {
                                    if (entry_found_in_base == null)
                                        base_fmgs[fmgFileIndex].Entries.Insert(fmgs_to_merge[fmgFileIndex].Entries.IndexOf(entry_to_merge), entry_to_merge);
                                    else
                                        entry_found_in_base.Text = entry_to_merge.Text;
                                }
                                else if(entry_found_in_vanilla == null)
                                {
                                    if (entry_found_in_base == null)
                                        base_fmgs[fmgFileIndex].Entries.Insert(fmgs_to_merge[fmgFileIndex].Entries.IndexOf(entry_to_merge), entry_to_merge);
                                    else
                                        entry_found_in_base.Text = entry_to_merge.Text;
                                }
                            }
                            else
                            {
                                // Fallback logic without vanilla file comparison - merge all entries
                                if (entry_found_in_base == null)
                                {
                                    // Add new entry if it doesn't exist in base
                                    base_fmgs[fmgFileIndex].Entries.Add(entry_to_merge);
                                }
                                else
                                {
                                    // Override existing entry with the new one (higher priority)
                                    entry_found_in_base.Text = entry_to_merge.Text;
                                }
                            }
                        }
                    }
                    
                    // Handle extra FMG files if the mod has more than the base
                    if (fmgs_to_merge.Count > base_fmgs.Count)
                    {
                        mainLog.AddSubLog($"Mod has {fmgs_to_merge.Count - base_fmgs.Count} additional FMG files - these will be skipped", LOGTYPE.WARNING);
                    }
                    
                    Console.Write($"\r{status} - Progress: 100%                                     ");

                }
                catch (Exception e)
                {
                    mainLog.AddSubLog($"Error during merging {files[f].Path}: {e.GetType().Name}: {e.Message}", LOGTYPE.ERROR);
                    if (e.InnerException != null)
                    {
                        mainLog.AddSubLog($"Inner exception: {e.InnerException.GetType().Name}: {e.InnerException.Message}", LOGTYPE.ERROR);
                    }
                }


            }

            try
            {
                Console.WriteLine();
                
                string outputPath = ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\" + files[0].ModRelativePath;
                
                // Ensure output directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                
                base_msgbnd.Save(outputPath);
                mainLog.AddSubLog("Saving modded .msgbnd.dcx file to: " + outputPath, LOGTYPE.SUCCESS);

            }
            catch (Exception e)
            {
                string outputPath = ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\" + files[0].ModRelativePath;
                mainLog.AddSubLog($"Error during saving modded .msgbnd.dcx file to {outputPath}: {e.GetType().Name}: {e.Message}",
                    LOGTYPE.ERROR);
                if (e.InnerException != null)
                {
                    mainLog.AddSubLog($"Save inner exception: {e.InnerException.GetType().Name}: {e.InnerException.Message}", LOGTYPE.ERROR);
                }
            }

        }
    }
}
