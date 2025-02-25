using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace FoxTunes
{
    public class ComponentResolver : IComponentResolver
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(AssemblyResolver).Assembly.Location);
            }
        }

        public static string FileName
        {
            get
            {
                return Path.Combine(Location, "Components.xml");
            }
        }

        public ComponentResolver()
        {
            this.Load();
        }

        public IDictionary<string, ComponentSlot> Slots { get; private set; }

        public bool Enabled
        {
            get
            {
                return Publication.IsPortable || this.HasBlockedSlots;
            }
        }

        public bool HasBlockedSlots
        {
            get
            {
                if (this.Slots == null)
                {
                    return false;
                }
                return this.Slots.Any(pair => pair.Value != null && string.Equals(pair.Value.Id, ComponentSlots.Blocked, StringComparison.OrdinalIgnoreCase));
            }
        }

        public void Load()
        {
            this.Slots = new Dictionary<string, ComponentSlot>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (File.Exists(FileName))
                {
                    Logger.Write(typeof(ComponentResolver), LogLevel.Debug, "Loading component slots from file: {0}", FileName);
                    using (var stream = File.OpenRead(FileName))
                    {
                        var pairs = Serializer.Load(stream);
                        foreach (var pair in pairs)
                        {
                            if (string.Equals(pair.Value.Id, ComponentSlots.None, StringComparison.OrdinalIgnoreCase) || string.Equals(pair.Value.Id, ComponentSlots.Blocked, StringComparison.OrdinalIgnoreCase))
                            {
                                //This was likely saved due to a bug, we should never "load" a blocked slot.
                                continue;
                            }
                            if (!this.Slots.TryAdd(pair.Key, pair.Value))
                            {
                                continue;
                            }
                            Logger.Write(typeof(ComponentResolver), LogLevel.Debug, "Loaded component slot: {0} => {1}", pair.Key, pair.Value);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Write(typeof(ComponentResolver), LogLevel.Warn, "Failed to load component slots: {0}", e.Message);
            }
            foreach (var slot in ComponentSlots.All)
            {
                if (!this.Slots.TryAdd(slot, ComponentSlot.None))
                {
                    continue;
                }
                Logger.Write(typeof(ComponentResolver), LogLevel.Debug, "Component slot {0} is not configured.", slot);
            }
        }

        public void Save()
        {
            if (!this.Enabled)
            {
                Logger.Write(typeof(ComponentResolver), LogLevel.Debug, "Cannot save component slots.");
                return;
            }
            if (!this.Slots.Any(pair => pair.Value.Flags.HasFlag(ComponentSlotFlags.Modified | ComponentSlotFlags.HasConflicts)))
            {
                Logger.Write(typeof(ComponentResolver), LogLevel.Debug, "Component slots are unmodified or have no conflicts, nothing to save.");
                return;
            }
            Logger.Write(typeof(ComponentResolver), LogLevel.Debug, "Saving component slots from file: {0}", FileName);
            try
            {
                using (var stream = File.Create(FileName))
                {
                    Serializer.Save(stream, this.Slots);
                }
            }
            catch (Exception e)
            {
                Logger.Write(typeof(ComponentResolver), LogLevel.Warn, "Failed to save component slots: {0}", e.Message);
            }
        }

        public bool Get(string slot, out string id)
        {
            var component = default(ComponentSlot);
            if (this.Slots.TryGetValue(slot, out component) && !string.Equals(component.Id, ComponentSlots.None, StringComparison.OrdinalIgnoreCase))
            {
                id = component.Id;
                return true;
            }
            else
            {
                id = ComponentSlots.None;
                return false;
            }
        }

        public void Add(string slot, string id)
        {
            var flags = ComponentSlotFlags.None;
            if (!string.Equals(id, ComponentSlots.None, StringComparison.OrdinalIgnoreCase) && !string.Equals(id, ComponentSlots.Blocked, StringComparison.OrdinalIgnoreCase))
            {
                flags |= ComponentSlotFlags.Modified;
            }
            this.Slots[slot] = new ComponentSlot(id, flags);
        }

        public void Remove(string slot)
        {
            this.Add(slot, ComponentSlots.None);
        }

        public void AddConflict(string slot)
        {
            var component = default(ComponentSlot);
            if (this.Slots.TryGetValue(slot, out component) && !string.Equals(component.Id, ComponentSlots.None, StringComparison.OrdinalIgnoreCase))
            {
                component.Flags |= ComponentSlotFlags.HasConflicts;
            }
        }

        public static readonly IComponentResolver Instance = new ComponentResolver();

        public class ComponentSlot
        {
            public ComponentSlot(string id)
            {
                this.Id = id;
            }

            public ComponentSlot(string id, ComponentSlotFlags flags) : this(id)
            {
                this.Flags = flags;
            }

            public string Id { get; private set; }

            public ComponentSlotFlags Flags { get; set; }

            public static readonly ComponentSlot None = new ComponentSlot(ComponentSlots.None);
        }

        public enum ComponentSlotFlags : byte
        {
            None,
            Modified,
            HasConflicts
        }

        public static class Serializer
        {
            const string Component = "Component";

            const string Slot = "Slot";

            const string Id = "Id";

            public static IEnumerable<KeyValuePair<string, ComponentSlot>> Load(Stream stream)
            {
                using (var reader = new XmlTextReader(stream))
                {
                    reader.ReadStartElement(Publication.Product);
                    while (reader.IsStartElement(Component))
                    {
                        var key = reader.GetAttribute(Slot);
                        var value = reader.GetAttribute(Id);
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            yield return new KeyValuePair<string, ComponentSlot>(key, new ComponentSlot(value));
                        }
                        if (reader.NodeType == XmlNodeType.EndElement && string.Equals(reader.Name, Component))
                        {
                            reader.ReadEndElement();
                        }
                        else
                        {
                            reader.Read();
                        }
                    }
                    if (reader.NodeType == XmlNodeType.EndElement && string.Equals(reader.Name, Publication.Product))
                    {
                        reader.ReadEndElement();
                    }
                    else
                    {
                        reader.Read();
                    }
                }
            }

            public static void Save(Stream stream, IEnumerable<KeyValuePair<string, ComponentSlot>> sequence)
            {
                using (var writer = new XmlTextWriter(stream, Encoding.Default))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement(Publication.Product);
                    foreach (var pair in sequence)
                    {
                        if (!pair.Value.Flags.HasFlag(ComponentSlotFlags.HasConflicts))
                        {
                            continue;
                        }
                        writer.WriteStartElement(Component);
                        writer.WriteAttributeString(Slot, pair.Key);
                        writer.WriteAttributeString(Id, pair.Value.Id);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
        }
    }
}
