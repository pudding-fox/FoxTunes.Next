using FoxTunes.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public class MiniPlaylistTemplateSelector : DataTemplateSelector
    {
        public static readonly IConfiguration Configuration = ComponentRegistry.Instance.GetComponent<IConfiguration>();

        public static readonly BooleanConfigurationElement ShowArtwork = Configuration.GetElement<BooleanConfigurationElement>(
            MiniPlayerBehaviourConfiguration.SECTION,
            MiniPlayerBehaviourConfiguration.PLAYLIST_ARTWORK_ELEMENT
        );

        public DataTemplate DefaultTemplate { get; set; }

        public DataTemplate ArtworkTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (ShowArtwork.Value)
            {
                return this.ArtworkTemplate;
            }
            else
            {
                return this.DefaultTemplate;
            }
        }
    }
}
