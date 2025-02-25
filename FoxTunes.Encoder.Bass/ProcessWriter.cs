using System.Diagnostics;

namespace FoxTunes
{
    public class ProcessWriter
    {
        public ProcessWriter(Process process)
        {
            this.Process = process;
        }

        public Process Process { get; }

        public void Write(byte[] buffer, int length)
        {
            this.Process.StandardInput.BaseStream.Write(buffer, 0, length);
        }

        public void Close()
        {
            this.Process.StandardInput.Close();
        }
    }
}
