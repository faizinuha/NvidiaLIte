using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace NvidiaCi
{
    public partial class OverlayWindow : Window
    {
        private const int HOTKEY_OVERLAY_ID = 9000;
        private const int HOTKEY_SCREENSHOT_ID = 9001;
        private readonly GameDataManager _dataManager = new GameDataManager();

        private bool _isChangingOverlayHotkey = false;
        private bool _isChangingScreenshotHotkey = false;

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
            LoadData();
            LoadGallery();
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb && rb.Tag is string viewName)
            {
                SwitchView(viewName);
            }
        }

        private void SwitchView(string viewName)
        {
            DashboardView.Visibility = Visibility.Collapsed;
            GalleryView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Collapsed;

            switch (viewName)
            {
                case "Dashboard":
                    DashboardView.Visibility = Visibility.Visible;
                    HeaderText.Text = " LITE";
                    break;
                case "Gallery":
                    GalleryView.Visibility = Visibility.Visible;
                    HeaderText.Text = " GALLERY";
                    LoadGallery();
                    break;
                case "Settings":
                    SettingsView.Visibility = Visibility.Visible;
                    HeaderText.Text = " SETTINGS";
                    break;
            }
        }

        private void LoadGallery()
        {
            try
            {
                string screenshotPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), 
                    "Screenshots");

                if (System.IO.Directory.Exists(screenshotPath))
                {
                    var files = System.IO.Directory.GetFiles(screenshotPath, "*.png")
                        .OrderByDescending(f => System.IO.File.GetCreationTime(f))
                        .Take(12)
                        .ToList();

                    GalleryItemsControl.ItemsSource = files;
                    NoGalleryText.Visibility = files.Any() ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    NoGalleryText.Visibility = Visibility.Visible;
                }
            }
            catch { }
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
                NoGamesFoundText.Visibility = games.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void RefreshGameList()
        {
            try
            {
                var scanner = new GameScanner();
                var games = scanner.ScanForGames(200); // Increased limit
                GameListBox.ItemsSource = games;
                _dataManager.SaveGames(games);
                NoGamesFoundText.Visibility = games.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error scanning games: {ex.Message}");
            }
        }
        
        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem mi && mi.DataContext is string imagePath)
            {
                Process.Start(new ProcessStartInfo(imagePath) { UseShellExecute = true });
            }
        }

        private void SaveImageAs_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem mi && mi.DataContext is string imagePath)
            {
                var sfd = new Microsoft.Win32.SaveFileDialog {
                    FileName = System.IO.Path.GetFileName(imagePath),
                    DefaultExt = ".png",
                    Filter = "PNG Image (.png)|*.png|All files (*.*)|*.*"
                };

                if (sfd.ShowDialog() == true)
                {
                    System.IO.File.Copy(imagePath, sfd.FileName, true);
                }
            }
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem mi && mi.DataContext is string imagePath)
            {
                try {
                    System.IO.File.Delete(imagePath);
                    LoadGallery();
                } catch { }
            }
        }

        // --- WINAPI HOTKEY LOGIC ---

        private void RegisterGlobalHotkey(IntPtr handle)
        {
            var source = HwndSource.FromHwnd(handle);
            source?.AddHook(WndProc);

            // Default Overlay: Alt + Z
            uint vkOverlay = (uint)HotkeyHelper.GetVirtualKeyCode(System.Windows.Input.Key.Z);
            HotkeyHelper.RegisterHotKey(handle, HOTKEY_OVERLAY_ID, HotkeyHelper.MOD_ALT, vkOverlay);

            // Default Screenshot: F10
            uint vkScreenshot = (uint)HotkeyHelper.GetVirtualKeyCode(System.Windows.Input.Key.F10);
            HotkeyHelper.RegisterHotKey(handle, HOTKEY_SCREENSHOT_ID, 0, vkScreenshot);
        }

        private void UnregisterGlobalHotkey()
        {
            var handle = new WindowInteropHelper(this).Handle;
            HotkeyHelper.UnregisterHotKey(handle, HOTKEY_OVERLAY_ID);
            HotkeyHelper.UnregisterHotKey(handle, HOTKEY_SCREENSHOT_ID);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == HotkeyHelper.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_OVERLAY_ID)
                {
                    this.Dispatcher.Invoke(() => {
                        if (System.Windows.Application.Current is App app) 
                            app.ToggleOverlay();
                    });
                    handled = true;
                }
                else if (id == HOTKEY_SCREENSHOT_ID)
                {
                    this.Dispatcher.Invoke(() => {
                        CaptureScreenshot();
                    });
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private async void CaptureScreenshot()
        {
            // Flash Effect
            FlashBorder.Visibility = Visibility.Visible;
            await Task.Delay(50);
            FlashBorder.Visibility = Visibility.Collapsed;

            string path = ScreenshotHelper.CaptureScreen();
            if (!string.IsNullOrEmpty(path))
            {
                if (this.IsVisible && HeaderText.Text.Contains("GALLERY"))
                {
                    LoadGallery();
                }
            }
        }

        private void ChangeOverlayHotkey_Click(object sender, RoutedEventArgs e)
        {
            OverlayHotkeyText.Text = "PRESS ANY KEY...";
            // In a real app we'd hook KeyDown here, but for now let's just show it's interactive
        }

        private void ChangeScreenshotHotkey_Click(object sender, RoutedEventArgs e)
        {
            ScreenshotHotkeyText.Text = "PRESS ANY KEY...";
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
        private void Background_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Grid)
            {
                this.Hide();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Hide();

        public void PositionWindow()
        {
            // Full Screen coverage
            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
            
            // Adjust Sidebar Width for Grid layout
            SidebarBorder.Width = 650; 

            // Ensure transparency and topmost
            this.Topmost = false;
            this.Topmost = true;
        }
    }
}
