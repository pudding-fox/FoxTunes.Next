#pragma warning disable 612, 618
using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Data;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class LibraryWriter : Disposable
    {
        public LibraryWriter(IDatabaseComponent database, ITransactionSource transaction)
        {
            this.Command = CreateCommand(database, transaction);
        }

        public IDatabaseCommand Command { get; private set; }

        public Task Write(LibraryItem libraryItem)
        {
            this.Command.Parameters["directoryName"] = libraryItem.DirectoryName;
            this.Command.Parameters["fileName"] = libraryItem.FileName;
            this.Command.Parameters["importDate"] = libraryItem.ImportDate;
            this.Command.Parameters["status"] = libraryItem.Status;
            this.Command.Parameters["flags"] = libraryItem.Flags;
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
            query.Add.AddColumn(database.Tables.LibraryItem.Column("DirectoryName"));
            query.Add.AddColumn(database.Tables.LibraryItem.Column("FileName"));
            query.Add.AddColumn(database.Tables.LibraryItem.Column("ImportDate"));
            query.Add.AddColumn(database.Tables.LibraryItem.Column("Status"));
            query.Add.AddColumn(database.Tables.LibraryItem.Column("Flags"));
            query.Add.SetTable(database.Tables.LibraryItem);
            query.Output.AddParameter("DirectoryName", DbType.String, 0, 0, 0, ParameterDirection.Input, false, null, DatabaseQueryParameterFlags.None);
            query.Output.AddParameter("FileName", DbType.String, 0, 0, 0, ParameterDirection.Input, false, null, DatabaseQueryParameterFlags.None);
            query.Output.AddParameter("ImportDate", DbType.String, 0, 0, 0, ParameterDirection.Input, false, null, DatabaseQueryParameterFlags.None);
            query.Output.AddParameter("Status", DbType.Byte, 0, 0, 0, ParameterDirection.Input, false, null, DatabaseQueryParameterFlags.None);
            query.Output.AddParameter("Flags", DbType.Byte, 0, 0, 0, ParameterDirection.Input, false, null, DatabaseQueryParameterFlags.None);
            query.Filter.Expressions.Add(
                query.Filter.CreateUnary(
                    QueryOperator.Not,
                    query.Filter.CreateFunction(
                        QueryFunction.Exists,
                        query.Filter.CreateSubQuery(
                            database.QueryFactory.Build().With(subQuery =>
                            {
                                subQuery.Output.AddOperator(QueryOperator.Star);
                                subQuery.Source.AddTable(database.Tables.LibraryItem);
                                subQuery.Filter.AddColumn(database.Tables.LibraryItem.Column("FileName"));
                            })
                        )
                    )
                )
            );
            return database.CreateCommand(query.Build(), DatabaseCommandFlags.NoCache, transaction);
        }
    }
}
