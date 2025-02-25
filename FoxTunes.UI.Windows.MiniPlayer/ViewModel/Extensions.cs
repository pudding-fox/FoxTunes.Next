using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public static partial class Extensions
    {
        public static T GetElement<T>(this IConfiguration configuration, IEnumerable<string> sectionIds, IEnumerable<string> elementIds) where T : ConfigurationElement
        {
            var pairs = new[]
            {
                sectionIds.ToArray(),
                elementIds.ToArray()
            };
            for (var a = 0; a < pairs[0].Length; a++)
            {
                var sectionId = pairs[0][a];
                var elementId = pairs[1][a];
                var element = configuration.GetElement<T>(sectionId, elementId);
                if (element != null)
                {
                    return element;
                }
            }
            return null;
        }
    }
}
