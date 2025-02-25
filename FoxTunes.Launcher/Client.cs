using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class Client
    {
        public async Task Send(string message)
        {
            using (var client = new NamedPipeClientStream(Server.Id))
            {
                using (var writer = new StreamWriter(client))
                {
#if NET40
                    client.Connect();
#else
                    await client.ConnectAsync().ConfigureAwait(false);
#endif
                    await writer.WriteLineAsync(message).ConfigureAwait(false);
                }
            }
        }
    }
}
