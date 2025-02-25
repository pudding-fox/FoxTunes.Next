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
    [UIComponent("381328C3-C2CE-4FDA-AC92-71A15C3FC387", role: UIComponentRole.Visualization)]
    [UIComponentToolbar(300, UIComponentToolbarAlignment.Left, true)]
    public partial class Spectrum : ConfigurableUIComponentBase, IInvocableComponent
    {
        public const string CATEGORY = "3DF40656-FDD5-4B98-A25C-66DDFFD66CA0";

        public Spectrum()
        {
            this.InitializeComponent();
        }

        public ThemeLoader ThemeLoader { get; private set; }

        public SelectionConfigurationElement Bars { get; private set; }

        public BooleanConfigurationElement Peaks { get; private set; }

        public IntegerConfigurationElement CutOff { get; private set; }

        public TextConfigurationElement ColorPalette { get; private set; }

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
                this.Bars = this.Configuration.GetElement<SelectionConfigurationElement>(
                    SpectrumConfiguration.SECTION,
                    SpectrumConfiguration.BARS_ELEMENT
                );
                this.Peaks = this.Configuration.GetElement<BooleanConfigurationElement>(
                    SpectrumConfiguration.SECTION,
                    SpectrumConfiguration.PEAKS_ELEMENT
                );
                this.CutOff = this.Configuration.GetElement<IntegerConfigurationElement>(
                    SpectrumConfiguration.SECTION,
                    SpectrumConfiguration.CUT_OFF_ELEMENT
                );
                this.ColorPalette = this.Configuration.GetElement<TextConfigurationElement>(
                    SpectrumConfiguration.SECTION,
                    SpectrumConfiguration.COLOR_PALETTE_ELEMENT
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
                foreach (var option in this.Bars.Options)
                {
                    var count = default(int);
                    if (int.TryParse(option.Name, out count))
                    {
                        if (count > 128)
                        {
                            //Don't show really high counts on this menu, they need additional configuration.
                            break;
                        }
                    }
                    yield return new InvocationComponent(
                        CATEGORY,
                        option.Id,
                        string.Format("{0} {1}", option.Name, this.Bars.Name),
                        path: this.Bars.Name,
                        attributes: this.Bars.Value == option ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                    );
                }
                foreach (var component in this.ThemeLoader.SelectColorPalette(CATEGORY, this.ColorPalette, ColorPaletteRole.Visualization))
                {
                    yield return component;
                }
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Peaks.Id,
                    this.Peaks.Name,
                    attributes: this.Peaks.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
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
            if (string.Equals(component.Id, this.Peaks.Id, StringComparison.OrdinalIgnoreCase))
            {
                this.Peaks.Toggle();
            }
            else if (this.ThemeLoader.SelectColorPalette(this.ColorPalette, component))
            {
                //Nothing to do.
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
                var bars = this.Bars.Options.FirstOrDefault(option => string.Equals(option.Id, component.Id, StringComparison.OrdinalIgnoreCase));
                if (bars != null)
                {
                    this.Bars.Value = bars;
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
                Strings.SpectrumConfiguration_Section,
                SpectrumConfiguration.SECTION
            );
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return SpectrumConfiguration.GetConfigurationSections();
        }
    }
}