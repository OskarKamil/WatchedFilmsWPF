using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace WatchedFilmsTracker.Source
{
    internal class SystemAccentColour
    {
        private static Color currentAccentColor = GetAccentColourRGB();

        public static event EventHandler AccentColorChanged;

        public static Color GetAccentColourRGB()
        {
            return ((SolidColorBrush)SystemParameters.WindowGlassBrush).Color;
        }

        public static Color GetBrightAccentColourRGB()
        {
            Color color = GetAccentColourRGB();

            // Convert RGB to HSL
            (double h, double s, double l) = RgbToHsl(color.R, color.G, color.B);

            // Set lightness
            l = 0.85;

            // Convert back to RGB
            Color brightColor = HslToRgb(h, s, l);
            return brightColor;
        }

        public static void Initialize()
        {
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        private static Color HslToRgb(double h, double s, double l)
        {
            double r, g, b;

            if (s == 0)
            {
                r = g = b = l; // achromatic
            }
            else
            {
                Func<double, double, double, double> hue2rgb = (p, q, t) =>
                {
                    if (t < 0) t += 1;
                    if (t > 1) t -= 1;
                    if (t < 1.0 / 6) return p + (q - p) * 6 * t;
                    if (t < 1.0 / 2) return q;
                    if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
                    return p;
                };

                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;
                r = hue2rgb(p, q, h + 1.0 / 3);
                g = hue2rgb(p, q, h);
                b = hue2rgb(p, q, h - 1.0 / 3);
            }

            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                var newAccentColor = GetAccentColourRGB();
                if (newAccentColor != currentAccentColor)
                {
                    currentAccentColor = newAccentColor;
                    // Invoke the custom event
                    AccentColorChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        private static (double h, double s, double l) RgbToHsl(byte r, byte g, byte b)
        {
            double dr = r / 255.0;
            double dg = g / 255.0;
            double db = b / 255.0;
            double max = Math.Max(dr, Math.Max(dg, db));
            double min = Math.Min(dr, Math.Min(dg, db));
            double h, s, l;
            l = (max + min) / 2.0;

            if (max == min)
            {
                h = s = 0; // achromatic
            }
            else
            {
                double d = max - min;
                s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

                if (max == dr)
                {
                    h = (dg - db) / d + (dg < db ? 6 : 0);
                }
                else if (max == dg)
                {
                    h = (db - dr) / d + 2;
                }
                else
                {
                    h = (dr - dg) / d + 4;
                }
                h /= 6.0;
            }

            return (h, s, l);
        }
    }
}