using System;
using System.Windows;
using System.Windows.Forms;

namespace myapp
{
    public partial class App : System.Windows.Application
    {
        private NotifyIcon? _notifyIcon;
        private OverlayWindow? _overlayWindow;

        protected override void OnStartup(StartupEventArgs e)
        {            
            base.OnStartup(e);

            // 1. Buat instance jendela overlay tapi jangan tampilkan
            _overlayWindow = new OverlayWindow();

            // 2. Setup NotifyIcon (System Tray)
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = new System.Drawing.Icon("Assets/icon.ico"); // Pastikan file icon ada
            _notifyIcon.Text = "Game Hub Overlay";
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

        public void ToggleOverlay()
        {
            if (_overlayWindow == null) return;

            if (_overlayWindow.IsVisible)
            {
                _overlayWindow.Hide();
            }
            else
            {
                _overlayWindow.Show();
                _overlayWindow.Activate(); // Bawa ke depan
                _overlayWindow.Topmost = true; // Pastikan selalu di atas jendela lain
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
