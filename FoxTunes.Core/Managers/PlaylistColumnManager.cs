using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class PlaylistColumnManager : StandardManager, IPlaylistColumnManager
    {
        public PlaylistColumnManager()
        {
            this._Providers = new Lazy<IDictionary<string, IPlaylistColumnProvider>>(
                () => ComponentRegistry.Instance.GetComponents<IPlaylistColumnProvider>().ToDictionary(
                    component => component.Id
                )
            );
        }

        public Lazy<IDictionary<string, IPlaylistColumnProvider>> _Providers { get; private set; }

        public IEnumerable<IPlaylistColumnProvider> Providers
        {
            get
            {
                return this._Providers.Value.Values;
            }
        }

        public IPlaylistColumnProvider GetProvider(string plugin)
        {
            var provider = default(IPlaylistColumnProvider);
            if (!this._Providers.Value.TryGetValue(plugin, out provider))
            {
                return null;
            }
            return provider;
        }
    }
}
