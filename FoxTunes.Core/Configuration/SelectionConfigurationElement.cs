using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FoxTunes
{
    public class SelectionConfigurationElement : ConfigurationElement<SelectionConfigurationOption>
    {
        public SelectionConfigurationElement(string id, string name = null, string description = null, string path = null)
            : base(id, name, description, path)
        {
            this.Options = new List<SelectionConfigurationOption>();
        }

        public IList<SelectionConfigurationOption> Options { get; set; }

        private bool Contains(string id)
        {
            return this.GetOption(id) != null;
        }

        public SelectionConfigurationOption GetOption(string optionId)
        {
            return this.Options.FirstOrDefault(option => string.Equals(option.Id, optionId, StringComparison.OrdinalIgnoreCase));
        }

        public SelectionConfigurationElement WithOptions(IEnumerable<SelectionConfigurationOption> options)
        {
            return this.WithOptions(options, false);
        }

        public SelectionConfigurationElement WithOptions(IEnumerable<SelectionConfigurationOption> options, bool clear)
        {
            var value = this.Value;
            if (clear)
            {
                this.Options.Clear();
            }
            foreach (var option in options)
            {
                if (option.IsDefault)
                {
                    if (value != null)
                    {
                        Logger.Write(typeof(SelectionConfigurationElement), LogLevel.Debug, "New default value \"{0}\" superseeds current value \"{1}\".", option.Id, value.Id);
                    }
                    value = option;
                }
                this.Options.Add(option);
            }
            if (value != null && this.Contains(value.Id))
            {
                this.Value = value;
            }
            else
            {
                this.Value = this.Options.FirstOrDefault();
            }
            return this;
        }

        protected override void Update(ConfigurationElement<SelectionConfigurationOption> element)
        {
            if (element is SelectionConfigurationElement)
            {
                this.Update(element as SelectionConfigurationElement);
            }
        }

        protected virtual void Update(SelectionConfigurationElement element)
        {
            this.WithOptions(element.Options);
        }

        public override string GetPersistentValue()
        {
            return this.Value.Id;
        }

        public override void SetPersistentValue(string value)
        {
            var option = this.GetOption(value);
            if (option == null)
            {
                Logger.Write(typeof(SelectionConfigurationElement), LogLevel.Warn, "Cannot restore value: Option \"{0}\" no longer exists.", value);
                return;
            }
            this.Value = option;
        }
    }
}
