using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using WpfColor = System.Windows.Media.Color;

namespace NvidiaCi
{
    public partial class ScreenFilterWindow : Window
    {
        // Win32 API to hide from Alt+Tab and make click-through
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public ScreenFilterWindow()
        {
            InitializeComponent();
            PositionWindow();
            
            // Hide from Alt+Tab and make click-through
            this.SourceInitialized += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                // Combine TOOLWINDOW (hide from Alt+Tab) and TRANSPARENT (click-through)
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT);
            };
            
            System.Diagnostics.Debug.WriteLine($"ScreenFilterWindow initialized: {this.Width}x{this.Height}");
        }

        public void PositionWindow()
        {
            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
            
            System.Diagnostics.Debug.WriteLine($"ScreenFilterWindow positioned: {this.Width}x{this.Height} at ({this.Left},{this.Top})");
        }

        /// <summary>
        /// Apply a color overlay filter. Pass Colors.Transparent to disable.
        /// </summary>
        public void ApplyFilter(FilterPreset preset)
        {
            switch (preset)
            {
                case FilterPreset.None:
                    FilterRect.Fill = new SolidColorBrush(WpfColor.FromArgb(0, 0, 0, 0));
                    FilterRect.Effect = null;
                    break;

                case FilterPreset.Vivid:
                    // Warm slight saturation boost via orange tint
                    FilterRect.Fill = new SolidColorBrush(WpfColor.FromArgb(18, 255, 140, 0));
                    FilterRect.Effect = null;
                    break;

                case FilterPreset.CoolBlue:
                    FilterRect.Fill = new SolidColorBrush(WpfColor.FromArgb(20, 30, 80, 200));
                    FilterRect.Effect = null;
                    break;

                case FilterPreset.NightMode:
                    // Red tint to reduce blue light
                    FilterRect.Fill = new SolidColorBrush(WpfColor.FromArgb(35, 180, 20, 0));
                    FilterRect.Effect = null;
                    break;

                case FilterPreset.Cinematic:
                    // Dark vignette-style desaturated look
                    FilterRect.Fill = new SolidColorBrush(WpfColor.FromArgb(25, 0, 0, 0));
                    FilterRect.Effect = null;
                    break;

                case FilterPreset.Reading:
                    // Sepia-like warm tone for comfortable reading (reduces blue light)
                    FilterRect.Fill = new SolidColorBrush(WpfColor.FromArgb(30, 255, 240, 200));
                    FilterRect.Effect = null;
                    break;

                case FilterPreset.Sharpen:
                    FilterRect.Fill = new SolidColorBrush(WpfColor.FromArgb(0, 0, 0, 0));
                    // Simulate sharpen via blur on a dark overlay (contrast edge trick)
                    FilterRect.Effect = new BlurEffect { Radius = 0.5, KernelType = KernelType.Gaussian };
                    break;
            }
        }

        /// <summary>
        /// Apply a custom RGBA tint with given opacity (0-100).
        /// </summary>
        public void ApplyCustomTint(WpfColor color, double opacity)
        {
            byte alpha = (byte)(opacity / 100.0 * 255);
            FilterRect.Fill = new SolidColorBrush(WpfColor.FromArgb(alpha, color.R, color.G, color.B));
            FilterRect.Effect = null;
        }
    }

    public enum FilterPreset
    {
        None,
        Vivid,
        CoolBlue,
        NightMode,
        Cinematic,
        Reading,
        Sharpen
    }
}
