using System;
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
            
            // PENTING: Paksa pembuatan Handle agar WinAPI bisa mendaftarkan hotkey 
            // meskipun jendela belum ditampilkan (Show).
            var handle = new WindowInteropHelper(this).EnsureHandle();
            
            // Daftarkan hotkey segera setelah handle tersedia
            RegisterGlobalHotkey(handle);

            this.Closed += OnClosed;
            PositionWindow();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Kita pindahkan LoadData ke sini agar daftar game di-refresh saat jendela muncul
            LoadData();
        }

        private void LoadData()
        {
            var games = _dataManager.LoadGames();
            if (games.Count == 0)
            {
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
                GameListBox.ItemsSource = games;
                _dataManager.SaveGames(games);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error scanning games: {ex.Message}");
            }
        }

        // --- WINAPI HOTKEY LOGIC ---

        private void RegisterGlobalHotkey(IntPtr handle)
        {
            var source = HwndSource.FromHwnd(handle);
            source?.AddHook(WndProc);

            uint vk = (uint)HotkeyHelper.GetVirtualKeyCode(System.Windows.Input.Key.Z);
            bool success = HotkeyHelper.RegisterHotKey(handle, HOTKEY_ID, HotkeyHelper.MOD_ALT, vk);
            
            if (!success)
            {
                // Jika gagal, biasanya karena aplikasi lain sudah pakai Alt+Z
                Debug.WriteLine("Failed to register hotkey.");
            }
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
                // Gunakan dispatcher untuk memastikan berjalan di UI Thread
                this.Dispatcher.Invoke(() => {
                    if (System.Windows.Application.Current is App app) 
                        app.ToggleOverlay();
                });
                handled = true;
            }
            return IntPtr.Zero;
        }

        // --- SISANYA TETAP SAMA ---

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
                if (string.IsNullOrEmpty(exePath) || !System.IO.File.Exists(exePath)) return;
                Process.Start(new ProcessStartInfo { 
                    FileName = exePath, 
                    WorkingDirectory = System.IO.Path.GetDirectoryName(exePath),
                    UseShellExecute = true 
                });
                this.Hide();
            }
            catch { }
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e) => RefreshGameList();
        private void OnClosed(object? sender, EventArgs e) => UnregisterGlobalHotkey();
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Hide();

        private void PositionWindow()
        {
            this.Width = 320;
            this.Left = SystemParameters.WorkArea.Right - this.Width;
            this.Top = 0;
            this.Height = SystemParameters.WorkArea.Height;
        }
    }
}
