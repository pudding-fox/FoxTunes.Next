#pragma warning disable 612, 618
using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class PlaylistItem : PersistableComponent, IFileData
    {
        public static readonly IDatabaseFactory DatabaseFactory = ComponentRegistry.Instance.GetComponent<IDatabaseFactory>();

        public PlaylistItem()
        {

        }

        public int Playlist_Id { get; set; }

        public int? LibraryItem_Id { get; set; }

        public int Sequence { get; set; }

        public string DirectoryName { get; set; }

        public string FileName { get; set; }

        public PlaylistItemStatus Status { get; set; }

        public PlaylistItemFlags Flags { get; set; }

        private IList<MetaDataItem> _MetaDatas { get; set; }

        [Relation(Flags = RelationFlags.AutoExpression | RelationFlags.EagerFetch | RelationFlags.ManyToMany)]
        public IList<MetaDataItem> MetaDatas
        {
            get
            {
                this.EnsureMetaData();
                return this._MetaDatas;
            }
            set
            {
                this._MetaDatas = value;
                this.OnMetaDatasChanged();
            }
        }

        protected virtual void OnMetaDatasChanged()
        {
            if (this.MetaDatasChanged != null)
            {
                this.MetaDatasChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MetaDatas");
        }

        public event EventHandler MetaDatasChanged;

        public void EnsureMetaData()
        {
            if (this._MetaDatas == null)
            {
                this._MetaDatas = this.GetMetaData();
            }
        }

        public void EnsureMetaData(IDatabaseComponent database, ITransactionSource transaction)
        {
            if (this._MetaDatas == null)
            {
                this._MetaDatas = this.GetMetaData(database, transaction);
            }
        }

        public override int GetHashCode()
        {
            //We need a hash code for this type for performance reasons.
            //base.GetHashCode() returns 0.
            return this.Id.GetHashCode() * 29;
        }

        public override bool Equals(IPersistableComponent other)
        {
            if (other is PlaylistItem)
            {
                return base.Equals(other) && string.Equals(this.FileName, (other as PlaylistItem).FileName, StringComparison.OrdinalIgnoreCase);
            }
            return base.Equals(other);
        }

        public IList<MetaDataItem> GetMetaData()
        {
            using (var database = DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    return this.GetMetaData(database, transaction);
                }
            }
        }

        public IList<MetaDataItem> GetMetaData(IDatabaseComponent database, ITransactionSource transaction)
        {
            var sequence = database.ExecuteEnumerator<MetaDataItem>(
                database.Queries.GetPlaylistItemMetaData,
                (parameters, phase) =>
                {
                    switch (phase)
                    {
                        case DatabaseParameterPhase.Fetch:
                            parameters["playlistItemId"] = this.Id;
                            parameters["libraryItemId"] = this.LibraryItem_Id;
                            break;
                    }
                },
                transaction
            );
            return sequence.ToList();
        }

        public static IEnumerable<PlaylistItem> Clone(IEnumerable<PlaylistItem> playlistItems)
        {
            return playlistItems.Select(Clone);
        }

        public static PlaylistItem Clone(PlaylistItem playlistItem)
        {
            var result = new PlaylistItem()
            {
                Playlist_Id = playlistItem.Id,
                LibraryItem_Id = playlistItem.LibraryItem_Id,
                Sequence = playlistItem.Sequence,
                DirectoryName = playlistItem.DirectoryName,
                FileName = playlistItem.FileName,
                Status = playlistItem.Status,
                Flags = playlistItem.Flags
            };
            if (!result.LibraryItem_Id.HasValue)
            {
                //TODO: Synchronization.
                result.MetaDatas = new List<MetaDataItem>(playlistItem.MetaDatas);
            }
            else
            {
                result.MetaDatas = new List<MetaDataItem>();
            }
            return result;
        }
    }

    public enum PlaylistItemStatus : byte
    {
        None = 0,
        Import = 1,
        Update = 2,
        Remove = 3
    }

    public enum PlaylistItemFlags : byte
    {
        None = 0,
        Export = 1
    }
}
