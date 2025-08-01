using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNext.Collections.Generic;
using System.IO;
using SoulsFormats;
using ERModsMerger.Core.Utility;
using ERModsMerger.Core.BHD5Handler;

namespace ERModsMerger.Core.Formats
{
    internal class EMEVD_DCX
    {
        public EMEVD Emevd {  get; set; }

        public EMEVD_DCX(string path, string searchVanillaRelativePath = "") 
        {
            if(searchVanillaRelativePath != "")
                Emevd = EMEVD.Read(BHD5Reader.Read(searchVanillaRelativePath));
            else
                Emevd =  EMEVD.Read(path);
            
        }


        /// <summary>
        /// First and quick version of merging internal events. 
        /// If 2 or more mods tries to modify the same event ID, event will be rewrited.
        /// TODO: instructions analyze / compare and merge
        /// </summary>
        public static void MergeFiles(List<FileToMerge> files)
        {
            
            Console.WriteLine();

            var mainLog = LOG.Log("Merging Events (EMEVD)");

            EMEVD? vanilla_emevd;
            try
            {
                vanilla_emevd = new EMEVD_DCX("", files[0].ModRelativePath).Emevd;
                mainLog.AddSubLog($"Vanilla {files[0].ModRelativePath} - Loaded ✓\n", LOGTYPE.SUCCESS);
            }
            catch (Exception)
            {
                mainLog.AddSubLog($"Could not load vanilla event file\n", LOGTYPE.ERROR);
                return;
            }

            EMEVD? base_emevd;
            try
            {
                base_emevd = new EMEVD_DCX(files[0].Path).Emevd;
                mainLog.AddSubLog($"Initial modded event: {files[0].Path} - Loaded ✓\n", LOGTYPE.SUCCESS);
            }
            catch (Exception)
            {
                mainLog.AddSubLog($"Could not load {files[0].Path}\n", LOGTYPE.ERROR);
                return;
            }


            for (var f = 1; f < files.Count; f++)
            {

                EMEVD? emevd_to_merge;
                try
                {
                    emevd_to_merge = new EMEVD_DCX(files[f].Path).Emevd;
                    mainLog.AddSubLog($"Modded event: {files[f].Path} - Loaded ✓\n");
                }
                catch (Exception)
                {
                    mainLog.AddSubLog($"Could not load {files[f].Path}\n", LOGTYPE.ERROR);
                    break;
                }

                try
                {
                    //foreach events
                    for (var e = 0; e < emevd_to_merge.Events.Count; e++)
                    {
                        var event_id_to_merge = emevd_to_merge.Events[e].ID;
                        var event_found_in_vanilla = vanilla_emevd.Events.Find(x => x.ID == event_id_to_merge);
                        var event_found_in_base = base_emevd.Events.Find(x => x.ID == event_id_to_merge);

                        //event dont exist in vanilla_emevd, we add it
                        if (event_found_in_vanilla == null || event_found_in_base == null)
                        {
                            base_emevd.Events.Add(emevd_to_merge.Events[e]);
                        }
                        else
                        {
                            //foreach instruction
                            for (var i = 0; i < emevd_to_merge.Events[e].Instructions.Count; i++)
                            {
                                var instruction_to_merge = emevd_to_merge.Events[e].Instructions[i];
                                EMEVD.Instruction? vanilla_instruction = null;
                                if (event_found_in_vanilla.Instructions.Count > i)
                                    vanilla_instruction = event_found_in_vanilla.Instructions[i];

                                EMEVD.Instruction? base_instruction = null;
                                if (event_found_in_base.Instructions.Count > i)
                                    base_instruction = event_found_in_base.Instructions[i];


                                //instruction to merge is different from vanilla
                                if (vanilla_instruction == null
                                    || !Utils.AdvancedEquals(instruction_to_merge.ArgData, vanilla_instruction.ArgData)
                                    || instruction_to_merge.ID != vanilla_instruction.ID
                                    || instruction_to_merge.Layer != vanilla_instruction.Layer
                                    || instruction_to_merge.Bank != vanilla_instruction.Bank)
                                {
                                    //rewrite the event when instruction differs from vanilla
                                    // TODO: instructions analyze and merge
                                    event_found_in_base = emevd_to_merge.Events[e];
                                    break;
                                }

                            }
                        }


                    }//END foreach events
                }
                catch (Exception)
                {
                    mainLog.AddSubLog($"Error during merging {files[f].Path}\n", LOGTYPE.ERROR);
                }

            }

            mainLog.AddSubLog($"Merging of {files[0].ModRelativePath} complete!", LOGTYPE.SUCCESS);


            try
            {
                Console.WriteLine();
                mainLog.AddSubLog("Saving modded event file to: " + ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\" + files[0].ModRelativePath);
                //write the file in the merged mods directory
                base_emevd.Write(ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\" + files[0].ModRelativePath);
            }
            catch (Exception)
            {
                mainLog.AddSubLog($"Error during saving modded event in {ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\" + files[0].ModRelativePath}\n",
                    LOGTYPE.ERROR);
            }

        }
    }
}
