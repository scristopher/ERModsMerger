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
                data = BHD5Reader.Read(searchVanillaRelativePath).ToArray();
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

            FlverData = FLVER2.Read(processedData);
            // Preserve the original compression type for writing
            FlverData.Compression = compressionType;
        }

        // Clean API methods for easy access to model data
        
        /// <summary>
        /// Gets all materials in the model
        /// </summary>
        public List<FLVER2.Material> Materials => FlverData.Materials;
        
        /// <summary>
        /// Gets all meshes in the model
        /// </summary>
        public List<FLVER2.Mesh> Meshes => FlverData.Meshes;
        
        /// <summary>
        /// Gets the total number of vertices across all meshes
        /// </summary>
        public int TotalVertexCount => FlverData.Meshes.Sum(m => m.Vertices.Count);
        
        /// <summary>
        /// Gets all vertices from all meshes as a flat list
        /// </summary>
        public List<FLVER.Vertex> GetAllVertices()
        {
            var allVertices = new List<FLVER.Vertex>();
            foreach (var mesh in FlverData.Meshes)
            {
                allVertices.AddRange(mesh.Vertices);
            }
            return allVertices;
        }
        
        /// <summary>
        /// Gets vertices for a specific mesh by index
        /// </summary>
        public List<FLVER.Vertex> GetVerticesForMesh(int meshIndex)
        {
            if (meshIndex < 0 || meshIndex >= FlverData.Meshes.Count)
                throw new ArgumentOutOfRangeException(nameof(meshIndex));
            return FlverData.Meshes[meshIndex].Vertices;
        }

        public void Save(string path)
        {
            // Ensure output directory exists
            if (!TextureManager.EnsureDirectoryExists(path, LOG.Log("Directory Creation")))
            {
                throw new DirectoryNotFoundException($"Could not create directory for {path}");
            }
            
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
            // More sophisticated mesh merging strategy
            if (hasVanillaReference && vanillaFLVER != null)
            {
                // Check if mod has different mesh count or structure than vanilla
                if (modFLVER.Meshes.Count != vanillaFLVER.Meshes.Count)
                {
                    baseFLVER.Meshes = new List<FLVER2.Mesh>(modFLVER.Meshes);
                    int totalVertices = modFLVER.Meshes.Sum(m => m.Vertices.Count);
                    log.AddSubLog($"Replaced all meshes and vertices (count changed: {modFLVER.Meshes.Count} meshes, {totalVertices} vertices)", LOGTYPE.INFO);
                }
                else
                {
                    // Check if individual meshes have changed
                    bool meshesChanged = false;
                    for (int i = 0; i < modFLVER.Meshes.Count && i < vanillaFLVER.Meshes.Count; i++)
                    {
                        if (modFLVER.Meshes[i].MaterialIndex != vanillaFLVER.Meshes[i].MaterialIndex ||
                            modFLVER.Meshes[i].Vertices.Count != vanillaFLVER.Meshes[i].Vertices.Count)
                        {
                            meshesChanged = true;
                            break;
                        }
                    }
                    
                    if (meshesChanged)
                    {
                        baseFLVER.Meshes = new List<FLVER2.Mesh>(modFLVER.Meshes);
                        log.AddSubLog($"Replaced meshes due to structural changes", LOGTYPE.INFO);
                    }
                }
            }
            else
            {
                // No vanilla reference - use mod meshes
                baseFLVER.Meshes = new List<FLVER2.Mesh>(modFLVER.Meshes);
                int totalVertices = modFLVER.Meshes.Sum(m => m.Vertices.Count);
                log.AddSubLog($"Merged meshes and vertices (no vanilla reference): {modFLVER.Meshes.Count} meshes, {totalVertices} vertices", LOGTYPE.INFO);
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
            // Comprehensive material comparison
            if (mat1.Name != mat2.Name) return false;
            if (mat1.MTD != mat2.MTD) return false;
            if (mat1.Textures.Count != mat2.Textures.Count) return false;
            
            for (int i = 0; i < mat1.Textures.Count; i++)
            {
                if (mat1.Textures[i].Path != mat2.Textures[i].Path) return false;
                if (mat1.Textures[i].Type != mat2.Textures[i].Type) return false;
            }
            
            return true;
        }
    }
}