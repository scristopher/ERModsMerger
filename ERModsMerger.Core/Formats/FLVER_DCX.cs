using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ERModsMerger.Core.Utility;
using ERModsMerger.Core.BHD5Handler;

namespace ERModsMerger.Core.Formats
{
    internal class FLVER_DCX
    {
        public FLVER2 FlverData { get; set; }
        
        public FLVER_DCX(string path, string searchVanillaRelativePath = "")
        {
            byte[] data;
            
            if (searchVanillaRelativePath != "")
                data = BHD5Reader.Read(searchVanillaRelativePath);
            else
                data = File.ReadAllBytes(path);

            FlverData = FLVER2.Read(data);
        }

        public void Save(string path)
        {
            // Ensure output directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            
            byte[] flverBytes = FlverData.Write();
            File.WriteAllBytes(path, flverBytes);
        }

        public static void MergeFiles(List<FileToMerge> files)
        {
            Console.Write("\n");
            string status = "";
            string percent = "0";

            var mainLog = LOG.Log("Merging FLVER Models");

            FLVER2? vanilla_flver = null;
            bool hasVanillaReference = false;
            try
            {
                vanilla_flver = new FLVER_DCX("", files[0].ModRelativePath).FlverData;
                hasVanillaReference = true;
                mainLog.AddSubLog($"Vanilla {files[0].ModRelativePath} - Loaded ✓\n", LOGTYPE.SUCCESS);
            }
            catch (Exception e)
            {
                mainLog.AddSubLog($"Could not load vanilla FLVER file - merging without vanilla reference\n", LOGTYPE.WARNING);
                mainLog.AddSubLog($"Vanilla loading error details: {e.GetType().Name}: {e.Message}\n", LOGTYPE.INFO);
                hasVanillaReference = false;
            }

            FLVER_DCX? base_flver_wrapper = null;
            FLVER2? base_flver = null;
            try
            {
                base_flver_wrapper = new FLVER_DCX(files[0].Path);
                base_flver = base_flver_wrapper.FlverData;
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
                FLVER2? flver_to_merge = null;
                try
                {
                    flver_to_merge = new FLVER_DCX(files[f].Path).FlverData;
                    mainLog.AddSubLog($"Modded {files[f].ModRelativePath} - Loaded ✓", LOGTYPE.SUCCESS);
                    
                    // Basic validation
                    if (flver_to_merge.Materials.Count != base_flver.Materials.Count)
                    {
                        mainLog.AddSubLog($"Warning: Material count mismatch - Base: {base_flver.Materials.Count}, Mod: {flver_to_merge.Materials.Count}. Will merge with caution.", LOGTYPE.WARNING);
                    }
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
                    
                    // Material merging strategy
                    Console.Write($"\r{status} - Progress: 25% (Materials)                                     ");
                    MergeMaterials(base_flver, flver_to_merge, vanilla_flver, hasVanillaReference, mainLog);
                    
                    // Mesh merging strategy 
                    Console.Write($"\r{status} - Progress: 50% (Meshes)                                     ");
                    MergeMeshes(base_flver, flver_to_merge, vanilla_flver, hasVanillaReference, mainLog);
                    
                    // Node merging
                    Console.Write($"\r{status} - Progress: 75% (Nodes)                                     ");
                    MergeNodes(base_flver, flver_to_merge, vanilla_flver, hasVanillaReference, mainLog);
                    
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
                
                base_flver_wrapper.Save(outputPath);
                mainLog.AddSubLog("Saving merged FLVER file to: " + outputPath, LOGTYPE.SUCCESS);
                
                // Copy related texture files
                try
                {
                    var modPaths = files.Select(f => Path.GetDirectoryName(f.Path)).Where(p => !string.IsNullOrEmpty(p)).ToList();
                    var outputDir = Path.GetDirectoryName(outputPath);
                    
                    if (modPaths.Any() && !string.IsNullOrEmpty(outputDir))
                    {
                        TextureManager.CopyTextureFiles(modPaths, outputDir, mainLog);
                        
                        // Validate texture paths in the merged FLVER
                        TextureManager.ValidateTexturePaths(outputPath, base_flver.Materials, mainLog);
                    }
                }
                catch (Exception texEx)
                {
                    mainLog.AddSubLog($"Warning: Texture copying failed: {texEx.Message}", LOGTYPE.WARNING);
                }
            }
            catch (Exception e)
            {
                string outputPath = ModsMergerConfig.LoadedConfig.CurrentProfile.MergedModsFolderPath + "\\" + files[0].ModRelativePath;
                mainLog.AddSubLog($"Error during saving merged FLVER file to {outputPath}: {e.GetType().Name}: {e.Message}",
                    LOGTYPE.ERROR);
                if (e.InnerException != null)
                {
                    mainLog.AddSubLog($"Save inner exception: {e.InnerException.GetType().Name}: {e.InnerException.Message}", LOGTYPE.ERROR);
                }
            }
        }

        private static void MergeMaterials(FLVER2 baseFLVER, FLVER2 modFLVER, FLVER2? vanillaFLVER, bool hasVanillaReference, LOG log)
        {
            // Simple material merging strategy - replace materials if they differ from vanilla
            int maxMaterials = Math.Min(baseFLVER.Materials.Count, modFLVER.Materials.Count);
            
            for (int i = 0; i < maxMaterials; i++)
            {
                var baseMaterial = baseFLVER.Materials[i];
                var modMaterial = modFLVER.Materials[i];
                
                if (hasVanillaReference && i < vanillaFLVER.Materials.Count)
                {
                    var vanillaMaterial = vanillaFLVER.Materials[i];
                    
                    // Check if mod material differs from vanilla
                    if (!MaterialsEqual(modMaterial, vanillaMaterial))
                    {
                        // Replace base material with mod material
                        baseFLVER.Materials[i] = modMaterial;
                        
                        // Handle texture path conflicts
                        TextureManager.ResolveTexturePaths(baseFLVER.Materials[i], modMaterial, vanillaMaterial, log);
                        
                        log.AddSubLog($"Replaced material {i} with modded version", LOGTYPE.INFO);
                    }
                }
                else
                {
                    // No vanilla reference - merge all materials
                    baseFLVER.Materials[i] = modMaterial;
                    log.AddSubLog($"Merged material {i} (no vanilla reference)", LOGTYPE.INFO);
                }
            }
            
            // Add any extra materials from the mod
            for (int i = maxMaterials; i < modFLVER.Materials.Count; i++)
            {
                baseFLVER.Materials.Add(modFLVER.Materials[i]);
                log.AddSubLog($"Added new material {i} from mod", LOGTYPE.INFO);
            }
        }

        private static void MergeMeshes(FLVER2 baseFLVER, FLVER2 modFLVER, FLVER2? vanillaFLVER, bool hasVanillaReference, LOG log)
        {
            // Simple mesh merging strategy - for now, just replace with mod meshes
            // In the future, this could be more sophisticated to merge vertex data
            
            if (hasVanillaReference && vanillaFLVER != null)
            {
                // Check if mod has different meshes than vanilla
                if (modFLVER.Meshes.Count != vanillaFLVER.Meshes.Count)
                {
                    baseFLVER.Meshes = modFLVER.Meshes;
                    log.AddSubLog($"Replaced all meshes with modded versions (count changed)", LOGTYPE.INFO);
                }
                else
                {
                    // Could implement more detailed mesh comparison here
                    baseFLVER.Meshes = modFLVER.Meshes;
                    log.AddSubLog($"Replaced meshes with modded versions", LOGTYPE.INFO);
                }
            }
            else
            {
                // No vanilla reference - use mod meshes
                baseFLVER.Meshes = modFLVER.Meshes;
                log.AddSubLog($"Merged meshes (no vanilla reference)", LOGTYPE.INFO);
            }
        }

        private static void MergeNodes(FLVER2 baseFLVER, FLVER2 modFLVER, FLVER2? vanillaFLVER, bool hasVanillaReference, LOG log)
        {
            // Simple node merging - replace if mod has different node count or structure
            if (modFLVER.Nodes.Count != baseFLVER.Nodes.Count)
            {
                baseFLVER.Nodes = modFLVER.Nodes;
                log.AddSubLog($"Replaced nodes with modded versions (count: {modFLVER.Nodes.Count})", LOGTYPE.INFO);
            }
            else if (hasVanillaReference && vanillaFLVER != null)
            {
                // Check if mod nodes differ from vanilla
                if (modFLVER.Nodes.Count != vanillaFLVER.Nodes.Count)
                {
                    baseFLVER.Nodes = modFLVER.Nodes;
                    log.AddSubLog($"Replaced nodes with modded versions", LOGTYPE.INFO);
                }
            }
        }

        private static bool MaterialsEqual(FLVER2.Material mat1, FLVER2.Material mat2)
        {
            // Simple material comparison - could be more thorough
            if (mat1.Name != mat2.Name) return false;
            if (mat1.MTD != mat2.MTD) return false;
            if (mat1.Textures.Count != mat2.Textures.Count) return false;
            
            for (int i = 0; i < mat1.Textures.Count; i++)
            {
                if (mat1.Textures[i].Path != mat2.Textures[i].Path) return false;
            }
            
            return true;
        }
    }
}