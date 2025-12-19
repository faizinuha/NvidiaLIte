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

            _overlayWindow = new OverlayWindow();
            // The window is created but not shown. We will control its visibility.

            _notifyIcon = new NotifyIcon();
            // _notifyIcon.Icon = new System.Drawing.Icon("Assets/icon.ico"); // This line is commented out.
            _notifyIcon.Text = "My WPF App";
            _notifyIcon.Visible = true;

            _notifyIcon.DoubleClick += (s, args) => ShowOverlay();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show/Hide Overlay", null, (s, args) => ToggleOverlay());
            contextMenu.Items.Add("Exit", null, (s, args) => ShutdownApp());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Dispose();
            }
            base.OnExit(e);
        }

        public void ToggleOverlay()
        {
            if (_overlayWindow?.IsVisible == true)
            {
                _overlayWindow.Hide();
            }
            else
            {
                ShowOverlay();
            }
        }

        public void ShowOverlay()
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.Show();
                _overlayWindow.Activate(); // Bring to front
            }
        }

        private void ShutdownApp()
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
