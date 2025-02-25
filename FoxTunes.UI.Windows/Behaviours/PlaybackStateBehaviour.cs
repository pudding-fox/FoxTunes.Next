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
    [ComponentPriority(ComponentPriorityAttribute.LOW)]
    [Component(ID)]
    [WindowsUserInterfaceDependency]
    public class PlaybackStateBehaviour : StandardBehaviour, IUIPlaylistColumnProvider, IDatabaseInitializer
    {
        public const string ID = "C0B2450C-DEDA-4D8B-8A32-5EA733F1FD45";

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
                return "Playback State";
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
            //ViewModel.PlaybackState tracks updates.
            return false;
        }

        public string GetValue(PlaylistItem playlistItem)
        {
            var currentStream = this.PlaybackManager.CurrentStream;
            if (currentStream != null)
            {
                var isPlaying = playlistItem.Id == currentStream.Id && string.Equals(playlistItem.FileName, currentStream.FileName, StringComparison.OrdinalIgnoreCase);
                if (isPlaying)
                {
                    return "1";
                }
            }
            return "0";
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

        public string Checksum
        {
            get
            {
                return "784D5E56-D574-4CA9-8F93-70A7B0FF9B45";
            }
        }

        public void InitializeDatabase(IDatabaseComponent database, DatabaseInitializeType type)
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
                    Name = "Playing",
                    Type = PlaylistColumnType.Plugin,
                    Sequence = 0,
                    Plugin = this.Id,
                    Enabled = true
                });
                transaction.Commit();
            }
        }

        #endregion

        public IPlaybackManager PlaybackManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            base.InitializeComponent(core);
        }

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
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Resources.PlaybackState)))
                {
                    var template = (DataTemplate)XamlReader.Load(stream);
                    template.Seal();
                    return template;
                }
            }
        }
    }
}
