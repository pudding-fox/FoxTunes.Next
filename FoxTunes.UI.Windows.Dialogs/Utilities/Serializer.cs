#pragma warning disable 0436
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace FoxTunes
{
    public static class Serializer
    {
        const string MetaDataEntry = "MetaDataEntry";
        const string Name = "Name";
        const string Value = "Value";

        private static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static void Save(Stream stream, IEnumerable<ToolWindowConfiguration> configs)
        {
            using (var writer = new XmlTextWriter(stream, Encoding.Default))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement(Publication.Product);
                foreach (var config in configs)
                {
                    writer.WriteStartElement(nameof(ToolWindowConfiguration));
                    writer.WriteAttributeString(nameof(ToolWindowConfiguration.Title), config.Title);
                    writer.WriteAttributeString(nameof(ToolWindowConfiguration.Left), Convert.ToString(config.Left));
                    writer.WriteAttributeString(nameof(ToolWindowConfiguration.Top), Convert.ToString(config.Top));
                    writer.WriteAttributeString(nameof(ToolWindowConfiguration.Width), Convert.ToString(config.Width));
                    writer.WriteAttributeString(nameof(ToolWindowConfiguration.Height), Convert.ToString(config.Height));
                    writer.WriteAttributeString(nameof(ToolWindowConfiguration.ShowWithMainWindow), Convert.ToString(config.ShowWithMainWindow));
                    writer.WriteAttributeString(nameof(ToolWindowConfiguration.ShowWithMiniWindow), Convert.ToString(config.ShowWithMiniWindow));
                    writer.WriteAttributeString(nameof(ToolWindowConfiguration.AlwaysOnTop), Convert.ToString(config.AlwaysOnTop));
                    SaveComponent(writer, config.Component);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        public static void Save(Stream stream, UIComponentConfiguration config)
        {
            using (var writer = new XmlTextWriter(stream, Encoding.Default))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement(Publication.Product);
                SaveComponent(writer, config);
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        public static string SaveComponent(UIComponentConfiguration config)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new XmlTextWriter(stream, Encoding.Default))
                {
                    writer.Formatting = Formatting.Indented;
                    SaveComponent(writer, config);
                    writer.Flush();
                    stream.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        private static void SaveComponent(XmlTextWriter writer, UIComponentConfiguration config)
        {
            writer.WriteStartElement(nameof(UIComponentConfiguration));
            if (config != null && !config.Component.IsEmpty)
            {
                writer.WriteAttributeString(nameof(UIComponentConfiguration.Component), config.Component.Id);
                foreach (var child in config.Children)
                {
                    SaveComponent(writer, child);
                }
                foreach (var pair in config.MetaData)
                {
                    SaveMetaData(writer, pair.Key, pair.Value);
                }
            }
            writer.WriteEndElement();
        }

        private static void SaveMetaData(XmlTextWriter writer, string key, string value)
        {
            writer.WriteStartElement(nameof(Serializer.MetaDataEntry));
            writer.WriteAttributeString(nameof(Serializer.Name), key);
            writer.WriteAttributeString(nameof(Serializer.Value), value);
            writer.WriteEndElement();
        }

        public static IEnumerable<ToolWindowConfiguration> LoadWindows(Stream stream)
        {
            using (var reader = new XmlTextReader(stream))
            {
                reader.WhitespaceHandling = WhitespaceHandling.Significant;
                reader.ReadStartElement(Publication.Product);
                while (reader.IsStartElement(nameof(ToolWindowConfiguration)))
                {
                    var title = reader.GetAttribute(nameof(ToolWindowConfiguration.Title));
                    var left = reader.GetAttribute(nameof(ToolWindowConfiguration.Left));
                    var top = reader.GetAttribute(nameof(ToolWindowConfiguration.Top));
                    var width = reader.GetAttribute(nameof(ToolWindowConfiguration.Width));
                    var height = reader.GetAttribute(nameof(ToolWindowConfiguration.Height));
                    var showWithMainWindow = reader.GetAttribute(nameof(ToolWindowConfiguration.ShowWithMainWindow));
                    var showWithMiniWindow = reader.GetAttribute(nameof(ToolWindowConfiguration.ShowWithMiniWindow));
                    var alwaysOnTop = reader.GetAttribute(nameof(ToolWindowConfiguration.AlwaysOnTop));
                    reader.ReadStartElement(nameof(ToolWindowConfiguration));
                    yield return new ToolWindowConfiguration()
                    {
                        Title = title,
                        Left = Convert.ToInt32(left),
                        Top = Convert.ToInt32(top),
                        Width = Convert.ToInt32(width),
                        Height = Convert.ToInt32(height),
                        ShowWithMainWindow = Convert.ToBoolean(showWithMainWindow),
                        ShowWithMiniWindow = Convert.ToBoolean(showWithMiniWindow),
                        AlwaysOnTop = Convert.ToBoolean(alwaysOnTop),
                        Component = LoadComponent(reader)
                    };
                    if (reader.NodeType == XmlNodeType.EndElement && string.Equals(reader.Name, nameof(ToolWindowConfiguration)))
                    {
                        reader.ReadEndElement();
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement && string.Equals(reader.Name, Publication.Product))
                {
                    reader.ReadEndElement();
                }
            }
        }

        public static UIComponentConfiguration LoadComponent(string value)
        {
            using (var stream = new MemoryStream(Encoding.Default.GetBytes(value)))
            {
                using (var reader = new XmlTextReader(stream))
                {
                    if (reader.IsStartElement(Publication.Product))
                    {
                        reader.ReadStartElement(Publication.Product);
                    }
                    var component = LoadComponent(reader);
                    if (reader.NodeType == XmlNodeType.EndElement && string.Equals(reader.Name, Publication.Product))
                    {
                        reader.ReadEndElement();
                    }
                    return component;
                }
            }
        }

        public static UIComponentConfiguration LoadComponent(Stream stream)
        {
            var component = default(UIComponentConfiguration);
            using (var reader = new XmlTextReader(stream))
            {
                reader.WhitespaceHandling = WhitespaceHandling.Significant;
                reader.ReadStartElement(Publication.Product);
                component = LoadComponent(reader);
                if (reader.NodeType == XmlNodeType.EndElement && string.Equals(reader.Name, Publication.Product))
                {
                    reader.ReadEndElement();
                }
            }
            return component;
        }

        private static UIComponentConfiguration LoadComponent(XmlReader reader)
        {
            if (!reader.IsStartElement(nameof(UIComponentConfiguration)))
            {
                return new UIComponentConfiguration();
            }
            var component = reader.GetAttribute(nameof(UIComponentConfiguration.Component));
            var children = new List<UIComponentConfiguration>();
            var metaDatas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var isEmptyElement = reader.IsEmptyElement;
            reader.ReadStartElement(nameof(UIComponentConfiguration));
            if (!isEmptyElement)
            {
                while (reader.IsStartElement())
                {
                    if (reader.IsStartElement(nameof(UIComponentConfiguration)))
                    {
                        var child = LoadComponent(reader);
                        if (child == null)
                        {
                            continue;
                        }
                        children.Add(child);
                    }
                    else if (reader.IsStartElement(nameof(Serializer.MetaDataEntry)))
                    {
                        var metaData = LoadMetaData(reader);
                        if (EqualityComparer<KeyValuePair<string, string>>.Default.Equals(metaData, default(KeyValuePair<string, string>)))
                        {
                            continue;
                        }
                        metaDatas[metaData.Key] = metaData.Value;
                    }
                    else
                    {
                        Logger.Write(typeof(Serializer), LogLevel.Warn, "Element \"{0}\" was not recognized.", reader.Name);
                        break;
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement && string.Equals(reader.Name, nameof(UIComponentConfiguration)))
                {
                    reader.ReadEndElement();
                }
            }
            return new UIComponentConfiguration()
            {
                Component = LayoutManager.Instance.GetComponent(component) ?? UIComponent.None,
                Children = new ObservableCollection<UIComponentConfiguration>(children),
                MetaData = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(metaDatas)
            };
        }

        private static KeyValuePair<string, string> LoadMetaData(XmlReader reader)
        {
            if (!reader.IsStartElement(nameof(Serializer.MetaDataEntry)))
            {
                return default(KeyValuePair<string, string>);
            }
            var name = reader.GetAttribute(nameof(Serializer.Name));
            var value = reader.GetAttribute(nameof(Serializer.Value));
            reader.ReadStartElement(nameof(Serializer.MetaDataEntry));
            if (reader.NodeType == XmlNodeType.EndElement && string.Equals(reader.Name, nameof(Serializer.MetaDataEntry)))
            {
                reader.ReadEndElement();
            }
            return new KeyValuePair<string, string>(name, value);
        }
    }
}
