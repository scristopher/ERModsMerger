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
                    mainLog.AddSubLog($"Modded {files[0].ModRelativePath} - Loaded ✓", LOGTYPE.SUCCESS);
                }
                catch (Exception e)
                {
                    mainLog.AddSubLog($"Could not load {files[f].Path}", LOGTYPE.ERROR);
                    break;
                }

                try
                {
                    for (int fmgFileIndex = 0; fmgFileIndex < fmgs_to_merge.Count; fmgFileIndex++)
                    {
                        status = "Merging " + files[0].ModRelativePath + " [" + (f+1).ToString() + "/" + files.Count.ToString() + "]";
                        percent = Math.Round(((double)fmgFileIndex / (double)fmgs_to_merge.Count) * 100, 0).ToString();

                        Console.Write($"\r{status} - Progress: {percent}%                                     ");

                        foreach (var entry_to_merge in fmgs_to_merge[fmgFileIndex].Entries)
                        {
                            var entry_id_to_merge = entry_to_merge.ID;
                            var entry_found_in_base = base_fmgs[fmgFileIndex].Entries.Find(x => x.ID == entry_id_to_merge);
                            
                            if (hasVanillaReference)
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
                    Console.Write($"\r{status} - Progress: 100%                                     ");

                }
                catch (Exception)
                {

                    mainLog.AddSubLog($"Error during merging {files[f].Path}", LOGTYPE.ERROR);
                }


            }

            try
            {
                Console.WriteLine();
                
                base_msgbnd.Save(ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\" + files[0].ModRelativePath);
                mainLog.AddSubLog("Saving modded .msgbnd.dcx file to: " + ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\" + files[0].ModRelativePath, LOGTYPE.SUCCESS);

            }
            catch (Exception)
            {
                mainLog.AddSubLog($"Error during saving modded .msgbnd.dcx file in {ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\" + files[0].ModRelativePath}",
                    LOGTYPE.ERROR);

            }

        }
    }
}
