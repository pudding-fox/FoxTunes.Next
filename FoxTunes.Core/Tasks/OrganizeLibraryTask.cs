using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class OrganizeLibraryTask : LibraryTaskBase
    {
        public OrganizeLibraryTask(LibraryHierarchy libraryHierarchy, string root)
        {
            this.LibraryHierarchy = libraryHierarchy;
            this.Root = root;
        }

        public LibraryHierarchy LibraryHierarchy { get; private set; }

        public string Root { get; private set; }

        public IScriptingRuntime ScriptingRuntime { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.ScriptingRuntime = core.Components.ScriptingRuntime;
            base.InitializeComponent(core);
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

        protected override Task OnStarted()
        {
            this.Name = "Organizing library";
            return base.OnStarted();
        }

        protected override async Task OnRun()
        {
            Logger.Write(this, LogLevel.Debug, "Organizing library using hierarchy \"{0}\"", this.LibraryHierarchy.Name);
            var roots = await this.GetRoots().ConfigureAwait(false);
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                var set = this.Database.Set<LibraryItem>(transaction);
                this.Count = set.Count;
                using (var scriptingContext = this.ScriptingRuntime.CreateContext())
                {
                    foreach (var libraryItem in set)
                    {
                        if (this.CancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        Logger.Write(this, LogLevel.Debug, "Organizing library item \"{0}\"", libraryItem.FileName);
                        this.Description = Path.GetFileName(libraryItem.FileName);
                        var sourceFileName = libraryItem.FileName;
                        var directoryName = Path.GetDirectoryName(sourceFileName);
                        var root = this.GetRoot(roots, sourceFileName);
                        if (string.IsNullOrEmpty(root))
                        {
                            Logger.Write(this, LogLevel.Warn, "Cannot organize library item \"{0}\", no root was found.", libraryItem.FileName);
                            continue;
                        }
                        Logger.Write(this, LogLevel.Debug, "Library item root \"{0}\"", root);
                        var destinationFileName = this.GetFileName(scriptingContext, libraryItem);
                        Logger.Write(this, LogLevel.Debug, "Library item destination \"{0}\"", destinationFileName);
                        try
                        {
                            this.Move(sourceFileName, destinationFileName);
                            this.Cleanup(root, directoryName);
                        }
                        catch
                        {
                            continue;
                        }
                        libraryItem.FileName = destinationFileName;
                        await set.AddOrUpdateAsync(libraryItem).ConfigureAwait(false);
                        this.Position++;
                    }
                    if (transaction.HasTransaction)
                    {
                        transaction.Commit();
                    }
                }
            }
        }

        protected virtual string GetRoot(IEnumerable<string> roots, string fileName)
        {
            var matching = roots.Where(_root => fileName.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
            var root = matching.OrderByDescending(path => path.Length);
            return root.FirstOrDefault();
        }

        protected virtual string GetFileName(IScriptingContext scriptingContext, LibraryItem libraryItem)
        {
            var invalidFileNameChars = Path.GetInvalidFileNameChars().Concat(new[] { '.' /*Dots are technically valid in a path but seem to cause many problems and there's no way to "escape" them. Just replace them for now.*/ });
            var builder = new StringBuilder();
            builder.Append(this.Root);
            foreach (var libraryHierarchyLevel in this.LibraryHierarchy.Levels)
            {
                var runner = new LibraryItemScriptRunner(
                    scriptingContext,
                    libraryItem,
                    libraryHierarchyLevel.Script
                );
                runner.Prepare();
                var value = Convert.ToString(runner.Run());
                var sanitized = string.Concat(
                    value.Select(c => invalidFileNameChars.Contains(c) ? '_' : c)
                );
                builder.Append(Path.DirectorySeparatorChar);
                builder.Append(sanitized);
            }
            builder.Append(Path.GetExtension(libraryItem.FileName));
            var fileName = builder.ToString();
            return fileName;
        }

        protected virtual void Move(string sourceFileName, string destinationFileName)
        {
            try
            {
                if (string.Equals(sourceFileName, destinationFileName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Write(this, LogLevel.Debug, "Source and destination are the same, skipping.");
                    return;
                }
                if (!File.Exists(sourceFileName))
                {
                    Logger.Write(this, LogLevel.Debug, "Source file \"{0}\" does not exists.", sourceFileName);
                    return;
                }
                if (File.Exists(destinationFileName))
                {
                    Logger.Write(this, LogLevel.Debug, "Destination file \"{0}\" already exists.", sourceFileName);
                    return;
                }
                var directoryName = Path.GetDirectoryName(destinationFileName);
                if (!Directory.Exists(directoryName))
                {
                    Logger.Write(this, LogLevel.Debug, "Creating directory \"{0}\"", directoryName);
                    Directory.CreateDirectory(directoryName);
                }
                if (!File.Exists(destinationFileName))
                {
                    Logger.Write(this, LogLevel.Debug, "Moving file from \"{0}\" to \"{1}\"", sourceFileName, destinationFileName);
                    File.Move(sourceFileName, destinationFileName);
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to move file from \"{0}\" to \"{1}\": {2}", sourceFileName, destinationFileName, e.Message);
                throw;
            }
        }

        protected virtual void Cleanup(string root, string directoryName)
        {
            try
            {
                while (!string.Equals(root, directoryName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!Directory.Exists(directoryName))
                    {
                        return;
                    }
                    if (Directory.GetDirectories(directoryName).Length == 0 && Directory.GetFiles(directoryName).Length == 0)
                    {
                        Logger.Write(this, LogLevel.Debug, "Deleting directory \"{0}\"", directoryName);
                        Directory.Delete(directoryName);
                    }
                    directoryName = Path.GetDirectoryName(directoryName);
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to cleanup directory \"{0}\": {1}", directoryName, e.Message);
                throw;
            }
        }
    }
}
