using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ERModsMerger.Core.Utility;

namespace ERModsMerger.Core.Formats
{
    public static class TextureManager
    {
        /// <summary>
        /// Safely ensure directory exists for a file path, with proper error handling
        /// </summary>
        /// <param name="filePath">Full path to the file</param>
        /// <param name="log">Logging instance</param>
        /// <returns>True if directory was created or already exists, false otherwise</returns>
        public static bool EnsureDirectoryExists(string filePath, LOG log)
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(filePath);
                if (string.IsNullOrEmpty(directoryPath))
                    return false;

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    log.AddSubLog($"Created directory: {directoryPath}", LOGTYPE.INFO);
                }
                return true;
            }
            catch (Exception e)
            {
                log.AddSubLog($"Failed to create directory for {filePath}: {e.Message}", LOGTYPE.ERROR);
                return false;
            }
        }

        /// <summary>
        /// Copy texture files from mod directories to merged output, handling conflicts
        /// </summary>
        /// <param name="modPaths">List of mod directory paths in merge order</param>
        /// <param name="outputPath">Target merged directory path</param>
        /// <param name="log">Logging instance</param>
        public static void CopyTextureFiles(List<string> modPaths, string outputPath, LOG log)
        {
            var textureFiles = new Dictionary<string, string>(); // relative path -> source full path
            var conflicts = new List<string>();

            foreach (var modPath in modPaths)
            {
                if (!Directory.Exists(modPath))
                    continue;

                var modTextureFiles = FindTextureFiles(modPath);
                
                foreach (var textureFile in modTextureFiles)
                {
                    var relativePath = Path.GetRelativePath(modPath, textureFile);
                    
                    if (textureFiles.ContainsKey(relativePath))
                    {
                        conflicts.Add(relativePath);
                        log.AddSubLog($"Texture conflict detected: {relativePath} - using version from {modPath}", LOGTYPE.WARNING);
                    }
                    
                    // Last mod wins (higher priority)
                    textureFiles[relativePath] = textureFile;
                }
            }

            // Copy all texture files to output
            foreach (var kvp in textureFiles)
            {
                try
                {
                    var targetPath = Path.Combine(outputPath, kvp.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    
                    File.Copy(kvp.Value, targetPath, true);
                    log.AddSubLog($"Copied texture: {kvp.Key}", LOGTYPE.SUCCESS);
                }
                catch (Exception e)
                {
                    log.AddSubLog($"Failed to copy texture {kvp.Key}: {e.Message}", LOGTYPE.ERROR);
                }
            }

            log.AddSubLog($"Texture copying completed - {textureFiles.Count} files copied, {conflicts.Count} conflicts resolved", LOGTYPE.INFO);
        }

        /// <summary>
        /// Find all texture files in a directory recursively
        /// </summary>
        /// <param name="directory">Directory to search</param>
        /// <returns>List of texture file paths</returns>
        private static List<string> FindTextureFiles(string directory)
        {
            var textureFiles = new List<string>();
            var textureExtensions = new[] { ".dds", ".tga", ".tpf", ".tpf.dcx" };

            try
            {
                var allFiles = new List<string>();
                Utils.FindAllFiles(directory, ref allFiles, true);

                textureFiles.AddRange(allFiles.Where(file => 
                    textureExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))));
            }
            catch (Exception)
            {
                // Ignore errors when scanning directories
            }

            return textureFiles;
        }

        /// <summary>
        /// Validate texture paths in FLVER materials and log any missing files
        /// </summary>
        /// <param name="flverPath">Path to the FLVER file</param>
        /// <param name="materials">List of materials to validate</param>
        /// <param name="log">Logging instance</param>
        public static void ValidateTexturePaths(string flverPath, List<SoulsFormats.FLVER2.Material> materials, LOG log)
        {
            var flverDirectory = Path.GetDirectoryName(flverPath);
            var missingTextures = new List<string>();

            foreach (var material in materials)
            {
                foreach (var texture in material.Textures)
                {
                    if (string.IsNullOrEmpty(texture.Path))
                        continue;

                    // Try to resolve texture path relative to FLVER location
                    var texturePath = Path.Combine(flverDirectory, texture.Path);
                    
                    if (!File.Exists(texturePath))
                    {
                        missingTextures.Add(texture.Path);
                    }
                }
            }

            if (missingTextures.Any())
            {
                log.AddSubLog($"Warning: {missingTextures.Count} texture references not found for {Path.GetFileName(flverPath)}", LOGTYPE.WARNING);
                foreach (var missing in missingTextures.Take(5)) // Limit output
                {
                    log.AddSubLog($"  Missing: {missing}", LOGTYPE.INFO);
                }
                if (missingTextures.Count > 5)
                {
                    log.AddSubLog($"  ... and {missingTextures.Count - 5} more", LOGTYPE.INFO);
                }
            }
        }

        /// <summary>
        /// Resolve texture path conflicts when merging FLVER materials
        /// </summary>
        /// <param name="baseMaterial">Base material</param>
        /// <param name="modMaterial">Mod material to merge</param>
        /// <param name="vanillaMaterial">Vanilla material for reference (can be null)</param>
        /// <param name="log">Logging instance</param>
        public static void ResolveTexturePaths(SoulsFormats.FLVER2.Material baseMaterial, 
                                             SoulsFormats.FLVER2.Material modMaterial, 
                                             SoulsFormats.FLVER2.Material vanillaMaterial,
                                             LOG log)
        {
            // Simple strategy: if mod material has different texture paths than vanilla, use mod paths
            for (int i = 0; i < Math.Min(baseMaterial.Textures.Count, modMaterial.Textures.Count); i++)
            {
                var baseTexture = baseMaterial.Textures[i];
                var modTexture = modMaterial.Textures[i];
                
                if (vanillaMaterial != null && i < vanillaMaterial.Textures.Count)
                {
                    var vanillaTexture = vanillaMaterial.Textures[i];
                    
                    // If mod texture path differs from vanilla, use the mod path
                    if (modTexture.Path != vanillaTexture.Path)
                    {
                        baseTexture.Path = modTexture.Path;
                        log.AddSubLog($"Updated texture path: {modTexture.Path}", LOGTYPE.INFO);
                    }
                }
                else
                {
                    // No vanilla reference, use mod texture path
                    baseTexture.Path = modTexture.Path;
                }
            }
        }
    }
}