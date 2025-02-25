using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    public class CoreSetup : ICoreSetup
    {
        public CoreSetup()
        {
            this.Slots = new HashSet<string>(ComponentSlots.All);
        }

        public HashSet<string> Slots { get; private set; }

        IEnumerable<string> ICoreSetup.Slots
        {
            get
            {
                return this.Slots;
            }
        }

        public bool HasSlot(string slot)
        {
            return this.Slots.Contains(slot);
        }

        public void Enable(IEnumerable<string> slots)
        {
            foreach (var slot in slots)
            {
                this.Enable(slot);
            }
        }

        public void Enable(string slot)
        {
            this.Slots.Add(slot);
        }

        public void Disable(IEnumerable<string> slots)
        {
            foreach (var slot in slots)
            {
                this.Disable(slot);
            }
        }

        public void Disable(string slot)
        {
            this.Slots.Remove(slot);
        }

        public static ICoreSetup Default
        {
            get
            {
                return new CoreSetup();
            }
        }
    }
}
