using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ERModsMerger.Core.Utility;
using ERModsMerger.Core.BHD5Handler;

namespace ERModsMerger.Core.Formats
{
    internal class MTD_DCX
    {
        public MTD MaterialData { get; set; }
        
        public MTD_DCX(string path, string searchVanillaRelativePath = "")
        {
            byte[] data;
            
            if (searchVanillaRelativePath != "")
                data = BHD5Reader.Read(searchVanillaRelativePath);
            else
                data = File.ReadAllBytes(path);

            // Handle DCX decompression if needed and preserve compression type
            Memory<byte> processedData;
            DCX.Type compressionType = DCX.Type.None;
            
            if (DCX.Is(data))
            {
                processedData = DCX.Decompress(data, out compressionType);
            }
            else
            {
                processedData = new Memory<byte>(data);
            }

            MaterialData = MTD.Read(processedData);
            // Preserve the original compression type for writing
            MaterialData.Compression = compressionType;
        }

        // Clean API methods for easy access to material data
        
        /// <summary>
        /// Gets the shader path for this material
        /// </summary>
        public string ShaderPath => MaterialData.ShaderPath;
        
        /// <summary>
        /// Gets the description for this material
        /// </summary>
        public string Description => MaterialData.Description;
        
        /// <summary>
        /// Gets all material parameters
        /// </summary>
        public List<MTD.Param> Parameters => MaterialData.Params;
        
        /// <summary>
        /// Gets all texture definitions
        /// </summary>
        public List<MTD.Texture> Textures => MaterialData.Textures;
        
        /// <summary>
        /// Gets a parameter by name, or null if not found
        /// </summary>
        public MTD.Param GetParameter(string name)
        {
            return MaterialData.Params.FirstOrDefault(p => p.Name == name);
        }
        
        /// <summary>
        /// Gets a texture definition by type, or null if not found
        /// </summary>
        public MTD.Texture GetTexture(string type)
        {
            return MaterialData.Textures.FirstOrDefault(t => t.Type == type);
        }
        
        /// <summary>
        /// Sets or adds a parameter with the specified name and value
        /// </summary>
        public void SetParameter(string name, MTD.Param.ParamType type, object value)
        {
            var existingParam = GetParameter(name);
            if (existingParam != null)
            {
                existingParam.Type = type;
                existingParam.Value = value;
            }
            else
            {
                MaterialData.Params.Add(new MTD.Param { Name = name, Type = type, Value = value });
            }
        }

        public void Save(string path)
        {
            // Ensure output directory exists
            if (!TextureManager.EnsureDirectoryExists(path, LOG.Log("Directory Creation")))
            {
                throw new DirectoryNotFoundException($"Could not create directory for {path}");
            }
            
            byte[] mtdBytes = MaterialData.Write();
            File.WriteAllBytes(path, mtdBytes);
        }

