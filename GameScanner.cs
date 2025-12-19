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
            @"C:\Program Files (x86)\Steam\steamapps\common",
            @"C:\Program Files\Steam\steamapps\common",
            @"C:\Program Files\Epic Games",
            @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\games",
            @"C:\Games",
            @"D:\Games",
            @"E:\Games",
            @"C:\Program Files (x86)", // Fallback scan
            @"C:\Program Files"        // Fallback scan
        };
        private readonly string[] _skipFolders = 
        { 
            "Windows", "Common Files", "Microsoft", "WindowsApps", "Reference Assemblies",
            "MSBuild", "Git", "NodeJS", "PowerShell", "Packages", "DriverStore", "Temp",
            "Uninstall Information", "System32", "SysWOW64", "AMD", "Intel", "NVIDIA Corporation",
            "Realtek", "Bonjour", "Adobe", "Dropbox", "OneDrive", "Docker", "bin", "obj", 
            "Autodesk", "Common", "DirectX", "Vulkan", "Microsoft.NET", "Internet Explorer", 
            "Windows Defender", "Windows Mail", "Windows NT", "Windows Photo Viewer", "Windows Sidebar",
            "Windows Portable Devices", "Windows PowerShell", "Microsoft Office"
        };

        private readonly string[] _skipFiles = 
        {
            "unins000", "uninstall", "helper", "crash", "setup", "update", "mDNSResponder",
            "ddpe", "dotnet", "apphost", "singlefilehost", "xmlwf", "wish", "tcl", "python",
            "protoc", "conhost", "cmd", "powershell", "vc_redist", "node", "npm", "git", "gpg",
            "DXSETUP", "vcredist", "UnityCrashHandler", "BsSndRpt", "Launcher", "Config", 
            "Report", "Bug", "Redist", "Framework", "Service", "Agent"
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

        private void ScanDirectory(string path, List<GameItem> results, int maxResults, int depth = 0)
        {
            if (results.Count >= maxResults || depth > 4) return;

            try
            {
                // 1. Get .exe files
                var files = Directory.GetFiles(path, "*.exe");
                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    // EXTENSIVE FILTERING for "Real Games"
                    if (_skipFiles.Any(s => fileName.Contains(s, StringComparison.OrdinalIgnoreCase))) continue;
                    
                    // Skip if path contains typical system folders even if not in root
                    if (path.Contains("Windows", StringComparison.OrdinalIgnoreCase) || 
                        path.Contains("System32", StringComparison.OrdinalIgnoreCase) ||
                        path.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)) continue;

                    // Requirements check: must not be a tiny file (games are usually > 1MB, utilities are small)
                    var info = new FileInfo(file);
                    if (info.Length < 1024 * 1024 && !path.Contains("Steam", StringComparison.OrdinalIgnoreCase)) continue;

                    results.Add(new GameItem { Name = fileName, Path = file });
                    if (results.Count >= maxResults) return;
                }

                // 2. Subdirectories
                var subDirs = Directory.GetDirectories(path);
                foreach (var dir in subDirs)
                {
                    string dirName = Path.GetFileName(dir);
                    if (_skipFolders.Any(s => dirName.Equals(s, StringComparison.OrdinalIgnoreCase))) continue;

                    ScanDirectory(dir, results, maxResults, depth + 1);
                    if (results.Count >= maxResults) return;
                }
            }
            catch { }
        }
    }
}
