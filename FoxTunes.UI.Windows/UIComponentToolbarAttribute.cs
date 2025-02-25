using System;

namespace FoxTunes
{
    public class UIComponentToolbarAttribute : Attribute
    {
        public UIComponentToolbarAttribute(int sequence, UIComponentToolbarAlignment alignment, bool @default = false)
        {
            this.Sequence = sequence;
            this.Alignment = alignment;
            this.Default = @default;
        }

        public int Sequence { get; private set; }

        public UIComponentToolbarAlignment Alignment { get; private set; }

        public bool Default { get; private set; }
    }

    public enum UIComponentToolbarAlignment : byte
    {
        None,
        Left,
        Stretch,
        Right
    }
}
