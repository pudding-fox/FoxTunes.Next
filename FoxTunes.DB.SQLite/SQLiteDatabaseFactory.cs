using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace FoxTunes
{
    [ComponentPreference(ComponentPreferenceAttribute.DEFAULT)]
    [Component(ID, ComponentSlots.Database)]
    public class SQLiteDatabaseFactory : DatabaseFactory
    {
        const string ID = "13A75018-8A24-413D-A731-C558C8FAF08F";

        public SQLiteDatabaseFactory() : base(ID, string.Format(Strings.SQLiteDatabaseFactory_Name, SQLiteConnection.SQLiteVersion))
        {

        }

        public override DatabaseFactoryFlags Flags
        {
            get
            {
                return DatabaseFactoryFlags.None;
            }
        }

        protected override DatabaseTestResult OnTest(IDatabase database)
        {
            if (File.Exists(SQLiteDatabase.FileName))
            {
                try
                {
                    switch (database.Connection.State)
                    {
                        case ConnectionState.Open:
                            if (!DatabaseChecksum.Instance.Validate(database, Resources.Database))
                            {
                                return DatabaseTestResult.Mismatch;
                            }
                            return DatabaseTestResult.OK;
                    }
                }
                catch (SQLiteException)
                {
                    //Nothing can be done.
                }
            }
            return DatabaseTestResult.Missing;
        }

        protected override void OnInitialize(IDatabase database)
        {
            this.CreateDatabase(database);
        }

        protected override IDatabaseComponent OnCreate()
        {
            return new SQLiteDatabase();
        }

        protected virtual void CreateDatabase(IDatabase database)
        {
            var exception = default(Exception);
            if (File.Exists(SQLiteDatabase.FileName))
            {
                SQLiteConnection.ClearAllPools();
                File.Delete(SQLiteDatabase.FileName);
            }
            database.Provider.CreateDatabase(SQLiteDatabase.FileName);
            try
            {
                database.Execute(database.QueryFactory.Create(Resources.Database));
                DatabaseChecksum.Instance.Set(database, Resources.Database);
                return;
            }
            catch (Exception e)
            {
                exception = e;
                try
                {
                    database.Connection.Close();
                    database.Provider.DeleteDatabase(SQLiteDatabase.FileName);
                }
                catch
                {
                    //Nothing can be done.
                }
            }
            if (File.Exists(SQLiteDatabase.FileName))
            {
                try
                {
                    File.Delete(SQLiteDatabase.FileName);
                }
                catch
                {
                    //Nothing can be done.
                }
            }
            throw new InvalidOperationException(string.Format("Failed to create the database: {0}", exception.Message), exception);
        }
    }
}
