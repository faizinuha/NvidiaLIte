using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace NvidiaCi
{
    public static class SystemControlHelper
    {
        // --- VOLUME CONTROL (SIMPLE) ---
        private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        private const int WM_APPCOMMAND = 0x319;

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        public static void IncreaseVolume(IntPtr handle)
        {
            SendMessageW(handle, WM_APPCOMMAND, handle, (IntPtr)APPCOMMAND_VOLUME_UP);
        }

        public static void DecreaseVolume(IntPtr handle)
        {
            SendMessageW(handle, WM_APPCOMMAND, handle, (IntPtr)APPCOMMAND_VOLUME_DOWN);
        }

        public static void ToggleMute(IntPtr handle)
        {
            SendMessageW(handle, WM_APPCOMMAND, handle, (IntPtr)APPCOMMAND_VOLUME_MUTE);
        }

        // --- WIFI CONTROL (Via netsh) ---

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

                    // Parse SSID line by line
                    // Output format: "SSID 1 : NetworkName"
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
                // Connect to a profile that matches the SSID name
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
