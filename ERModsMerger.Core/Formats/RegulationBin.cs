using ERModsMerger.Core.Utility;
using SoulsFormats;
using System;
using System.Collections;
using System.IO;
using static SoulsFormats.PARAM;

namespace ERModsMerger.Core.Formats
{
    class RegulationBin : IDisposable
    {
        BND4 bnd;
        Dictionary<string, PARAMDEF> _paramdefs;
        public Dictionary<string, PARAM> Params { get; set; }

        public string Version { get; set; }

        public RegulationBin(string path)
        {
            LoadParamDefs();
            Params = new Dictionary<string, PARAM>();
            bnd = SFUtil.DecryptERRegulation(path);
            Load();
        }

        private void Load()
        {
            Version = Utils.ParseParamVersion(Convert.ToUInt64(bnd.Version)); // should be 11220021

            RegLog.AddSubLog("Regulation version: " + Version);

            var log = RegLog.AddSubLog($"Progess: 0%");
            for (int i = 0; i < bnd.Files.Count; i++)
            {
                double progress = i / (double)bnd.Files.Count * 100;
                log.Message = $"Progess: {Math.Round(progress, 0)}%";
                log.Progress = progress;

                var paramName = Path.GetFileNameWithoutExtension(bnd.Files[i].Name);

                if (!bnd.Files[i].Name.ToUpper().EndsWith(".PARAM"))
                {
                    continue;
                }

                PARAM p;

                p = PARAM.ReadIgnoreCompression(bnd.Files[i].Bytes);



                if (!_paramdefs.ContainsKey(p.ParamType ?? ""))
                {
                    continue;
                }

                if (p.ParamType == null)
                {
                    throw new Exception("Param type is unexpectedly null");
                }

                PARAMDEF def = _paramdefs[p.ParamType];
                try
                {
                    p.ApplyParamdef(def);
                }
                catch (Exception e)
                {
                    var name = bnd.Files[i].Name.Split("\\").Last();
                    var message = $"Could not apply ParamDef for {name}";
                }

                Params.Add(paramName, p);
            }

            log.Message = $"Progess: 100% - Loaded ✓";
            log.Type = LOGTYPE.SUCCESS;
            log.Progress = 100;
        }


        public List<ParamRowToMerge> FindRowsToMerge(Dictionary<string, PARAM> vanillaParams)
        {
            List<ParamRowToMerge> rowsToMerge = new List<ParamRowToMerge>();

            var log = RegLog.AddSubLog("Gathering rows to merge - Progress: 0%");
            double counter = 0;
            double max = Params.Count;
            foreach (var param in Params)
            {
                double progress = counter / max * 100;
                log.Message = $"Gathering rows to merge - Progess: {Math.Round(progress, 0)}%";
                log.Progress = progress;
                counter++;

                int moddedRowIndex = 0;
                int vanillaRowIndex = 0;

                var moddedRow = param.Value.Rows[moddedRowIndex];
                var vanillaRow = vanillaParams[param.Key].Rows[vanillaRowIndex];

                while ( moddedRowIndex < param.Value.Rows.Count )
                {
                    try
                    {
                        moddedRow = param.Value.Rows[moddedRowIndex];

                        if(vanillaRowIndex < vanillaParams[param.Key].Rows.Count)
                            vanillaRow = vanillaParams[param.Key].Rows[vanillaRowIndex];

                        ParamRowToMerge? potentialToMergeRow = null;

                        //new row
                        if (moddedRow.ID != vanillaRow.ID)
                        {
                            potentialToMergeRow = new ParamRowToMerge(param.Key, moddedRow.ID, vanillaRowIndex, moddedRow.Name, true);
                            potentialToMergeRow.Row = moddedRow;

                            for (int i = 0; i < moddedRow.Cells.Count; i++)
                                potentialToMergeRow.Cells.Add(i, moddedRow.Cells[i].Value);
                        }
                        //Same row id
                        else
                        {

                            for (int i = 0; i < moddedRow.Cells.Count; i++)
                            {
                                //field modified
                                if (!Utils.AdvancedEquals(moddedRow.Cells[i].Value, vanillaRow.Cells[i].Value))
                                {
                                    if (potentialToMergeRow == null)
                                        potentialToMergeRow = new ParamRowToMerge(param.Key, moddedRow.ID, vanillaRowIndex, moddedRow.Name, false);

                                    potentialToMergeRow.Cells.Add(i, moddedRow.Cells[i].Value);
                                }
                            }

                            vanillaRowIndex++;
                        }


                        if (potentialToMergeRow != null)
                            rowsToMerge.Add(potentialToMergeRow);

                        moddedRowIndex++;
                    }
                    catch (Exception e)
                    {

                        throw;
                    }
                    
                }
            }

            log.Message = $"Gathering rows to merge - Progess: 100% ✓";
            log.Type = LOGTYPE.SUCCESS;
            log.Progress = 100;

            return rowsToMerge;
        }

