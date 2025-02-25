using System;

namespace FoxTunes
{
    [Serializable]
    public class ScannerStatus
    {
        public ScannerStatus(ScannerStatusType type)
        {
            this.Type = type;
        }

        public ScannerStatusType Type { get; private set; }
    }

    public enum ScannerStatusType
    {
        None = 0,
        Complete = 1,
        Error = 2
    }
}
