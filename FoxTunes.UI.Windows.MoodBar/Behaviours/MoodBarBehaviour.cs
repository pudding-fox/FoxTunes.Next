using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Markup;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class MoodBarBehaviour : StandardBehaviour, IUIPlaylistColumnProvider, IDatabaseInitializer
    {
        public const string ID = "D4A7AC1D-8268-4F7B-B582-EAA19E77E9D8";

        #region IPlaylistColumnProvider

        public string Id
        {
            get
            {
                return ID;
            }
        }

        public string Name
        {
            get
            {
                return Strings.MoodBarStreamPosition_Name;
            }
        }

        public string Description
        {
            get
            {
                return null;
            }
        }

        public bool DependsOn(IEnumerable<string> names)
        {
            return false;
        }

        public string GetValue(PlaylistItem playlistItem)
        {
            return null;
        }

        #endregion

        #region IUIPlaylistColumnProvider

        public DataTemplate CellTemplate
        {
            get
            {
                return TemplateFactory.Template;
            }
        }

        #endregion

        #region IDatabaseInitializer

        string IDatabaseInitializer.Checksum
        {
            get
            {
                return "D4A7AC1D-8268-4F7B-B582-EAA19E77E9D8";
            }
        }

        void IDatabaseInitializer.InitializeDatabase(IDatabaseComponent database, DatabaseInitializeType type)
        {
            //IMPORTANT: When editing this function remember to change the checksum.
            if (!type.HasFlag(DatabaseInitializeType.Playlist))
            {
                return;
            }
            using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
            {
                var set = database.Set<PlaylistColumn>(transaction);
                set.Add(new PlaylistColumn()
                {
                    Name = Strings.MoodBarStreamPosition_Name,
                    Type = PlaylistColumnType.Plugin,
                    Sequence = 100,
                    Plugin = ID,
                    Enabled = false
                });
                transaction.Commit();
            }
        }

        #endregion

        private static class TemplateFactory
        {
            private static Lazy<DataTemplate> _Template = new Lazy<DataTemplate>(GetTemplate);

            public static DataTemplate Template
            {
                get
                {
                    return _Template.Value;
                }
            }

            private static DataTemplate GetTemplate()
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Resources.MoodBar)))
                {
                    var template = (DataTemplate)XamlReader.Load(stream);
                    template.Seal();
                    return template;
                }
            }
        }
    }
}
