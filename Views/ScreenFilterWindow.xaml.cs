using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace NvidiaCi
{
    public partial class ScreenFilterWindow : Window
    {
        public ScreenFilterWindow()
        {
            InitializeComponent();
            PositionWindow();
        }

        public void PositionWindow()
        {
            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
        }

        /// <summary>
        /// Apply a color overlay filter. Pass Colors.Transparent to disable.
        /// </summary>
        public void ApplyFilter(FilterPreset preset)
        {
            switch (preset)
            {
                case FilterPreset.None:
                    FilterRect.Fill = new SolidColorBrush(Colors.Transparent);
                    FilterRect.Effect = null;
                    break;

                case FilterPreset.Vivid:
                    // Warm slight saturation boost via orange tint
                    FilterRect.Fill = new SolidColorBrush(Color.FromArgb(18, 255, 140, 0));
                    FilterRect.Effect = null;
                    break;

                case FilterPreset.CoolBlue:
                    FilterRect.Fill = new SolidColorBrush(Color.FromArgb(20, 30, 80, 200));
                    FilterRect.Effect = null;
                    break;

                case FilterPreset.NightMode:
                    // Red tint to reduce blue light
                    FilterRect.Fill = new SolidColorBrush(Color.FromArgb(35, 180, 20, 0));
                    FilterRect.Effect = null;
                    break;

                case FilterPreset.Cinematic:
                    // Dark vignette-style desaturated look
                    FilterRect.Fill = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0));
                    FilterRect.Effect = null;
                    break;

                case FilterPreset.Sharpen:
                    FilterRect.Fill = new SolidColorBrush(Colors.Transparent);
                    // Simulate sharpen via blur on a dark overlay (contrast edge trick)
                    FilterRect.Effect = new BlurEffect { Radius = 0.5, KernelType = KernelType.Gaussian };
                    break;
            }
        }

        /// <summary>
        /// Apply a custom RGBA tint with given opacity (0-100).
        /// </summary>
        public void ApplyCustomTint(Color color, double opacity)
        {
            byte alpha = (byte)(opacity / 100.0 * 255);
            FilterRect.Fill = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
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
        Sharpen
    }
}
