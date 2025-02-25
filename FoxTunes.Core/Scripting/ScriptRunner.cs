using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FoxTunes
{
    public abstract class ScriptRunner<T> where T : IFileData
    {
        protected ScriptRunner(IScriptingContext scriptingContext, T item, string script)
        {
            this.ScriptingContext = scriptingContext;
            this.Item = item;
            this.Script = script;
        }

        public IScriptingContext ScriptingContext { get; private set; }

        public T Item { get; private set; }

        public string Script { get; private set; }

        public virtual void Prepare()
        {
            var tags = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var documents = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var collections = new Dictionary<MetaDataItemType, IDictionary<string, object>>()
            {
                { MetaDataItemType.Tag, tags },
                { MetaDataItemType.Property, properties },
                { MetaDataItemType.Document, documents },
                { MetaDataItemType.CustomTag, tags },
            };
            var fileName = default(string);
            var directoryName = default(string);
            if (this.Item != null && this.Item.MetaDatas != null)
            {
                lock (this.Item.MetaDatas)
                {
                    foreach (var item in this.Item.MetaDatas)
                    {
                        if (!collections.ContainsKey(item.Type))
                        {
                            continue;
                        }
                        var key = this.GetKey(item);
                        var collection = collections[item.Type];
                        if (collection.ContainsKey(key))
                        {
                            //Not sure what to do. Doesn't happen often.
                            continue;
                        }
                        collection.Add(key, this.GetValue(item));
                    }
                }
                fileName = this.Item.FileName;
                directoryName = this.Item.DirectoryName;
            }
            this.ScriptingContext.SetValue("tag", tags);
            this.ScriptingContext.SetValue("property", properties);
            this.ScriptingContext.SetValue("document", documents);
            this.ScriptingContext.SetValue("file", fileName);
            this.ScriptingContext.SetValue("folder", directoryName);
        }

        public string GetKey(MetaDataItem item)
        {
            var name = item.Name;
            switch (item.Type)
            {
                case MetaDataItemType.Document:
                    var parts = name.Split(new[] { ':' }, 2);
                    if (parts.Length != 2)
                    {
                        //Expected document:Name.
                    }
                    else
                    {
                        name = parts[1];
                    }
                    break;
            }
            return name.ToLower();
        }

        public object GetValue(MetaDataItem item)
        {
            if (string.IsNullOrEmpty(item.Value))
            {
                return string.Empty;
            }
            switch (item.Type)
            {
                case MetaDataItemType.Document:
                    var parts = item.Value.Split(new[] { ':' }, 2);
                    if (parts.Length != 2)
                    {
                        //Expected MimeType:Data.
                        return item.Value;
                    }
                    return this.GetDocument(parts[0], parts[1]);
                default:
                    return item.Value;
            }
        }

        public object GetDocument(string mimeType, string data)
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "mime", mimeType },
                { "data", data }
            };
        }

        [DebuggerNonUserCode]
        public object Run()
        {
            const string RESULT = "__result";
            try
            {
                this.ScriptingContext.Run(string.Concat("var ", RESULT, " = ", this.Script, ";"));
            }
            catch (ScriptingException e)
            {
                return e.Message;
            }
            return this.ScriptingContext.GetValue(RESULT);
        }
    }
}
