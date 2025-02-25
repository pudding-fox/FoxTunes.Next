using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Spectrum.xaml
    /// </summary>
    [UIComponent("9A696BBC-F6AB-4180-8661-353A79253AA9", role: UIComponentRole.Visualization)]
    public partial class EnhancedSpectrum : ConfigurableUIComponentBase
    {
        public const string CATEGORY = "13FD94CF-9FBF-460F-9501-571BA7144DCF";

        public EnhancedSpectrum()
        {
            this.InitializeComponent();
        }

        public ThemeLoader ThemeLoader { get; private set; }

        public SelectionConfigurationElement Bands { get; private set; }

        public BooleanConfigurationElement Peak { get; private set; }

        public BooleanConfigurationElement Rms { get; private set; }

        public TextConfigurationElement ColorPalette { get; private set; }

        public IntegerConfigurationElement Duration { get; private set; }

        public IntegerConfigurationElement Interval { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.ThemeLoader = ComponentRegistry.Instance.GetComponent<ThemeLoader>();
            base.InitializeComponent(core);
        }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Bands = this.Configuration.GetElement<SelectionConfigurationElement>(
                   EnhancedSpectrumConfiguration.SECTION,
                   EnhancedSpectrumConfiguration.BANDS_ELEMENT
               );
                this.Peak = this.Configuration.GetElement<BooleanConfigurationElement>(
                    EnhancedSpectrumConfiguration.SECTION,
                    EnhancedSpectrumConfiguration.PEAK_ELEMENT
                );
                this.Rms = this.Configuration.GetElement<BooleanConfigurationElement>(
                    EnhancedSpectrumConfiguration.SECTION,
                    EnhancedSpectrumConfiguration.RMS_ELEMENT
                );
                this.ColorPalette = this.Configuration.GetElement<TextConfigurationElement>(
                    EnhancedSpectrumConfiguration.SECTION,
                    EnhancedSpectrumConfiguration.COLOR_PALETTE_ELEMENT
                );
                this.Duration = this.Configuration.GetElement<IntegerConfigurationElement>(
                    EnhancedSpectrumConfiguration.SECTION,
                    EnhancedSpectrumConfiguration.DURATION_ELEMENT
                );
                this.Interval = this.Configuration.GetElement<IntegerConfigurationElement>(
                    VisualizationBehaviourConfiguration.SECTION,
                    VisualizationBehaviourConfiguration.INTERVAL_ELEMENT
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
                foreach (var option in this.Bands.Options)
                {
                    var count = default(int);
                    var parts = option.Name.Split(new string[] { " " }, 1, StringSplitOptions.RemoveEmptyEntries);
                    if (!int.TryParse(parts[0], out count))
                    {
                        continue;
                    }
                    yield return new InvocationComponent(
                        CATEGORY,
                        option.Id,
                        string.Format("{0} {1}", option.Name, this.Bands.Name),
                        path: this.Bands.Name,
                        attributes: this.Bands.Value == option ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                    );
                }
                foreach (var component in this.ThemeLoader.SelectColorPalette(CATEGORY, this.ColorPalette, ColorPaletteRole.Visualization))
                {
                    yield return component;
                }
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Peak.Id,
                    this.Peak.Name,
                    attributes: this.Peak.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Rms.Id,
                    this.Rms.Name,
                    attributes: this.Rms.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Duration.Id,
                    Strings.EnhancedSpectrumConfiguration_Duration_Low,
                    path: this.Duration.Name,
                    attributes: this.Duration.Value == EnhancedSpectrumConfiguration.DURATION_MIN ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Duration.Id,
                    Strings.EnhancedSpectrumConfiguration_Duration_Medium,
                    path: this.Duration.Name,
                    attributes: this.Duration.Value == EnhancedSpectrumConfiguration.DURATION_DEFAULT ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Duration.Id,
                    Strings.EnhancedSpectrumConfiguration_Duration_High,
                    path: this.Duration.Name,
                    attributes: this.Duration.Value == EnhancedSpectrumConfiguration.DURATION_MAX ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Interval.Id,
                    Strings.Visualization_Speed_Slow,
                    path: this.Interval.Name,
                    attributes: this.Interval.Value == VisualizationBehaviourConfiguration.MAX_INTERVAL ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Interval.Id,
                    Strings.Visualization_Speed_Default,
                    path: this.Interval.Name,
                    attributes: this.Interval.Value == VisualizationBehaviourConfiguration.DEFAULT_INTERVAL ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Interval.Id,
                    Strings.Visualization_Speed_Fast,
                    path: this.Interval.Name,
                    attributes: this.Interval.Value == VisualizationBehaviourConfiguration.MIN_INTERVAL ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                foreach (var invocationComponent in base.Invocations)
                {
                    yield return invocationComponent;
                }
            }
        }

        public override Task InvokeAsync(IInvocationComponent component)
        {
            if (string.Equals(component.Id, this.Peak.Id, StringComparison.OrdinalIgnoreCase))
            {
                this.Peak.Toggle();
            }
            else if (string.Equals(component.Id, this.Rms.Id, StringComparison.OrdinalIgnoreCase))
            {
                this.Rms.Toggle();
            }
            else if (this.ThemeLoader.SelectColorPalette(this.ColorPalette, component))
            {
                //Nothing to do.
            }
            else if (string.Equals(this.Duration.Name, component.Path))
            {
                if (string.Equals(component.Name, Strings.EnhancedSpectrumConfiguration_Duration_Low, StringComparison.OrdinalIgnoreCase))
                {
                    this.Duration.Value = EnhancedSpectrumConfiguration.DURATION_MIN;
                }
                else if (string.Equals(component.Name, Strings.EnhancedSpectrumConfiguration_Duration_High, StringComparison.OrdinalIgnoreCase))
                {
                    this.Duration.Value = EnhancedSpectrumConfiguration.DURATION_MAX;
                }
                else
                {
                    this.Duration.Value = EnhancedSpectrumConfiguration.DURATION_DEFAULT;
                }
            }
            else if (string.Equals(this.Interval.Name, component.Path))
            {
                if (string.Equals(component.Name, Strings.Visualization_Speed_Slow, StringComparison.OrdinalIgnoreCase))
                {
                    this.Interval.Value = VisualizationBehaviourConfiguration.MAX_INTERVAL;
                }
                else if (string.Equals(component.Name, Strings.Visualization_Speed_Fast, StringComparison.OrdinalIgnoreCase))
                {
                    this.Interval.Value = VisualizationBehaviourConfiguration.MIN_INTERVAL;
                }
                else
                {
                    this.Interval.Value = VisualizationBehaviourConfiguration.DEFAULT_INTERVAL;
                }
            }
            else if (string.Equals(component.Id, SETTINGS, StringComparison.OrdinalIgnoreCase))
            {
                return this.ShowSettings();
            }
            else
            {
                var bands = this.Bands.Options.FirstOrDefault(option => string.Equals(option.Id, component.Id, StringComparison.OrdinalIgnoreCase));
                if (bands != null)
                {
                    this.Bands.Value = bands;
                }
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
                Strings.EnhancedSpectrumConfiguration_Section,
                EnhancedSpectrumConfiguration.SECTION
            );
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return EnhancedSpectrumConfiguration.GetConfigurationSections();
        }
    }
}