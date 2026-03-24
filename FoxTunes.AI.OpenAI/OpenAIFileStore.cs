using FoxTunes.Interfaces;
using OpenAI.Files;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class OpenAIFileStore : AIFileStore
    {
        public OpenAIFileStore(IAIContext context, OpenAIFileClient client)
        {
            this.Context = context;
            this.Client = client;
        }

        public IAIContext Context { get; private set; }

        public OpenAIFileClient Client { get; private set; }

        public override async Task<string> Create(Stream content, string fileName)
        {
            var id = default(string);
            {
                Logger.Write(this, LogLevel.Debug, "Uploading file \"{0}\", {1} bytes.", fileName, content.Length);
                var result = await this.Client.UploadFileAsync(content, fileName, FileUploadPurpose.Assistants).ConfigureAwait(false);
                id = result.Value.Id;
                Logger.Write(this, LogLevel.Debug, "Uploaded file: {0}", id);
            }
            {
                var result = await this.Client.GetFileAsync(id).ConfigureAwait(false);
                //TODO: Wait for file to be processed somehow?
            }
            return id;
        }

        public override async Task Delete(string fileId)
        {
            Logger.Write(this, LogLevel.Debug, "Deleting file: {0}", fileId);
            var result = await this.Client.DeleteFileAsync(fileId).ConfigureAwait(false);
            Logger.Write(this, LogLevel.Debug, "Deleted file: {0}", fileId);
            //Nothing to do.
        }
    }
}
