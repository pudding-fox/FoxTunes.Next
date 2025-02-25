using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class RendererTargetFactory : StandardFactory, IConfigurableComponent
    {
        public IEnumerable<IRendererTargetBehaviour> Backends { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public SelectionConfigurationElement Backend { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Backends = ComponentRegistry.Instance.GetComponents<IRendererTargetBehaviour>().ToArray();
            this.Configuration = core.Components.Configuration;
            this.Backend = this.Configuration.GetElement<SelectionConfigurationElement>(
                RendererTargetFactoryConfiguration.SECTION,
                RendererTargetFactoryConfiguration.BACKEND
            );
            base.InitializeComponent(core);
        }

        public RendererTarget Create(int width, int height)
        {
            foreach (var backend in this.Backends)
            {
                if (string.Equals(backend.Id, this.Backend.Value.Id, StringComparison.OrdinalIgnoreCase))
                {
                    return backend.Create(width, height);
                }
            }
            return new WriteableBitmapRendererTarget(width, height);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return RendererTargetFactoryConfiguration.GetConfigurationSections();
        }
    }
}
