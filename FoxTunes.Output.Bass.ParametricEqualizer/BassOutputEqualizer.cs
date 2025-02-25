using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class BassOutputEqualizer : BassOutputEffect, IOutputEqualizer, IStandardComponent, IInvocableComponent, IDisposable
    {
        const string ENABLED = "AAAA";

        const string SAVE = "ZZZZ";

        public override bool Enabled
        {
            get
            {
                return this.EnabledElement.Value;
            }
            set
            {
                this.EnabledElement.Value = value;
            }
        }

        public IEnumerable<IOutputEqualizerBand> Bands { get; private set; }

        public IEnumerable<string> Presets
        {
            get
            {
                return BassParametricEqualizerPreset.Presets;
            }
        }

        protected virtual void OnPresetsChanged()
        {
            this.OnPropertyChanged("Presets");
            if (this.PresetsChanged != null)
            {
                this.PresetsChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler PresetsChanged;

        public string Preset
        {
            get
            {
                return BassParametricEqualizerPreset.Preset;
            }
            set
            {
                BassParametricEqualizerPreset.Preset = value;
                this.OnPresetChanged();
            }
        }

        protected virtual void OnPresetChanged()
        {
            this.OnPropertyChanged("Preset");
            if (this.PresetChanged != null)
            {
                this.PresetChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler PresetChanged;

        public ICore Core { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        public BooleanConfigurationElement EnabledElement { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.Core = core;
            this.UserInterface = core.Components.UserInterface;
            this.EnabledElement = this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassParametricEqualizerStreamComponentConfiguration.ENABLED
            );
            this.EnabledElement.ValueChanged += this.OnEnabledChanged;
            this.Bands = new List<IOutputEqualizerBand>(this.GetBands());
        }

        protected virtual void OnEnabledChanged(object sender, EventArgs e)
        {
            this.OnEnabledChanged();
        }

        protected virtual IEnumerable<IOutputEqualizerBand> GetBands()
        {
            var bands = BassParametricEqualizerStreamComponentConfiguration.Bands.ToArray();
            for (var position = 0; position < bands.Length; position++)
            {
                var band = new BassOutputEqualizerBand(bands[position].Key, position, bands[position].Value);
                band.InitializeComponent(this.Core);
                yield return band;
            }
        }

        public void SavePreset()
        {
            var name = this.UserInterface.Prompt(Strings.BassOutputEqualizer_SaveAs);
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            this.SavePreset(name);
        }

        public void SavePreset(string name)
        {
            BassParametricEqualizerPreset.SavePreset(name);
            this.OnPresetsChanged();
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_EQUALIZER;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_EQUALIZER,
                    ENABLED,
                    Strings.BassOutputEqualizer_Enabled,
                    attributes: this.Enabled ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
                var first = true;
                var active = this.Preset;
                var position = 0;
                foreach (var preset in this.Presets)
                {
                    var attributes = InvocationComponent.ATTRIBUTE_NONE;
                    if (string.Equals(preset, active, StringComparison.OrdinalIgnoreCase))
                    {
                        attributes |= InvocationComponent.ATTRIBUTE_SELECTED;
                    }
                    if (first)
                    {
                        attributes |= InvocationComponent.ATTRIBUTE_SEPARATOR;
                        first = false;
                    }
                    yield return new InvocationComponent(
                        InvocationComponent.CATEGORY_EQUALIZER,
                        string.Format("BBB{0}", position++),
                        preset,
                        attributes: attributes
                    );
                }
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_EQUALIZER,
                    SAVE,
                    Strings.BassOutputEqualizer_Save,
                    attributes: InvocationComponent.ATTRIBUTE_SEPARATOR
                );
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            if (string.Equals(component.Id, ENABLED))
            {
                this.Enabled = !this.Enabled;
            }
            else if (string.Equals(component.Id, SAVE))
            {
                this.SavePreset();
            }
            else
            {
                this.Preset = component.Name;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.EnabledElement != null)
            {
                this.EnabledElement.ValueChanged -= this.OnEnabledChanged;
            }
            if (this.Bands != null)
            {
                foreach (var band in this.Bands.OfType<IDisposable>())
                {
                    band.Dispose();
                }
            }
        }

        ~BassOutputEqualizer()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
