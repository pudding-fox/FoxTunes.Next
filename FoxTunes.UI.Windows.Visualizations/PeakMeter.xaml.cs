using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for PeakMeter.xaml
    /// </summary>
    [UIComponent("F8231616-9D5E-45C8-BD72-506FC5FC9C95", role: UIComponentRole.Visualization)]
    [UIComponentToolbar(500, UIComponentToolbarAlignment.Left, false)]
    public partial class PeakMeter : ConfigurableUIComponentBase
    {
        public const string CATEGORY = "F58AE444-E7F1-4D3D-9CE6-D1612892CF28";

        public PeakMeter()
        {
            this.InitializeComponent();
        }

        public ThemeLoader ThemeLoader { get; private set; }

        public BooleanConfigurationElement Peaks { get; private set; }

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
                this.Peaks = this.Configuration.GetElement<BooleanConfigurationElement>(
                    PeakMeterConfiguration.SECTION,
                    PeakMeterConfiguration.PEAKS_ELEMENT
                );
                this.ColorPalette = this.Configuration.GetElement<TextConfigurationElement>(
                    PeakMeterConfiguration.SECTION,
                    PeakMeterConfiguration.COLOR_PALETTE
                );
                this.Duration = this.Configuration.GetElement<IntegerConfigurationElement>(
                    PeakMeterConfiguration.SECTION,
                    PeakMeterConfiguration.DURATION
                );
                this.Interval = this.Configuration.GetElement<IntegerConfigurationElement>(
                    VisualizationBehaviourConfiguration.SECTION,
                    VisualizationBehaviourConfiguration.INTERVAL_ELEMENT
                );
            }
            base.OnConfigurationChanged();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            var size = sizeInfo.NewSize;
            if (!double.IsNaN(size.Width) && !double.IsNaN(size.Height) && size.Width > 0 && size.Height > 0)
            {
                var ratio = size.Width / size.Height;
                var orientation = default(Orientation);
                if (ratio > 1)
                {
                    orientation = Orientation.Horizontal;
                    this.MinWidth = 160;
                    this.MinHeight = 30;
                }
                else
                {
                    orientation = Orientation.Vertical;
                    this.MinWidth = 30;
                    this.MinHeight = 160;
                }
                var viewModel = this.FindResource<global::FoxTunes.ViewModel.PeakMeter>("ViewModel");
                if (viewModel != null && viewModel.Orientation != orientation)
                {
                    viewModel.Orientation = orientation;
                }
            }
            base.OnRenderSizeChanged(sizeInfo);
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
                    this.Duration.Id,
                    Strings.PeakMeterConfiguration_Duration_Low,
                    path: this.Duration.Name,
                    attributes: this.Duration.Value == PeakMeterConfiguration.DURATION_MIN ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Duration.Id,
                    Strings.PeakMeterConfiguration_Duration_Medium,
                    path: this.Duration.Name,
                    attributes: this.Duration.Value == PeakMeterConfiguration.DURATION_DEFAULT ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
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
                yield return new InvocationComponent(
                    CATEGORY,
                    this.Duration.Id,
                    Strings.PeakMeterConfiguration_Duration_High,
                    path: this.Duration.Name,
                    attributes: this.Duration.Value == PeakMeterConfiguration.DURATION_MAX ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
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
            else if (string.Equals(this.Duration.Name, component.Path))
            {
                if (string.Equals(component.Name, Strings.PeakMeterConfiguration_Duration_Low, StringComparison.OrdinalIgnoreCase))
                {
                    this.Duration.Value = PeakMeterConfiguration.DURATION_MIN;
                }
                else if (string.Equals(component.Name, Strings.PeakMeterConfiguration_Duration_High, StringComparison.OrdinalIgnoreCase))
                {
                    this.Duration.Value = PeakMeterConfiguration.DURATION_MAX;
                }
                else
                {
                    this.Duration.Value = PeakMeterConfiguration.DURATION_DEFAULT;
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
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected override Task<bool> ShowSettings()
        {
            return this.ShowSettings(
                Strings.PeakMeterConfiguration_Section,
                PeakMeterConfiguration.SECTION
            );
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return PeakMeterConfiguration.GetConfigurationSections();
        }
    }
}