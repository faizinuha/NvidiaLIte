using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using NAudio.CoreAudioApi;

namespace NvidiaCi
{
    public static class SystemControlHelper
    {
        // --- VOLUME CONTROL (Precise with NAudio) ---
        public static float GetSystemVolume()
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                return device.AudioEndpointVolume.MasterVolumeLevelScalar * 100;
            }
            catch { return 50; }
        }

        public static void SetSystemVolume(float level)
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                device.AudioEndpointVolume.MasterVolumeLevelScalar = level / 100f;
            }
            catch { }
        }

        // --- BRIGHTNESS CONTROL (Via WMI) ---
        public static int GetBrightness()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBrightness"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        return (byte)obj.GetPropertyValue("CurrentBrightness");
                    }
                }
            }
            catch { }
            return 100; // Default or desktop fallback
        }

        public static void SetBrightness(int level)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBrightnessMethods"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        obj.InvokeMethod("WmiSetBrightness", new object[] { uint.MaxValue, (byte)level });
                        break;
                    }
                }
            }
            catch { }
        }

        // --- WIFI CONTROL (Keep existing) ---
        public static List<string> GetAvailableNetworks()
        {
            var networks = new List<string>();
            try
            {
                var psi = new ProcessStartInfo("netsh", "wlan show networks")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null) return networks;
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("SSID") && trimmed.Contains(":"))
                        {
                            var parts = trimmed.Split(new[] { ':' }, 2);
                            if (parts.Length == 2)
                            {
                                networks.Add(parts[1].Trim());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WiFi Scan Error: {ex.Message}");
            }
            return networks;
        }

        public static void ConnectToNetwork(string ssid)
        {
            try
            {
                var psi = new ProcessStartInfo("netsh", $"wlan connect name=\"{ssid}\"")
                {
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                Process.Start(psi);
            }
            catch { }
        }

        public static void OpenWifiSettings()
        {
            Process.Start(new ProcessStartInfo("ms-settings:network-wifi") { UseShellExecute = true });
        }
    }
}
