using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace NvidiaCi
{
    public partial class OverlayWindow : Window
    {
        private const int HOTKEY_OVERLAY_ID = 9000;
        private const int HOTKEY_SCREENSHOT_ID = 9001;
        private readonly GameDataManager _dataManager = new GameDataManager();

        private bool _isChangingOverlayHotkey = false;
        private bool _isChangingScreenshotHotkey = false;
        private System.Windows.Input.Key _tempOverlayKey = System.Windows.Input.Key.Z;
        private System.Windows.Input.Key _tempScreenshotKey = System.Windows.Input.Key.F10;


        public OverlayWindow()
        {
            InitializeComponent();
            
            var handle = new WindowInteropHelper(this).EnsureHandle();
            RegisterGlobalHotkey(handle);

            this.Closed += OnClosed;
            PositionWindow();
            
            // Start hidden but initialized
            this.Opacity = 0;
            this.Visibility = Visibility.Collapsed;
        }

        public Task ShowAnimated()
        {
            PositionWindow();
            this.Visibility = Visibility.Visible;
            this.Opacity = 1;
            
            var sb = (Storyboard)this.Resources["ShowSidebar"];
            sb.Begin();
            
            this.Activate();
            this.Topmost = false;
            this.Topmost = true;
            this.Focus();

            return Task.CompletedTask;
        }

        public async Task HideAnimated()
        {
            var sb = (Storyboard)this.Resources["HideSidebar"];
            sb.Begin();
            
            // Wait for animation to finish (matching duration in XAML: 0.3s)
            await Task.Delay(350);
            this.Visibility = Visibility.Collapsed;
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
                    SyncSystemSliders();
                    LoadWifiNetworks();
                    break;
            }

            // Trigger Fade-in Animation
            var sb = (Storyboard)this.Resources["FadeInContent"];
            sb.Begin(ContentView);
        }

        private void SyncSystemSliders()
        {
            try {
                // Audio
                VolumeSlider.Value = SystemControlHelper.GetSystemVolume();
                // Brightness
                BrightnessSlider.Value = SystemControlHelper.GetBrightness();
            } catch { }
        }

        private void LoadWifiNetworks()
        {
            try {
                var networks = SystemControlHelper.GetAvailableNetworks();
                WifiComboBox.ItemsSource = networks;
                if (networks.Any()) WifiComboBox.SelectedIndex = 0;
            } catch { }
        }

        private void WifiComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (WifiComboBox.SelectedItem is string ssid)
            {
                WifiPasswordBox.Visibility = Visibility.Visible;
                WifiConnectBtn.Visibility = Visibility.Visible;
                WifiPasswordBox.Password = "";
            }
        }

        private void ConnectWifi_Click(object sender, RoutedEventArgs e)
        {
            if (WifiComboBox.SelectedItem is string ssid)
            {
                string password = WifiPasswordBox.Password;
                if (string.IsNullOrEmpty(password)) 
                {
                    System.Windows.MessageBox.Show("Password is required for this Beta version.");
                    return;
                }
                
                // netsh connection with password requires a profile or direct command
                // For this demo, we'll simulate the attempt
                Task.Run(() => SystemControlHelper.ConnectToNetwork(ssid));
                WifiPasswordBox.Visibility = Visibility.Collapsed;
                WifiConnectBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsLoaded)
            {
                SystemControlHelper.SetSystemVolume((float)e.NewValue);
            }
        }

        private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsLoaded)
            {
                SystemControlHelper.SetBrightness((int)e.NewValue);
            }
        }

        private void ManageWifi_Click(object sender, RoutedEventArgs e)
        {
            SystemControlHelper.OpenWifiSettings();
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
                        .Select(f => {
                            try {
                                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(f);
                                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                bitmap.EndInit();
                                return new { Path = f, Source = bitmap };
                            } catch { return null; }
                        })
                        .Where(x => x != null)
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
            if (sender is System.Windows.Controls.MenuItem mi && mi.Tag is string imagePath)
            {
                Process.Start(new ProcessStartInfo(imagePath) { UseShellExecute = true });
            }
        }

        private void CopyImage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem mi && mi.Tag is string imagePath)
            {
                try
                {
                    // Copy file to clipboard (as file and as image)
                    var paths = new System.Collections.Specialized.StringCollection { imagePath };
                    System.Windows.Clipboard.SetFileDropList(paths);
                    
                    // Also copy as bitmap image for quick paste in Discord/PS
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(imagePath));
                    System.Windows.Clipboard.SetImage(bitmap);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to copy image: {ex.Message}");
                }
            }
        }

        private void GalleryItem_PreviewClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext != null)
            {
                dynamic data = fe.DataContext;
                FullImageDisplay.Source = data.Source;
                ImagePreviewModal.Visibility = Visibility.Visible;
            }
        }

        private void ClosePreview_Click(object sender, RoutedEventArgs e)
        {
            ImagePreviewModal.Visibility = Visibility.Collapsed;
        }

        private void ClosePreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ImagePreviewModal.Visibility = Visibility.Collapsed;
        }

        private void CopyCurrentPreview_Click(object sender, RoutedEventArgs e)
        {
            if (FullImageDisplay.Source is System.Windows.Media.Imaging.BitmapSource bitmap)
            {
                System.Windows.Clipboard.SetImage(bitmap);
            }
        }

        private void SaveImageAs_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem mi && mi.Tag is string imagePath)
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
            if (sender is System.Windows.Controls.MenuItem mi && mi.Tag is string imagePath)
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
            _isChangingOverlayHotkey = true;
            _isChangingScreenshotHotkey = false;
            OverlayHotkeyText.Text = "PRESS ANY KEY...";
            ChangeOverlayBtn.Visibility = Visibility.Collapsed;
        }

        private void ChangeScreenshotHotkey_Click(object sender, RoutedEventArgs e)
        {
            _isChangingScreenshotHotkey = true;
            _isChangingOverlayHotkey = false;
            ScreenshotHotkeyText.Text = "PRESS ANY KEY...";
            ChangeScreenshotBtn.Visibility = Visibility.Collapsed;
        }

        private void OnWindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                if (ImagePreviewModal.Visibility == Visibility.Visible)
                {
                    ImagePreviewModal.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                    return;
                }
            }

            // Ignore modifiers themselves as primary keys if needed, or handle them
            if (e.Key == System.Windows.Input.Key.LeftAlt || e.Key == System.Windows.Input.Key.RightAlt ||
                e.Key == System.Windows.Input.Key.LeftCtrl || e.Key == System.Windows.Input.Key.RightCtrl ||
                e.Key == System.Windows.Input.Key.LeftShift || e.Key == System.Windows.Input.Key.RightShift)
                return;

            if (_isChangingOverlayHotkey)
            {
                _tempOverlayKey = e.Key;
                OverlayHotkeyText.Text = $"Alt + {_tempOverlayKey}";
                _isChangingOverlayHotkey = false;
                ChangeOverlayBtn.Visibility = Visibility.Visible;
                
                var handle = new WindowInteropHelper(this).Handle;
                HotkeyHelper.UnregisterHotKey(handle, HOTKEY_OVERLAY_ID);
                uint vk = (uint)HotkeyHelper.GetVirtualKeyCode(_tempOverlayKey);
                HotkeyHelper.RegisterHotKey(handle, HOTKEY_OVERLAY_ID, HotkeyHelper.MOD_ALT, vk);
                e.Handled = true;
            }
            else if (_isChangingScreenshotHotkey)
            {
                _tempScreenshotKey = e.Key;
                ScreenshotHotkeyText.Text = _tempScreenshotKey.ToString();
                _isChangingScreenshotHotkey = false;
                ChangeScreenshotBtn.Visibility = Visibility.Visible;

                var handle = new WindowInteropHelper(this).Handle;
                HotkeyHelper.UnregisterHotKey(handle, HOTKEY_SCREENSHOT_ID);
                uint vk = (uint)HotkeyHelper.GetVirtualKeyCode(_tempScreenshotKey);
                HotkeyHelper.RegisterHotKey(handle, HOTKEY_SCREENSHOT_ID, 0, vk);
                e.Handled = true;
            }
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

        private async void LaunchGame(string exePath)
        {
            try
            {
                if (string.IsNullOrEmpty(exePath) || !System.IO.File.Exists(exePath)) return;
                Process.Start(new ProcessStartInfo { 
                    FileName = exePath, 
                    WorkingDirectory = System.IO.Path.GetDirectoryName(exePath),
                    UseShellExecute = true 
                });
                await HideAnimated();
            }
            catch { }
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e) => RefreshGameList();
        private void OnClosed(object? sender, EventArgs e) => UnregisterGlobalHotkey();
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string query = SearchBox.Text.ToLower().Trim();
                var games = _dataManager.LoadGames();
                
                if (string.IsNullOrEmpty(query))
                {
                    GameListBox.ItemsSource = games;
                }
                else
                {
                    var filtered = games.Where(g => g.Name.ToLower().Contains(query)).ToList();
                    GameListBox.ItemsSource = filtered;
                    NoGamesFoundText.Visibility = filtered.Any() ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch { }
        }

        private async void Background_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Grid)
            {
                await HideAnimated();
            }
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e) => await HideAnimated();

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
