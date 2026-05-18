using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class DiscogsFetchArtistTask : BackgroundTask
    {
        public const string ID = "B9C1E218-4485-429D-9F6D-559E5106E660";

        public DiscogsFetchArtistTask(IEnumerable<LibraryHierarchyNode> libraryHierarchyNodes) : base(ID)
        {
            this.LibraryHierarchyNodes = libraryHierarchyNodes;
        }

        public IEnumerable<LibraryHierarchyNode> LibraryHierarchyNodes { get; private set; }

        public override bool Visible
        {
            get
            {
                return true;
            }
        }

        public override bool Cancellable
        {
            get
            {
                return true;
            }
        }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IOnDemandMetaDataProvider OnDemandMetaDataProvider { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.OnDemandMetaDataProvider = core.Components.OnDemandMetaDataProvider;
            base.InitializeComponent(core);
        }

        protected override async Task OnRun()
        {
            this.Name = "Fetching artist images..";
            this.Count = this.LibraryHierarchyNodes.Count();
            foreach (var libraryHierarchyNode in this.LibraryHierarchyNodes)
            {
                if (this.IsCancellationRequested)
                {
                    return;
                }
                if (!libraryHierarchyNode.LibraryHierarchyLevelId.HasValue)
                {
                    continue;
                }
                this.Description = libraryHierarchyNode.Value;
                var libraryHierarchyLevel = this.LibraryHierarchyBrowser.GetLevel(libraryHierarchyNode.LibraryHierarchyLevelId.Value);
                if (libraryHierarchyLevel != null)
                {
                    switch (libraryHierarchyLevel.Hints)
                    {
                        case LibraryHierarchyLevelHints.Artist:
                            var skip = new[]
                            {
                                "No Artist",
                                "Various Artists"
                            };
                            if (!skip.Any(_skip => string.Equals(libraryHierarchyNode.Value, _skip, StringComparison.OrdinalIgnoreCase)))
                            {
                                var libraryItems = this.LibraryHierarchyBrowser.GetItems(libraryHierarchyNode);
                                if (!libraryItems.Any())
                                {
                                    continue;
                                }
                                await this.OnDemandMetaDataProvider.GetMetaData(
                                    libraryItems,
                                    new OnDemandMetaDataRequest(
                                        CommonImageTypes.Artist,
                                        MetaDataItemType.Image,
                                        MetaDataUpdateType.System
                                    )
                                ).ConfigureAwait(false);
                            }
                            await Task.Delay(100).ConfigureAwait(false);
                            break;
                    }
                }
                this.Position++;
            }
        }
    }
}
