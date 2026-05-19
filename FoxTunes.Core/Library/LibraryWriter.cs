#pragma warning disable 612, 618
using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class LibraryWriter : Disposable
    {
        public LibraryWriter(IDatabaseComponent database, ITransactionSource transaction)
        {
            this.Command = CreateCommand(database, database.Queries.AddLibraryItem, transaction);
        }

        public IDatabaseCommand Command { get; private set; }

        public async Task Write(LibraryItem libraryItem)
        {
            this.Command.Parameters["directoryName"] = libraryItem.DirectoryName;
            this.Command.Parameters["fileName"] = libraryItem.FileName;
            this.Command.Parameters["importDate"] = libraryItem.ImportDate;
            this.Command.Parameters["status"] = libraryItem.Status;
            this.Command.Parameters["flags"] = libraryItem.Flags;
            var libraryItemId = Convert.ToInt32(await this.Command.ExecuteScalarAsync().ConfigureAwait(false));
            libraryItem.Id = libraryItemId;
        }

        protected override void OnDisposing()
        {
            this.Command.Dispose();
            base.OnDisposing();
        }

        private static IDatabaseCommand CreateCommand(IDatabase database, IDatabaseQuery query, ITransactionSource transaction)
        {
            return database.CreateCommand(query, DatabaseCommandFlags.NoCache, transaction);
        }
    }
}