        public void ApplyModifiedRows(List<ParamRowToMerge> rows)
        {
            var log = RegLog.AddSubLog("Merging modified rows - Progress: 0%");
            double counter = 0;
            double max = rows.Count;

            List<string> modifiedParams = new List<string>();
            foreach (ParamRowToMerge row in rows)
            {
                double progress = counter / max * 100;
                log.Message = $"Merging modified rows - Progess: {Math.Round(progress, 0)}%";
                log.Progress = progress;
                counter++;
                try
                {
                    if (row.NewRow) // add new row
                    {
                        var indexExist = Params[row.ParamKey].Rows.FindIndex(x=>x.ID == row.RowID);
                        if (indexExist != -1) //another mod already use that ID
                        {
                            Params[row.ParamKey].Rows[indexExist] = row.Row; // for now we replace and overwrite existing new modded row
                        }
                        else // ID is available, row can be inserted
                        {
                            // find insertable row index
                            int index = Params[row.ParamKey].Rows.FindIndex(x => x.ID > row.RowID);

                            if (index != -1)
                                Params[row.ParamKey].Rows.Insert(index, row.Row);
                        }

                    }
                    else // modifying existing row
                    {
                        int rowIndex = 0;
                        if (Params[row.ParamKey].Rows[row.RowIndex].ID == row.RowID)
                            rowIndex = row.RowIndex;
                        else
                            rowIndex = Params[row.ParamKey].Rows.FindIndex(x=>x.ID == row.RowID); //find the row index based on ID in case of an other mod add new row(s)

                        foreach (var cell in row.Cells)
                            Params[row.ParamKey].Rows[rowIndex].Cells[cell.Key].Value = cell.Value;
                    }

                    if(!modifiedParams.Contains(row.ParamKey))
                        modifiedParams.Add(row.ParamKey);

                }
                catch (Exception e)
                {

                    throw;
                }
            }

            //save in bnd
            foreach (var key in modifiedParams)
            {
                int bndFileIndex = bnd.Files.FindIndex(x => x.Name.Contains(key + ".param"));
                if (bndFileIndex != -1)
                {
                    bnd.Files[bndFileIndex].Bytes = Params[key].Write();
                }
                else
                {
                    LOG.Log("BND file not found", LOGTYPE.ERROR);
                }
            }

            log.Message = $"Merging modified rows - Progess: 100% ✓";
            log.Type = LOGTYPE.SUCCESS;
            log.Progress = 100;

        }

        internal class ParamRowToMerge
        {
            public string ParamKey { get; set; }
            public int RowID { get; set; }
            public int RowIndex { get; set; }
            public string Name { get; set; }
            public Dictionary<int, object> Cells { get; set; }

            //for new rows
            public bool NewRow { get; set; }
            public PARAM.Row? Row { get; set; }

