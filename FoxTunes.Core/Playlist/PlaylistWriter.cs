#pragma warning disable 612, 618
using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class PlaylistWriter : Disposable
    {
        public PlaylistWriter(IDatabaseComponent database, ITransactionSource transaction)
        {
            this.Command = CreateCommand(database, transaction);
        }

        public IDatabaseCommand Command { get; private set; }

        public Task Write(PlaylistItem playlistItem)
        {
            this.Command.Parameters["playlistId"] = playlistItem.Playlist_Id;
            this.Command.Parameters["directoryName"] = playlistItem.DirectoryName;
            this.Command.Parameters["fileName"] = playlistItem.FileName;
            this.Command.Parameters["sequence"] = playlistItem.Sequence;
            this.Command.Parameters["status"] = playlistItem.Status;
            this.Command.Parameters["flags"] = playlistItem.Flags;
            return this.Command.ExecuteNonQueryAsync();
        }

        protected override void OnDisposing()
        {
            this.Command.Dispose();
            base.OnDisposing();
        }

        private static IDatabaseCommand CreateCommand(IDatabaseComponent database, ITransactionSource transaction)
        {
            var query = database.QueryFactory.Build();
            query.Add.SetTable(database.Tables.PlaylistItem);
            query.Add.AddColumns(database.Tables.PlaylistItem.Columns.Except(database.Tables.PlaylistItem.PrimaryKeys));
            query.Output.AddSubQuery(database.QueryFactory.Build().With(subQuery =>
            {
                subQuery.Output.AddColumn(database.Tables.LibraryItem.Column("Id"));
                subQuery.Source.AddTable(database.Tables.LibraryItem);
                subQuery.Filter.AddColumn(database.Tables.LibraryItem.Column("FileName"));
            }));
            query.Output.AddParameters(database.Tables.PlaylistItem.Columns.Except(database.Tables.PlaylistItem.PrimaryKeys.Concat(database.Tables.PlaylistItem.Column("LibraryItem_Id"))));
            return database.CreateCommand(query.Build(), DatabaseCommandFlags.NoCache, transaction);
        }
    }
}
