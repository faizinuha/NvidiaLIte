using System;
using System.Windows;
using System.Windows.Forms;

namespace NvidiaCi
{
    public partial class App : System.Windows.Application
    {
        private NotifyIcon? _notifyIcon;
        private OverlayWindow? _overlayWindow;
        private static System.Threading.Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {            
            // Multiple instance prevention
            _mutex = new System.Threading.Mutex(true, "NvidiaCiMutex", out bool createdNew);
            if (!createdNew)
            {
                System.Windows.MessageBox.Show("Aplikasi sudah berjalan.", "NVIDIA Lite", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Windows.Application.Current.Shutdown();
                return;
            }

            this.DispatcherUnhandledException += (s, args) => {
                System.Diagnostics.Debug.WriteLine($"UNHANDLED ERROR: {args.Exception.Message}\n\nStack: {args.Exception.StackTrace}");
                args.Handled = true;
            };

            base.OnStartup(e);

            // 1. Buat instance jendela overlay tapi jangan tampilkan
            try {
                _overlayWindow = new OverlayWindow();
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"CRASH AT INIT: {ex.Message}\n{ex.InnerException?.Message}");
                ShutdownApp();
                return;
            }

            // 2. Setup NotifyIcon (System Tray) with robust icon loading
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "NVIDIA Lite";
            
            try 
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    // Try relative path as fallback
                    _notifyIcon.Icon = new System.Drawing.Icon("Assets/icon.ico");
                }
            }
            catch (Exception ex)
            {
                // Fallback to system icon if custom icon fails
                System.Diagnostics.Debug.WriteLine($"Failed to load icon: {ex.Message}");
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            
            _notifyIcon.Visible = true;

            // 3. Tambahkan event handler untuk double-click
            _notifyIcon.DoubleClick += (s, args) => ToggleOverlay();

            // 4. Buat menu konteks (klik kanan)
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show/Hide Sidebar (Alt+Z)", null, (s, args) => ToggleOverlay());
            contextMenu.Items.Add("-"); // Separator
            contextMenu.Items.Add("Exit", null, (s, args) => ShutdownApp());
            _notifyIcon.ContextMenuStrip = contextMenu;

            // Penting: Hentikan aplikasi dari shutdown otomatis
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        public async void ToggleOverlay()
        {
            if (_overlayWindow == null) return;

            if (_overlayWindow.IsVisible)
            {
                await _overlayWindow.HideAnimated();
            }
            else
            {
                await _overlayWindow.ShowAnimated();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose(); // Bersihkan ikon saat aplikasi ditutup
            base.OnExit(e);
        }

        private void ShutdownApp()
        {
            // Ini akan memicu OnExit dan menutup aplikasi dengan benar
            System.Windows.Application.Current.Shutdown();
        }
    }
}