        public static void MergeFiles(List<FileToMerge> files)
        {
            Console.Write("\n");
            string status = "";

            var mainLog = LOG.Log("Merging Material Definitions (MTD)");

            MTD? vanilla_mtd = null;
            bool hasVanillaReference = false;
            try
            {
                vanilla_mtd = new MTD_DCX("", files[0].ModRelativePath).MaterialData;
                hasVanillaReference = true;
                mainLog.AddSubLog($"Vanilla {files[0].ModRelativePath} - Loaded ✓\n", LOGTYPE.SUCCESS);
            }
            catch (Exception e)
            {
                mainLog.AddSubLog($"Could not load vanilla MTD file - merging without vanilla reference\n", LOGTYPE.WARNING);
                mainLog.AddSubLog($"Vanilla loading error details: {e.GetType().Name}: {e.Message}\n", LOGTYPE.INFO);
                hasVanillaReference = false;
            }

            MTD_DCX? base_mtd_wrapper = null;
            MTD? base_mtd = null;
            try
            {
                base_mtd_wrapper = new MTD_DCX(files[0].Path);
                base_mtd = base_mtd_wrapper.MaterialData;
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
                MTD? mtd_to_merge = null;
                try
                {
                    mtd_to_merge = new MTD_DCX(files[f].Path).MaterialData;
                    mainLog.AddSubLog($"Modded {files[f].ModRelativePath} - Loaded ✓", LOGTYPE.SUCCESS);
                }
                catch (Exception e)
                {
                    mainLog.AddSubLog($"Could not load {files[f].Path}", LOGTYPE.ERROR);
                    mainLog.AddSubLog($"Mod file loading error details: {e.GetType().Name}: {e.Message}", LOGTYPE.ERROR);
                    continue; // Continue with next file
                }

                try
                {
                    status = "Merging " + files[f].ModRelativePath + " [" + (f + 1).ToString() + "/" + files.Count.ToString() + "]";
                    Console.Write($"\r{status} - Progress: 50%                                     ");
                    
                    // MTD merging strategy - merge shader path, description, params, and textures
                    MergeMTDData(base_mtd, mtd_to_merge, vanilla_mtd, hasVanillaReference, mainLog);
                    
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
                
                base_mtd_wrapper.Save(outputPath);
                mainLog.AddSubLog("Saving merged MTD file to: " + outputPath, LOGTYPE.SUCCESS);
            }
            catch (Exception e)
            {
                string outputPath = ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\" + files[0].ModRelativePath;
                mainLog.AddSubLog($"Error during saving merged MTD file to {outputPath}: {e.GetType().Name}: {e.Message}",
                    LOGTYPE.ERROR);
                if (e.InnerException != null)
                {
                    mainLog.AddSubLog($"Save inner exception: {e.InnerException.GetType().Name}: {e.InnerException.Message}", LOGTYPE.ERROR);
                }
            }
        }

        private static void MergeMTDData(MTD baseMTD, MTD modMTD, MTD? vanillaMTD, bool hasVanillaReference, LOG log)
        {
            // Merge shader path
            if (hasVanillaReference && vanillaMTD != null)
            {
                if (modMTD.ShaderPath != vanillaMTD.ShaderPath)
                {
                    baseMTD.ShaderPath = modMTD.ShaderPath;
                    log.AddSubLog($"Updated shader path: {modMTD.ShaderPath}", LOGTYPE.INFO);
                }
            }
            else
            {
                baseMTD.ShaderPath = modMTD.ShaderPath;
                log.AddSubLog($"Set shader path: {modMTD.ShaderPath}", LOGTYPE.INFO);
            }

            // Merge description
            if (hasVanillaReference && vanillaMTD != null)
            {
                if (modMTD.Description != vanillaMTD.Description)
                {
                    baseMTD.Description = modMTD.Description;
                    log.AddSubLog($"Updated description", LOGTYPE.INFO);
                }
            }
            else
            {
                baseMTD.Description = modMTD.Description;
            }

            // Merge parameters
            MergeMTDParams(baseMTD, modMTD, vanillaMTD, hasVanillaReference, log);

            // Merge textures
            MergeMTDTextures(baseMTD, modMTD, vanillaMTD, hasVanillaReference, log);
        }

        private static void MergeMTDParams(MTD baseMTD, MTD modMTD, MTD? vanillaMTD, bool hasVanillaReference, LOG log)
        {
            // Create a dictionary for efficient parameter lookup
            var baseParams = baseMTD.Params.ToDictionary(p => p.Name, p => p);
            var vanillaParams = hasVanillaReference && vanillaMTD != null ? 
                                vanillaMTD.Params.ToDictionary(p => p.Name, p => p) : 
                                new Dictionary<string, MTD.Param>();

            foreach (var modParam in modMTD.Params)
            {
                bool shouldMerge = false;

                if (hasVanillaReference && vanillaParams.ContainsKey(modParam.Name))
                {
                    var vanillaParam = vanillaParams[modParam.Name];
                    // Check if mod parameter differs from vanilla
                    if (!ParamsEqual(modParam, vanillaParam))
                    {
                        shouldMerge = true;
                    }
                }
                else if (!hasVanillaReference)
                {
                    shouldMerge = true;
                }
                else if (hasVanillaReference && !vanillaParams.ContainsKey(modParam.Name))
                {
                    // New parameter not in vanilla
                    shouldMerge = true;
                }

                if (shouldMerge)
                {
                    if (baseParams.ContainsKey(modParam.Name))
                    {
                        baseParams[modParam.Name] = modParam;
                        log.AddSubLog($"Updated parameter: {modParam.Name}", LOGTYPE.INFO);
                    }
                    else
                    {
                        baseMTD.Params.Add(modParam);
                        baseParams[modParam.Name] = modParam;
                        log.AddSubLog($"Added new parameter: {modParam.Name}", LOGTYPE.INFO);
                    }
                }
            }

            // Update the params list with merged values
            baseMTD.Params = baseParams.Values.ToList();
        }

        private static void MergeMTDTextures(MTD baseMTD, MTD modMTD, MTD? vanillaMTD, bool hasVanillaReference, LOG log)
        {
            // Create a dictionary for efficient texture lookup
            var baseTextures = baseMTD.Textures.ToDictionary(t => t.Type, t => t);
            var vanillaTextures = hasVanillaReference && vanillaMTD != null ? 
                                  vanillaMTD.Textures.ToDictionary(t => t.Type, t => t) : 
                                  new Dictionary<string, MTD.Texture>();

            foreach (var modTexture in modMTD.Textures)
            {
                bool shouldMerge = false;

                if (hasVanillaReference && vanillaTextures.ContainsKey(modTexture.Type))
                {
                    var vanillaTexture = vanillaTextures[modTexture.Type];
                    // Check if mod texture differs from vanilla
                    if (!TexturesEqual(modTexture, vanillaTexture))
                    {
                        shouldMerge = true;
                    }
                }
                else if (!hasVanillaReference)
                {
                    shouldMerge = true;
                }
                else if (hasVanillaReference && !vanillaTextures.ContainsKey(modTexture.Type))
                {
                    // New texture type not in vanilla
                    shouldMerge = true;
                }

                if (shouldMerge)
                {
                    if (baseTextures.ContainsKey(modTexture.Type))
                    {
                        baseTextures[modTexture.Type] = modTexture;
                        log.AddSubLog($"Updated texture type: {modTexture.Type}", LOGTYPE.INFO);
                    }
                    else
                    {
                        baseMTD.Textures.Add(modTexture);
                        baseTextures[modTexture.Type] = modTexture;
                        log.AddSubLog($"Added new texture type: {modTexture.Type}", LOGTYPE.INFO);
                    }
                }
            }

            // Update the textures list with merged values
            baseMTD.Textures = baseTextures.Values.ToList();
        }

        private static bool ParamsEqual(MTD.Param param1, MTD.Param param2)
        {
            if (param1.Name != param2.Name) return false;
            if (param1.Type != param2.Type) return false;
            if (param1.Value != param2.Value) return false;
            return true;
        }

        private static bool TexturesEqual(MTD.Texture tex1, MTD.Texture tex2)
        {
            if (tex1.Type != tex2.Type) return false;
            if (tex1.Extended != tex2.Extended) return false;
            if (tex1.UVNumber != tex2.UVNumber) return false;
            return true;
        }
    }
}