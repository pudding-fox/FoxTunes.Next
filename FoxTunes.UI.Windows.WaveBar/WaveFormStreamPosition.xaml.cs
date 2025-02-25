using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for WaveFormStreamPosition.xaml
    /// </summary>
    [UIComponent("1CABFBE6-C5BD-4818-A092-2D79509D3A52", role: UIComponentRole.Playback)]
    [UIComponentToolbar(800, UIComponentToolbarAlignment.Stretch, false)]
    public partial class WaveFormStreamPosition : ConfigurableUIComponentBase
    {
        public const string CATEGORY = "0E698392-FF2C-415A-BB6E-754604DFAB57";

        public WaveFormStreamPosition()
        {
            this.InitializeComponent();
        }

        public ThemeLoader ThemeLoader { get; private set; }

        public SelectionConfigurationElement Mode { get; private set; }

        public BooleanConfigurationElement Rms { get; private set; }

        public BooleanConfigurationElement Logarithmic { get; private set; }

        public IntegerConfigurationElement Smoothing { get; private set; }

        public TextConfigurationElement ColorPalette { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.ThemeLoader = ComponentRegistry.Instance.GetComponent<ThemeLoader>();
            base.InitializeComponent(core);
        }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Mode = this.Configuration.GetElement<SelectionConfigurationElement>(
                    WaveFormStreamPositionConfiguration.SECTION,
                    WaveFormStreamPositionConfiguration.MODE_ELEMENT
                );
                this.Rms = this.Configuration.GetElement<BooleanConfigurationElement>(
                    WaveFormStreamPositionConfiguration.SECTION,
                    WaveFormStreamPositionConfiguration.RMS_ELEMENT
                );
                this.Logarithmic = this.Configuration.GetElement<BooleanConfigurationElement>(
                    WaveFormStreamPositionConfiguration.SECTION,
                    WaveFormStreamPositionConfiguration.DB_ELEMENT
                );
                this.Smoothing = this.Configuration.GetElement<IntegerConfigurationElement>(
                    WaveFormStreamPositionConfiguration.SECTION,
                    WaveFormStreamPositionConfiguration.SMOOTHING_ELEMENT
                );
                this.ColorPalette = this.Configuration.GetElement<TextConfigurationElement>(
                    WaveFormStreamPositionConfiguration.SECTION,
                    WaveFormStreamPositionConfiguration.COLOR_PALETTE_ELEMENT
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
                foreach (var component in this.ThemeLoader.SelectColorPalette(CATEGORY, this.ColorPalette, ColorPaletteRole.WaveForm))
                {
                    yield return component;
                }
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Rms.Id,
                    this.Rms.Name,
                    attributes: this.Rms.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Logarithmic.Id,
                    this.Logarithmic.Name,
                    attributes: this.Logarithmic.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Smoothing.Id,
                    Strings.WaveFormStreamPositionConfiguration_Smoothing_Off,
                    path: this.Smoothing.Name,
                    attributes: this.Smoothing.Value == WaveFormStreamPositionConfiguration.SMOOTHING_MIN ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Smoothing.Id,
                    Strings.WaveFormStreamPositionConfiguration_Smoothing_Low,
                    path: this.Smoothing.Name,
                    attributes: this.Smoothing.Value == WaveFormStreamPositionConfiguration.SMOOTHING_LOW ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Smoothing.Id,
                    Strings.WaveFormStreamPositionConfiguration_Smoothing_Medium,
                    path: this.Smoothing.Name,
                    attributes: this.Smoothing.Value == WaveFormStreamPositionConfiguration.SMOOTHING_MEDIUM ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Smoothing.Id,
                    Strings.WaveFormStreamPositionConfiguration_Smoothing_High,
                    path: this.Smoothing.Name,
                    attributes: this.Smoothing.Value == WaveFormStreamPositionConfiguration.SMOOTHING_MAX ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                foreach (var invocationComponent in base.Invocations)
                {
                    yield return invocationComponent;
                }
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            if (string.Equals(this.Mode.Name, component.Path))
            {
                this.Mode.Value = this.Mode.Options.FirstOrDefault(option => string.Equals(option.Id, component.Id));
            }
            else if (string.Equals(this.Rms.Name, component.Name))
            {
                this.Rms.Toggle();
            }
            else if (string.Equals(this.Logarithmic.Name, component.Name))
            {
                this.Logarithmic.Toggle();
            }
            else if (string.Equals(this.Smoothing.Name, component.Path))
            {
                if (string.Equals(component.Name, Strings.WaveFormStreamPositionConfiguration_Smoothing_Off, StringComparison.OrdinalIgnoreCase))
                {
                    this.Smoothing.Value = WaveFormStreamPositionConfiguration.SMOOTHING_MIN;
                }
                else if (string.Equals(component.Name, Strings.WaveFormStreamPositionConfiguration_Smoothing_Low, StringComparison.OrdinalIgnoreCase))
                {
                    this.Smoothing.Value = WaveFormStreamPositionConfiguration.SMOOTHING_LOW;
                }
                else if (string.Equals(component.Name, Strings.WaveFormStreamPositionConfiguration_Smoothing_Medium, StringComparison.OrdinalIgnoreCase))
                {
                    this.Smoothing.Value = WaveFormStreamPositionConfiguration.SMOOTHING_MEDIUM;
                }
                else if (string.Equals(component.Name, Strings.WaveFormStreamPositionConfiguration_Smoothing_High, StringComparison.OrdinalIgnoreCase))
                {
                    this.Smoothing.Value = WaveFormStreamPositionConfiguration.SMOOTHING_MAX;
                }
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
                WaveFormStreamPositionConfiguration.SECTION
            );
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return WaveFormStreamPositionConfiguration.GetConfigurationSections();
        }
    }
}
