using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class AddPathsToLibraryTask : LibraryTaskBase
    {
        public AddPathsToLibraryTask(IEnumerable<string> paths)
            : base()
        {
            this.Paths = paths;
        }

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

        public IEnumerable<string> Roots
        {
            get
            {
                var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var path in this.Paths)
                {
                    if (Directory.Exists(path))
                    {
                        roots.Add(path);
                    }
                    else if (File.Exists(path))
                    {
                        roots.Add(Path.GetDirectoryName(path));
                    }
                }
                return roots;
            }
        }

        public IEnumerable<string> Paths { get; private set; }

        protected override Task OnStarted()
        {
            this.Name = "Getting file list";
            return base.OnStarted();
        }

        protected override async Task OnRun()
        {
            await this.AddRoots(this.Roots).ConfigureAwait(false);
            await this.AddPaths(this.Paths).ConfigureAwait(false);
        }

        protected override async Task OnCompleted()
        {
            await base.OnCompleted().ConfigureAwait(false);
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.LibraryUpdated)).ConfigureAwait(false);
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.HierarchiesUpdated)).ConfigureAwait(false);
        }
    }
}
