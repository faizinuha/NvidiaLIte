using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace myapp
{
    public partial class OverlayWindow : Window
    {
        private const int HOTKEY_ID = 9000;
        private readonly GameDataManager _dataManager = new GameDataManager();

        public OverlayWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
            this.Closed += OnClosed;
            PositionWindow();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RegisterGlobalHotkey();
            LoadData(); // Load dari JSON saat startup
        }

        private void LoadData()
        {
            var games = _dataManager.LoadGames();
            
            if (games.Count == 0)
            {
                // Jika tidak ada data tersimpan, coba scan otomatis pertama kali
                RefreshGameList();
            }
            else
            {
                GameListBox.ItemsSource = games;
            }
        }

        private void RefreshGameList()
        {
            try
            {
                var scanner = new GameScanner();
                var games = scanner.ScanForGames(100);
                
                // Simpan ke ListBox UI
                GameListBox.ItemsSource = games;
                
                // Simpan ke file JSON
                _dataManager.SaveGames(games);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error scanning games: {ex.Message}");
            }
        }

        private void GameListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GameListBox.SelectedItem is GameItem selectedGame)
            {
                LaunchGame(selectedGame.Path);
                GameListBox.SelectedItem = null;
            }
        }

        private void LaunchGame(string exePath)
        {
            try
            {
                if (string.IsNullOrEmpty(exePath) || !System.IO.File.Exists(exePath))
                {
                    System.Windows.MessageBox.Show("Game executable not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(exePath),
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                this.Hide();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Launch Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshGameList();
        }

        // --- WINAPI HOTKEY LOGIC ---

        private void OnClosed(object? sender, EventArgs e)
        {
            UnregisterGlobalHotkey();
        }

        private void RegisterGlobalHotkey()
        {
            var handle = new WindowInteropHelper(this).EnsureHandle();
            var source = HwndSource.FromHwnd(handle);
            source?.AddHook(WndProc);

            uint vk = (uint)HotkeyHelper.GetVirtualKeyCode(System.Windows.Input.Key.Z);
            HotkeyHelper.RegisterHotKey(handle, HOTKEY_ID, HotkeyHelper.MOD_ALT, vk);
        }

        private void UnregisterGlobalHotkey()
        {
            var handle = new WindowInteropHelper(this).Handle;
            HotkeyHelper.UnregisterHotKey(handle, HOTKEY_ID);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == HotkeyHelper.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                if (System.Windows.Application.Current is App app) app.ToggleOverlay();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void PositionWindow()
        {
            this.Width = 320;
            this.Left = SystemParameters.WorkArea.Right - this.Width;
            this.Top = 0;
            this.Height = SystemParameters.WorkArea.Height;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
