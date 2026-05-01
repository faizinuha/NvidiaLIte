using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace NvidiaCi
{
    public class GameScanner
    {
        private readonly List<string> _targetFolders = new();
        
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

        public GameScanner()
        {
            // Detect game library paths from registry
            DetectSteamLibraries();
            DetectEpicGames();
            DetectGOGGames();
            DetectEAGames();
            
            // Fallback manual paths
            AddFallbackPaths();
        }

        private void DetectSteamLibraries()
        {
            try
            {
                // Steam install path
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                if (key != null)
                {
                    string? steamPath = key.GetValue("SteamPath") as string;
                    if (!string.IsNullOrEmpty(steamPath))
                    {
                        // Main library
                        string mainLibrary = Path.Combine(steamPath, "steamapps", "common");
                        if (Directory.Exists(mainLibrary))
                            _targetFolders.Add(mainLibrary);

                        // Additional libraries from libraryfolders.vdf
                        string vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                        if (File.Exists(vdfPath))
                        {
                            var lines = File.ReadAllLines(vdfPath);
                            foreach (var line in lines)
                            {
                                if (line.Contains("\"path\""))
                                {
                                    var match = System.Text.RegularExpressions.Regex.Match(line, "\"path\"\\s+\"(.+?)\"");
                                    if (match.Success)
                                    {
                                        string libPath = Path.Combine(match.Groups[1].Value.Replace("\\\\", "\\"), "steamapps", "common");
                                        if (Directory.Exists(libPath) && !_targetFolders.Contains(libPath))
                                            _targetFolders.Add(libPath);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void DetectEpicGames()
        {
            try
            {
                string epicPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                    "Epic", "EpicGamesLauncher", "Data", "Manifests");
                
                if (Directory.Exists(epicPath))
                {
                    var manifests = Directory.GetFiles(epicPath, "*.item");
                    foreach (var manifest in manifests)
                    {
                        try
                        {
                            var json = File.ReadAllText(manifest);
                            var match = System.Text.RegularExpressions.Regex.Match(json, "\"InstallLocation\"\\s*:\\s*\"(.+?)\"");
                            if (match.Success)
                            {
                                string installPath = match.Groups[1].Value.Replace("\\\\", "\\");
                                if (Directory.Exists(installPath) && !_targetFolders.Contains(installPath))
                                    _targetFolders.Add(installPath);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void DetectGOGGames()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games");
                if (key != null)
                {
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using var gameKey = key.OpenSubKey(subKeyName);
                        string? path = gameKey?.GetValue("path") as string;
                        if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && !_targetFolders.Contains(path))
                            _targetFolders.Add(path);
                    }
                }
            }
            catch { }
        }

        private void DetectEAGames()
        {
            try
            {
                string eaPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EA Games");
                if (Directory.Exists(eaPath))
                    _targetFolders.Add(eaPath);
            }
            catch { }
        }

        private void AddFallbackPaths()
        {
            var fallbacks = new[]
            {
                @"C:\Games",
                @"D:\Games",
                @"E:\Games",
                @"C:\Program Files (x86)\Steam\steamapps\common",
                @"C:\Program Files\Steam\steamapps\common"
            };

            foreach (var path in fallbacks)
            {
                if (Directory.Exists(path) && !_targetFolders.Contains(path))
                    _targetFolders.Add(path);
            }
        }

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

            return results.OrderBy(g => g.Name).ToList();
        }

        private void ScanDirectory(string path, List<GameItem> results, int maxResults, int depth = 0)
        {
            if (results.Count >= maxResults || depth > 3) return;

            try
            {
                // 1. Get .exe files
                var files = Directory.GetFiles(path, "*.exe");
                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    // EXTENSIVE FILTERING for "Real Games"
                    if (_skipFiles.Any(s => fileName.Contains(s, StringComparison.OrdinalIgnoreCase))) continue;
                    
                    // Skip if path contains typical system folders
                    if (path.Contains("Windows", StringComparison.OrdinalIgnoreCase) || 
                        path.Contains("System32", StringComparison.OrdinalIgnoreCase) ||
                        path.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)) continue;

                    // Requirements check: must not be a tiny file (games are usually > 1MB)
                    var info = new FileInfo(file);
                    if (info.Length < 1024 * 1024) continue;

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
