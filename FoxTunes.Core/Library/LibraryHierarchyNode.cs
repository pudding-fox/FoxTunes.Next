using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    [Table(Name = "LibraryHierarchyItems")]
    public class LibraryHierarchyNode : PersistableComponent, IExpandable, IHierarchical
    {
        public LibraryHierarchyNode()
        {

        }

        [Column(Name = "LibraryHierarchy_Id")]
        public int LibraryHierarchyId { get; set; }

        public string Value { get; set; }

        public bool IsLeaf { get; set; }

        public LibraryHierarchyNode Parent { get; set; }

        private ResettableLazy<LibraryHierarchyNode[]> _Children { get; set; }

        public LibraryHierarchyNode[] Children
        {
            get
            {
                if (this.IsExpanded)
                {
                    if (this._Children != null)
                    {
                        return this._Children.Value;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (this.IsLeaf)
                {
                    return new LibraryHierarchyNode[] { };
                }
                else
                {
                    return new LibraryHierarchyNode[] { LibraryHierarchyNode.Empty };
                }
            }
        }

        protected virtual void OnChildrenChanged()
        {
            if (this.ChildrenChanged != null)
            {
                this.ChildrenChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Children");
        }

        public event EventHandler ChildrenChanged;

        private bool _IsExpanded { get; set; }

        public bool IsExpanded
        {
            get
            {
                return this._IsExpanded;
            }
            set
            {
                this._IsExpanded = value;
                this.OnIsExpandedChanged();
            }
        }

        protected virtual void OnIsExpandedChanged()
        {
            this.OnChildrenChanged();
            if (this.IsExpandedChanged != null)
            {
                this.IsExpandedChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsExpanded");
        }

        public event EventHandler IsExpandedChanged;

        #region IHierarchical

        IHierarchical IHierarchical.Parent
        {
            get
            {
                return this.Parent;
            }
        }

        IEnumerable<IHierarchical> IHierarchical.Children
        {
            get
            {
                return this.Children;
            }
        }

        #endregion

        public virtual void InitializeComponent(ILibraryHierarchyBrowser libraryHierarchyBrowser)
        {
            this._Children = new ResettableLazy<LibraryHierarchyNode[]>(
                () => libraryHierarchyBrowser.GetNodes(this)
            );
        }

        public void Refresh(IEnumerable<string> names)
        {
            if (names != null && !names.Contains(CommonImageTypes.FrontCover, StringComparer.OrdinalIgnoreCase))
            {
                //Only refresh if the artwork has changed.
                return;
            }
            this.Refresh(HierarchyDirection.Both);
        }

        protected virtual void Refresh(HierarchyDirection direction)
        {
            if (direction.HasFlag(HierarchyDirection.Up) && this.Parent != null)
            {
                this.Parent.Refresh(HierarchyDirection.Up);
            }
            //TODO: What we really want to do is refresh all {Bindings} against this instance but there isn't always a Path, sometimes it's a Converter for the entire object.
            this.OnPropertyChanged(string.Empty);
            if (direction.HasFlag(HierarchyDirection.Down) && this._Children.IsValueCreated)
            {
                foreach (var libraryHierarchyNode in this._Children.Value)
                {
                    libraryHierarchyNode.Refresh(HierarchyDirection.Down);
                }
            }
        }

        public override int GetHashCode()
        {
            //We need a hash code for this type for performance reasons.
            //base.GetHashCode() returns 0.
            return this.Id.GetHashCode() * 29;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as LibraryHierarchyNode);
        }

        public static readonly LibraryHierarchyNode Empty = new LibraryHierarchyNode();

        [Flags]
        public enum HierarchyDirection : byte
        {
            None = 0,
            Up = 1,
            Down = 2,
            Both = Up | Down
        }
    }
}
