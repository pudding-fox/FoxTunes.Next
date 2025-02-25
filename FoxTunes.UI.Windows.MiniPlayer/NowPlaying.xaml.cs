using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for NowPlaying.xaml
    /// </summary>
    [UIComponent("CFF16494-CB86-4483-99C7-07E496FE894A", role: UIComponentRole.Info)]
    public partial class NowPlaying : ConfigurableUIComponentBase
    {
        public const string CATEGORY = "C0803688-E230-4DFF-AFCB-931B3AA5BE6D";

        public NowPlaying()
        {
            this.InitializeComponent();
        }

        protected override void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.Configuration == null)
            {
                var viewModel = default(global::FoxTunes.ViewModel.NowPlaying);
                if (this.TryFindResource<global::FoxTunes.ViewModel.NowPlaying>("ViewModel", out viewModel))
                {
                    viewModel.Configuration = ComponentRegistry.Instance.GetComponent<IConfiguration>();
                }
            }
            base.OnLoaded(sender, e);
        }

        public override IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return CATEGORY;
            }
        }

        public override IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.Configuration != null)
                {
                    return base.Invocations;
                }
                return Enumerable.Empty<IInvocationComponent>();
            }
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return NowPlayingConfiguration.GetConfigurationSections();
        }

        protected override Task<bool> ShowSettings()
        {
            return this.ShowSettings(
                Strings.NowPlaying_Name,
                NowPlayingConfiguration.SECTION
            );
        }
    }
}
