using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class ScriptMetaDataProvider : StandardComponent, IMetaDataProvider, IDisposable
    {
        public IScriptingRuntime ScriptingRuntime { get; private set; }

#if NET40
        private TrackingThreadLocal<IScriptingContext> Contexts { get; set; }
#else
        private ThreadLocal<IScriptingContext> Contexts { get; set; }
#endif

        public override void InitializeComponent(ICore core)
        {
            this.ScriptingRuntime = core.Components.ScriptingRuntime;
#if NET40
            this.Contexts = new TrackingThreadLocal<IScriptingContext>();
#else
            this.Contexts = new ThreadLocal<IScriptingContext>(true);
#endif
            base.InitializeComponent(core);
        }

        public MetaDataProviderType Type
        {
            get
            {
                return MetaDataProviderType.Script;
            }
        }

        public bool AddOrUpdate(string fileName, IList<MetaDataItem> metaDataItems, MetaDataProvider provider)
        {
            var item = new FileData(fileName, metaDataItems);
            var runner = new ScriptRunner(
                this.GetOrAddContext(),
                item,
                provider.Script
            );
            runner.Prepare();
            var value = Convert.ToString(runner.Run());
            return this.AddOrUpdate(metaDataItems, provider.Name, value);
        }

        public bool AddOrUpdate(IFileAbstraction fileAbstraction, IList<MetaDataItem> metaDataItems, MetaDataProvider provider)
        {
            var item = new FileData(fileAbstraction.FileName, metaDataItems);
            var runner = new ScriptRunner(
                this.GetOrAddContext(),
                item,
                provider.Script
            );
            runner.Prepare();
            var value = Convert.ToString(runner.Run());
            return this.AddOrUpdate(metaDataItems, provider.Name, value);
        }

        protected virtual bool AddOrUpdate(IList<MetaDataItem> metaDataItems, string name, string value)
        {
            foreach (var metaDataItem in metaDataItems)
            {
                if (string.Equals(metaDataItem.Name, name, StringComparison.OrdinalIgnoreCase) && metaDataItem.Type == MetaDataItemType.CustomTag)
                {
                    if (string.Equals(metaDataItem.Value, value))
                    {
                        return false;
                    }
                    metaDataItem.Value = value;
                    return true;
                }
            }
            metaDataItems.Add(new MetaDataItem(name, MetaDataItemType.CustomTag)
            {
                Value = value
            });
            return true;
        }

        private IScriptingContext GetOrAddContext()
        {
            if (this.Contexts.IsValueCreated)
            {
                return this.Contexts.Value;
            }
            return this.Contexts.Value = this.ScriptingRuntime.CreateContext();
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
            if (this.Contexts != null)
            {
                foreach (var context in this.Contexts.Values)
                {
                    context.Dispose();
                }
                this.Contexts.Dispose();
            }
        }

        ~ScriptMetaDataProvider()
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

        private class FileData : PersistableComponent, IFileData
        {
            public FileData(string fileName, IList<MetaDataItem> metaDatas)
            {
                this.DirectoryName = Path.GetDirectoryName(fileName);
                this.FileName = fileName;
                this.MetaDatas = metaDatas;
            }

            public string DirectoryName { get; set; }

            public string FileName { get; set; }

            public IList<MetaDataItem> MetaDatas { get; set; }

        }

        private class ScriptRunner : ScriptRunner<FileData>
        {
            public ScriptRunner(IScriptingContext scriptingContext, FileData item, string script) : base(scriptingContext, item, script)
            {
            }
        }
    }
}
