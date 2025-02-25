using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace FoxTunes
{
    public class SQLiteDatabase : Database
    {
        public static readonly string FileName = Path.Combine(
            Publication.StoragePath,
            "Database.db"
        );

        public SQLiteDatabase()
            : base(GetProvider())
        {

        }

        public override IsolationLevel PreferredIsolationLevel
        {
            get
            {
                return IsolationLevel.ReadCommitted;
            }
        }

        protected override IDatabaseQueries CreateQueries()
        {
            var queries = new SQLiteDatabaseQueries(this);
            queries.InitializeComponent(this.Core);
            return queries;
        }

        private static IProvider GetProvider()
        {
            var builder = new SQLiteConnectionStringBuilder();
            builder.DataSource = FileName;
            builder.Pooling = true;
            builder.JournalMode = SQLiteJournalModeEnum.Wal;
            builder["cache"] = "shared";
            builder["mode"] = "rwc";
            return new SQLiteProvider(builder);
        }
    }
}
