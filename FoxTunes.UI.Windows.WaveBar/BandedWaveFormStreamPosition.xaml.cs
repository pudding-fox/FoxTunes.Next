using FoxTunes.Config;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for BandedWaveFormStreamPosition.xaml
    /// </summary>
    [UIComponent("6EDA5DCD-7A00-4933-A2EF-C70C99F7B36A", role: UIComponentRole.Playback)]
    [UIComponentToolbar(900, UIComponentToolbarAlignment.Stretch, false)]
    public partial class BandedWaveFormStreamPosition : ConfigurableUIComponentBase
    {
        public const string CATEGORY = "BEE11F64-A91C-461C-9199-98854BF68708";

        public BandedWaveFormStreamPosition()
        {
            this.InitializeComponent();
        }

        protected override void InitializeComponent(ICore core)
        {
            this.ThemeLoader = ComponentRegistry.Instance.GetComponent<ThemeLoader>();
            base.InitializeComponent(core);
        }

        public ThemeLoader ThemeLoader { get; private set; }

        public BooleanConfigurationElement Logarithmic { get; private set; }

        public IntegerConfigurationElement Smoothing { get; private set; }

        public SelectionConfigurationElement Mode { get; private set; }

        public TextConfigurationElement ColorPalette { get; private set; }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Logarithmic = this.Configuration.GetElement<BooleanConfigurationElement>(
                    BandedWaveFormStreamPositionConfiguration.SECTION,
                    BandedWaveFormStreamPositionConfiguration.DB_ELEMENT
                );
                this.Smoothing = this.Configuration.GetElement<IntegerConfigurationElement>(
                    BandedWaveFormStreamPositionConfiguration.SECTION,
                    BandedWaveFormStreamPositionConfiguration.SMOOTHING_ELEMENT
                );
                this.Mode = this.Configuration.GetElement<SelectionConfigurationElement>(
                    BandedWaveFormStreamPositionConfiguration.SECTION,
                    BandedWaveFormStreamPositionConfiguration.MODE_ELEMENT
                );
                this.ColorPalette = this.Configuration.GetElement<TextConfigurationElement>(
                    BandedWaveFormStreamPositionConfiguration.SECTION,
                    BandedWaveFormStreamPositionConfiguration.COLOR_PALETTE_ELEMENT
                );
            }
            base.OnConfigurationChanged();
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
                foreach (var component in this.ThemeLoader.SelectColorPalette(CATEGORY, this.ColorPalette, ColorPaletteRole.WaveForm))
                {
                    yield return component;
                }
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Logarithmic.Id,
                    this.Logarithmic.Name,
                    attributes: this.Logarithmic.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Smoothing.Id,
                    Strings.BandedWaveFormStreamPositionConfiguration_Smoothing_Off,
                    path: this.Smoothing.Name,
                    attributes: this.Smoothing.Value == BandedWaveFormStreamPositionConfiguration.SMOOTHING_MIN ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Smoothing.Id,
                    Strings.BandedWaveFormStreamPositionConfiguration_Smoothing_Low,
                    path: this.Smoothing.Name,
                    attributes: this.Smoothing.Value == BandedWaveFormStreamPositionConfiguration.SMOOTHING_LOW ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Smoothing.Id,
                    Strings.BandedWaveFormStreamPositionConfiguration_Smoothing_Medium,
                    path: this.Smoothing.Name,
                    attributes: this.Smoothing.Value == BandedWaveFormStreamPositionConfiguration.SMOOTHING_MEDIUM ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Smoothing.Id,
                    Strings.BandedWaveFormStreamPositionConfiguration_Smoothing_High,
                    path: this.Smoothing.Name,
                    attributes: this.Smoothing.Value == BandedWaveFormStreamPositionConfiguration.SMOOTHING_MAX ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                foreach (var option in this.Mode.Options)
                {
                    yield return new InvocationComponent(
                        CATEGORY,
                        option.Id,
                        option.Name,
                        path: this.Mode.Name,
                        attributes: this.Mode.Value == option ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                    );
                }
                foreach (var invocationComponent in base.Invocations)
                {
                    yield return invocationComponent;
                }
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            if (string.Equals(this.Logarithmic.Name, component.Name))
            {
                this.Logarithmic.Toggle();
            }
            else if (string.Equals(this.Smoothing.Name, component.Path))
            {
                if (string.Equals(component.Name, Strings.BandedWaveFormStreamPositionConfiguration_Smoothing_Off, StringComparison.OrdinalIgnoreCase))
                {
                    this.Smoothing.Value = BandedWaveFormStreamPositionConfiguration.SMOOTHING_MIN;
                }
                else if (string.Equals(component.Name, Strings.BandedWaveFormStreamPositionConfiguration_Smoothing_Low, StringComparison.OrdinalIgnoreCase))
                {
                    this.Smoothing.Value = BandedWaveFormStreamPositionConfiguration.SMOOTHING_LOW;
                }
                else if (string.Equals(component.Name, Strings.BandedWaveFormStreamPositionConfiguration_Smoothing_Medium, StringComparison.OrdinalIgnoreCase))
                {
                    this.Smoothing.Value = BandedWaveFormStreamPositionConfiguration.SMOOTHING_MEDIUM;
                }
                else if (string.Equals(component.Name, Strings.BandedWaveFormStreamPositionConfiguration_Smoothing_High, StringComparison.OrdinalIgnoreCase))
                {
                    this.Smoothing.Value = BandedWaveFormStreamPositionConfiguration.SMOOTHING_MAX;
                }
            }
            else if (string.Equals(this.Mode.Name, component.Path))
            {
                this.Mode.Value = this.Mode.Options.FirstOrDefault(option => string.Equals(option.Id, component.Id));
            }
            else if (this.ThemeLoader.SelectColorPalette(this.ColorPalette, component))
            {
                //Nothing to do.
            }
            else if (string.Equals(component.Id, SETTINGS, StringComparison.OrdinalIgnoreCase))
            {
                return this.ShowSettings();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected override Task<bool> ShowSettings()
        {
            return this.ShowSettings(
                Strings.WaveFormStreamPositionConfiguration_Section,
                BandedWaveFormStreamPositionConfiguration.SECTION
            );
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return BandedWaveFormStreamPositionConfiguration.GetConfigurationSections();
        }
    }
}
