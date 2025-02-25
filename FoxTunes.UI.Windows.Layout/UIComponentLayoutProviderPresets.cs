using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class UIComponentLayoutProviderPresets : StandardComponent
    {
        public UIComponentLayoutProviderPresets()
        {
            Instance = this;
            this.Presets = new Dictionary<string, IList<IUIComponentLayoutProviderPreset>>(StringComparer.OrdinalIgnoreCase);
        }

        public IDictionary<string, IList<IUIComponentLayoutProviderPreset>> Presets { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            foreach (var preset in ComponentRegistry.Instance.GetComponents<IUIComponentLayoutProviderPreset>())
            {
                this.Presets.GetOrAdd(
                    preset.Category,
                    () => new List<IUIComponentLayoutProviderPreset>()
                ).Add(preset);
            }
            base.InitializeComponent(core);
        }

        public IEnumerable<IUIComponentLayoutProviderPreset> GetPresetsByCategory(string category)
        {
            var presets = default(IList<IUIComponentLayoutProviderPreset>);
            if (this.Presets.TryGetValue(category, out presets))
            {
                return presets;
            }
            return Enumerable.Empty<IUIComponentLayoutProviderPreset>();
        }

        public IUIComponentLayoutProviderPreset GetPresetById(string id, string category)
        {
            return this.GetPresetsByCategory(category).FirstOrDefault(preset => string.Equals(preset.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public IUIComponentLayoutProviderPreset GetPresetByName(string name, string category)
        {
            return this.GetPresetsByCategory(category).FirstOrDefault(preset => string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public IUIComponentLayoutProviderPreset GetActivePreset(string category)
        {
            var configuration = ComponentRegistry.Instance.GetComponent<IConfiguration>();
            var presetElement = configuration.GetElement<SelectionConfigurationElement>(
                UIComponentLayoutProviderPreset.GetSection(category),
                UIComponentLayoutProviderPreset.GetPreset(category)
            );
            if (presetElement.Value == null)
            {
                //No preset selected.
                return null;
            }
            var preset = this.GetPresetById(presetElement.Value.Id, category);
            if (preset == null)
            {
                //The selected preset was not found.
                return null;
            }
            var layoutElement = configuration.GetElement<TextConfigurationElement>(
                UIComponentLayoutProviderPreset.GetSection(category),
                UIComponentLayoutProviderPreset.GetLayout(category)
            );
            return preset;
        }

        public bool IsLoaded(IUIComponentLayoutProviderPreset preset)
        {
            var configuration = ComponentRegistry.Instance.GetComponent<IConfiguration>();
            var presetElement = configuration.GetElement<SelectionConfigurationElement>(
                UIComponentLayoutProviderPreset.GetSection(preset.Category),
                UIComponentLayoutProviderPreset.GetPreset(preset.Category)
            );
            if (presetElement.Value == null || !string.Equals(presetElement.Value.Id, preset.Id, StringComparison.OrdinalIgnoreCase))
            {
                //Preset not selected.
                return false;
            }
            return true;
        }

        public static UIComponentLayoutProviderPresets Instance { get; private set; }
    }
}
