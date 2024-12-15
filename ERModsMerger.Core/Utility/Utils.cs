using DotNext.Collections.Generic;
using Microsoft.Win32;
using Org.BouncyCastle.Math;
using SoulsFormats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERModsMerger.Core.Utility
{
    public static class Utils
    {
        public static string GetInstallPath(string applicationName)
        {
            var installPath = FindApplicationPath(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", applicationName);

            if (installPath == null)
            {
                installPath = FindApplicationPath(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", applicationName);
            }

            return installPath;
        }

        private static string FindApplicationPath(string keyPath, string applicationName)
        {

            var hklm = Registry.LocalMachine;
            var uninstall = hklm.OpenSubKey(keyPath);
            foreach (var productSubKey in uninstall.GetSubKeyNames())
            {
                var product = uninstall.OpenSubKey(productSubKey);

                var displayName = product.GetValue("DisplayName");
                if (displayName != null && displayName.ToString() == applicationName)
                {
                    return product.GetValue("InstallLocation").ToString();
                }

            }

            return null;
        }

        /// <summary>
        ///     Returns string representing version of param or regulation.
        /// </summary>
        public static string ParseParamVersion(ulong version)
        {
            string verStr = version.ToString();
            if (verStr.Length == 7 || verStr.Length == 8)
            {
                char major = verStr[0];
                string minor = verStr[1..3];
                char patch = verStr[3];
                string rev = verStr[4..];
                return $"{major}.{minor}.{patch}";
            }

            return "Unknown version format";
        }


        /// <summary>
        /// Same as normal Equals but can also check equality between Enumerables/Arrays
        /// </summary>
        /// <returns></returns>
        public static bool AdvancedEquals(object? x, object? y)
        {
            if (x is null && y is null)
                return true;

            if (x is null || y is null)
                return false;

            if (x is IEnumerable a && y is IEnumerable b)
                return a.Cast<object>().SequenceEqual(b.Cast<object>());

            return x.Equals(y);
        }

        private const uint PRIME = 37;
        private const ulong PRIME64 = 0x85ul;
        public static ulong ComputeHash(string path, BHD5.Game game)
        {
            string hashable = path.Trim().Replace('\\', '/').ToLowerInvariant();
            if (!hashable.StartsWith("/"))
            {
                hashable = '/' + hashable;
            }
            return game >= BHD5.Game.EldenRing ? hashable.Aggregate(0ul, (i, c) => i * PRIME64 + c) : hashable.Aggregate(0u, (i, c) => i * PRIME + c);
        }

        /// <summary>
        /// Returns the input string with the first character converted to uppercase
        /// </summary>
        public static string FirstLetterToUpperCase(string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentException("There is no first letter");

            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static void FindAllFiles(string path, ref List<string> files, bool searchInSubDirectories, bool returnDirNames = false)
        {
            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory)) // path is a directory / folder
            {
                if (returnDirNames)
                    files.AddAll(Directory.GetDirectories(path).ToList());

                files.AddAll(Directory.GetFiles(path).ToList());


                if (searchInSubDirectories && Directory.GetDirectories(path).Length > 0)
                {
                    foreach (var directory in Directory.GetDirectories(path))
                        FindAllFiles(directory, ref files, searchInSubDirectories, returnDirNames);
                }
            }
        }


        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = true, List<string>? ignoredFiles = null)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            if(ignoredFiles != null)
            {
                // Get the files in the source directory and copy to the destination directory
                foreach (FileInfo file in dir.GetFiles())
                {
                    if (!ignoredFiles.Contains(file.FullName))
                    {
                        string targetFilePath = Path.Combine(destinationDir, file.Name);
                        file.CopyTo(targetFilePath, true);
                    }
                }
            }
            else
            {
                // Get the files in the source directory and copy to the destination directory
                foreach (FileInfo file in dir.GetFiles())
                {

                    string targetFilePath = Path.Combine(destinationDir, file.Name);
                    file.CopyTo(targetFilePath, true);
                }
            }

           

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true, ignoredFiles);
                }
            }
        }


    }
}
