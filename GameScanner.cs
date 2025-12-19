using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NvidiaCi
{
    public class GameScanner
    {
        private readonly string[] _targetFolders = 
        { 
            @"C:\Program Files", 
            @"C:\Program Files (x86)" 
        };

        private readonly string[] _skipFolders = 
        { 
            "Windows", "Common Files", "Microsoft", "WindowsApps", "Reference Assemblies",
            "MSBuild", "Git", "NodeJS", "PowerShell", "Packages", "DriverStore", "Temp",
            "Uninstall Information", "System32", "SysWOW64", "AMD", "Intel", "NVIDIA Corporation",
            "Realtek", "Bonjour", "Adobe", "Dropbox", "OneDrive", "Docker", "bin", "obj", "Autodesk", "Common"
        };

        private readonly string[] _skipFiles = 
        {
            "unins000", "uninstall", "helper", "crash", "setup", "update", "mDNSResponder",
            "ddpe", "dotnet", "apphost", "singlefilehost", "xmlwf", "wish", "tcl", "python",
            "protoc", "conhost", "cmd", "powershell", "vc_redist", "node", "npm", "git", "gpg"
        };

        public List<GameItem> ScanForGames(int maxResults = 100)
        {
            var results = new List<GameItem>();

            foreach (var root in _targetFolders)
            {
                if (!Directory.Exists(root)) continue;

                try
                {
                    ScanDirectory(root, results, maxResults);
                }
                catch (Exception)
                {
                    // Root level access issues or drive not ready
                }

                if (results.Count >= maxResults) break;
            }

            return results;
        }

        private void ScanDirectory(string path, List<GameItem> results, int maxResults)
        {
            if (results.Count >= maxResults) return;

            try
            {
                // 1. Get .exe files in current directory
                var files = Directory.GetFiles(path, "*.exe");
                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    // Skip known system/utility files
                    if (_skipFiles.Any(s => fileName.Contains(s, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    results.Add(new GameItem
                    {
                        Name = fileName,
                        Path = file
                    });

                    if (results.Count >= maxResults) return;
                }

                // 2. Recurse into subdirectories
                var subDirs = Directory.GetDirectories(path);
                foreach (var dir in subDirs)
                {
                    string dirName = Path.GetFileName(dir);

                    // Skip forbidden folders
                    if (_skipFolders.Any(s => dirName.Equals(s, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    ScanDirectory(dir, results, maxResults);
                    
                    if (results.Count >= maxResults) return;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Expected for many system/protected folders
            }
            catch (Exception)
            {
                // Other I/O errors
            }
        }
    }
}
