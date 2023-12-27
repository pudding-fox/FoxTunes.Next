using System;

namespace FoxTunes
{
    [Serializable]
    public class ScannerCommand
    {
        public ScannerCommand(ScannerCommandType type)
        {
            this.Type = type;
        }

        public ScannerCommandType Type { get; private set; }
    }

    public enum ScannerCommandType
    {
        None = 0,
        Cancel = 1,
        Quit
    }
}
