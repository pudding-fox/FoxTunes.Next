using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace FoxTunes
{
    public static partial class Extensions
    {
        private static ConditionalWeakTable<IColorPalette, Color[]> Gradients = new ConditionalWeakTable<IColorPalette, Color[]>();

        public static Color[] GetGradient(this IColorPalette colorPalette)
        {
            var colors = default(Color[]);
            if (!Gradients.TryGetValue(colorPalette, out colors))
            {
                try
                {
                    colors = colorPalette.Value.ToColorStops().ToGradient();
                }
                catch
                {
                    //Nothing can be done.
                }
                Gradients.Add(colorPalette, colors);
            }
            return colors;
        }
    }
}