            public ParamRowToMerge(string paramKey, int rowID, int rowIndex, string name, bool newRow, PARAM.Row? row = null)
            {
                ParamKey = paramKey;
                RowID = rowID;
                RowIndex = rowIndex;
                Name = name;
                NewRow = newRow;
                Row = row;
                Cells = new Dictionary<int, object>();
            }
        }

        static LOG RegLog;
        public static void MergeRegulationsV2(List<FileToMerge> regulationBinFiles, bool manualConflictResolving)
        {
            var mainLog = LOG.Log("Merging regulations");
            Console.WriteLine();
            RegLog = mainLog.AddSubLog("Loading vanilla regulation.bin");

            if (!File.Exists(ModsMergerConfig.LoadedConfig.GamePath + "\\regulation.bin"))
            {
                RegLog.AddSubLog($"Could not locate vanilla regulation bin at {ModsMergerConfig.LoadedConfig.GamePath}⚠  Please verify GamePath in ERModsMergerConfig\\config.json", LOGTYPE.ERROR);
                return;
            }
            
            //load vanilla regulation.bin
            RegulationBin vanillaRegulationBin;
            try
            {
                vanillaRegulationBin = new RegulationBin(ModsMergerConfig.LoadedConfig.GamePath + "\\regulation.bin");
            }
            catch (Exception e)
            {
                RegLog.AddSubLog($"Could not load vanilla regulation.bin⚠  Your game regulation version might be incompatible", LOGTYPE.ERROR);
                return;
            }
            Console.WriteLine();
            //reload again vanilla as main modded regulation.bin (reload because can't find a way to clone bnd object)
            RegLog = mainLog.AddSubLog($"Loading initial modded regulation");
            RegulationBin mainRegulationBin;
            mainRegulationBin = new RegulationBin(ModsMergerConfig.LoadedConfig.GamePath + "\\regulation.bin");

            Console.WriteLine();

            for (int i = 0; i < regulationBinFiles.Count; i++)
            {
                if (File.Exists(regulationBinFiles[i].Path))
                {
                    //load modded regulation.bin
                    string relativePathLog = regulationBinFiles[i].Path.Replace("\\"+regulationBinFiles[i].ModRelativePath, "").Split("\\").Last() + " : " +  regulationBinFiles[i].ModRelativePath;
                    RegLog = mainLog.AddSubLog($"Loading {relativePathLog}");

                    try
                    {
                        RegulationBin moddedRegulationBin = new RegulationBin(regulationBinFiles[i].Path);

                        if (moddedRegulationBin.Version != vanillaRegulationBin.Version)
                            RegLog.AddSubLog("Regulation version doesn't match - If you encounter any issue, please update this mod", LOGTYPE.WARNING);

                        var rows = moddedRegulationBin.FindRowsToMerge(vanillaRegulationBin.Params);
                        mainRegulationBin.ApplyModifiedRows(rows);
                    }
                    catch (Exception e)
                    {
                        RegLog.AddSubLog($"Could not merge {regulationBinFiles[i].Path} ⚠  Regulation version might be incompatible", LOGTYPE.ERROR);
                    }
                }
                Console.WriteLine();
            }

            RegLog = mainLog.AddSubLog("Saving merged regulation.bin");
            mainRegulationBin.Save(ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\regulation.bin");
            RegLog.AddSubLog("Saved in: " + ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\regulation.bin", LOGTYPE.SUCCESS);
        }

        public void MergeFrom(Dictionary<string, PARAM> fromParams, Dictionary<string, PARAM> vanillaParams, bool manualConflictResolving = false)
        {
            int counter = 0;
            int maxCounter = fromParams.Count;
            foreach (var fromParam in fromParams)
            {
                string mergingProgressConsole = $"\r🛈  Merging regulation.bin - Progress {Math.Round(counter / (double)maxCounter * 100, 0)}%";

                Console.Write(mergingProgressConsole);

                bool modifiedParam = false;

                for (int r = 0; r < fromParam.Value.Rows.Count; r++)
                {
                    int rowId = fromParam.Value.Rows[r].ID;
                    int rowIndexFound = Params[fromParam.Key].Rows.FindIndex(x => x.ID == rowId);

                    // if row to merge already exist AND have not ID duplicates
                    if (rowIndexFound != -1 && Params[fromParam.Key].Rows.Count(x => x.ID == rowId) == 1)
                    {
                        try
                        {
                            for (int c = 0; c < fromParam.Value.Rows[r].Cells.Count; c++)
                            {
                                if (Params[fromParam.Key].Rows[rowIndexFound].Cells[c] != null && fromParam.Value.Rows[r].Cells[c] != null)
                                {
                                    var valCurrent = Params[fromParam.Key].Rows[rowIndexFound].Cells[c].Value;
                                    var valFromParam = fromParam.Value.Rows[r].Cells[c].Value;


                                    // verify if row exist in vanilla
                                    if (vanillaParams[fromParam.Key].Rows.Count > rowIndexFound)
                                    {
                                        var valFromVanilla = vanillaParams[fromParam.Key].Rows[rowIndexFound].Cells[c].Value;


                                        //modded param->row->field is different from vanilla
                                        if (!Utils.AdvancedEquals(valFromVanilla, valFromParam))
                                        {
                                            //detect an attempt to re-edit a value already edited by another mod
                                            //manual resolving
                                            if (manualConflictResolving && !Utils.AdvancedEquals(valCurrent, valFromVanilla) && !Utils.AdvancedEquals(valCurrent, valFromParam))
                                            {
                                                Console.WriteLine();
                                                LOG.Log($"- Detected conflict in {fromParam.Key}->[{rowIndexFound.ToString()}] {Params[fromParam.Key].Rows[rowIndexFound].Name}->{Params[fromParam.Key].Rows[rowIndexFound].Cells[c].Def.ToString()}\n" +
                                                        $"   From value: {valCurrent.ToString()}\n" +
                                                        $"   To value: {valFromParam.ToString()}\n\n",
                                                        LOGTYPE.WARNING);


                                                if (LOG.QueryUserYesNoQuestion("Apply new value?"))
                                                {
                                                    Params[fromParam.Key].Rows[rowIndexFound].Cells[c].Value = valFromParam;
                                                    modifiedParam = true;
                                                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                                                    Console.Write("\rNew value applied!                                                         \n\n");
                                                    Console.ResetColor();
                                                }
                                                else
                                                {
                                                    Console.ForegroundColor = ConsoleColor.DarkRed;
                                                    Console.Write("\rNew value ignored!                                                         \n\n");
                                                    Console.ResetColor();
                                                }

                                            }
                                            else
                                            {
                                                Params[fromParam.Key].Rows[rowIndexFound].Cells[c].Value = valFromParam;
                                                modifiedParam = true;
                                            }

                                        }
                                    }
                                    else
                                    {
                                        Params[fromParam.Key].Rows[rowIndexFound].Cells[c].Value = valFromParam;
                                        modifiedParam = true;
                                    }

                                }

                            }

                        }
                        catch (Exception e)
                        {
                            LOG.Log($"Error during merging Row {r.ToString()} in Param {fromParam.Key}\n", LOGTYPE.ERROR);
                        }


                    }
                    else if (rowIndexFound == -1) // if the row to merge is a new one
                    {
                        Params[fromParam.Key].Rows.Add(new PARAM.Row(fromParam.Value.Rows[r]));
                        modifiedParam = true;
                    }

                }


                if (modifiedParam)
                {
                    int bndFileIndex = bnd.Files.FindIndex(x => x.Name.Contains(fromParam.Key + ".param"));
                    try
                    {
                        if (bndFileIndex != -1)
                        {
                            bnd.Files[bndFileIndex].Bytes = Params[fromParam.Key].Write();
                        }
                        else
                        {
                            LOG.Log("BND file not found", LOGTYPE.ERROR);
                        }
                    }
                    catch (Exception e)
                    {
                        LOG.Log($"Unable to merge Param {fromParam.Key}\n", LOGTYPE.ERROR);
                    }
                }
                counter++;
            }

            LOG.Log($"Merging regulation.bin - Progess: 100% - Done ✓\n\n");
        }

        public void Save(string path)
        {
            SFUtil.EncryptERRegulation(path, bnd);
        }


        public void Dispose()
        {
            bnd.Dispose();
            Params.Clear();
            _paramdefs.Clear();

            // Notify the garbage collector 
            // about the cleaning event 
            GC.SuppressFinalize(this);
        }

        private void LoadParamDefs()
        {
            _paramdefs = new Dictionary<string, PARAMDEF>();
            //Load paramdefs
            var files = Directory.GetFiles(ModsMergerConfig.LoadedConfig.AppDataFolderPath +"\\ParamDefs", "*.xml");

            foreach (var f in files)
            {
                var pdef = PARAMDEF.XmlDeserialize(f, false);

                _paramdefs.Add(pdef.ParamType, pdef);
            }

        }

        public static void MergeRegulationsV1(List<FileToMerge> regulationBinFiles, bool manualConflictResolving)
        {
            LOG.Log("Loading vanilla regulation.bin");

            if (!File.Exists(ModsMergerConfig.LoadedConfig.GamePath + "\\regulation.bin"))
            {
                LOG.Log($"Could not locate vanilla regulation bin at {ModsMergerConfig.LoadedConfig.GamePath}\n⚠  Please verify GamePath in ERModsMergerConfig\\config.json", LOGTYPE.ERROR);
                return;
            }

            //load vanilla regulation.bin
            RegulationBin vanillaRegulationBin;
            try
            {
                vanillaRegulationBin = new RegulationBin(ModsMergerConfig.LoadedConfig.GamePath + "\\regulation.bin");
            }
            catch (Exception e)
            {
                LOG.Log($"Could not load vanilla regulation.bin\n⚠  Your game regulation version might be incompatible", LOGTYPE.ERROR);
                return;
            }

            //load modded regulation.bin
            LOG.Log($"Loading initial modded regulation: {regulationBinFiles[0].Path}");

            RegulationBin mainRegulationBin;
            try
            {
                mainRegulationBin = new RegulationBin(regulationBinFiles[0].Path);

                if (mainRegulationBin.Version != vanillaRegulationBin.Version)
                    LOG.Log("Regulation version doesn't match - If you encounter any issue, please update this mod\n", LOGTYPE.WARNING);
            }
            catch (Exception e)
            {
                LOG.Log($"Could not load {regulationBinFiles[0].Path}\n⚠  Regulation version might be incompatible", LOGTYPE.ERROR);
                return;
            }

            Console.WriteLine();

            for (int i = 1; i < regulationBinFiles.Count; i++)
            {
                if (File.Exists(regulationBinFiles[i].Path))
                {
                    //load modded regulation.bin
                    LOG.Log($"Loading {regulationBinFiles[i].Path}");

                    try
                    {
                        RegulationBin moddedRegulationBin = new RegulationBin(regulationBinFiles[i].Path);

                        if (moddedRegulationBin.Version != vanillaRegulationBin.Version)
                            LOG.Log("Regulation version doesn't match - If you encounter any issue, please update this mod\n", LOGTYPE.WARNING);

                        LOG.Log($"Merging ...");
                        mainRegulationBin.MergeFrom(moddedRegulationBin.Params, vanillaRegulationBin.Params, manualConflictResolving);
                    }
                    catch (Exception e)
                    {
                        LOG.Log($"Could not load {regulationBinFiles[i].Path}\n⚠  Regulation version might be incompatible", LOGTYPE.ERROR);
                    }
                }
            }

            LOG.Log("Saving merged regulation.bin");
            mainRegulationBin.Save(ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\regulation.bin");
            LOG.Log("Saved in: " + ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\regulation.bin\n");
        }

    }

}
