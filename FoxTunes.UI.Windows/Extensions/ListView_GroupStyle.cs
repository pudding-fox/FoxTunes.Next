using FoxTunes.Interfaces;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FoxTunes
{
    public static partial class ListViewExtensions
    {
        private static readonly ConditionalWeakTable<ListView, GroupStyleBehaviour> GroupStyleBehaviours = new ConditionalWeakTable<ListView, GroupStyleBehaviour>();

        public static readonly DependencyProperty GroupStyleProperty = DependencyProperty.RegisterAttached(
            "GroupStyle",
            typeof(bool),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnGroupStylePropertyChanged))
        );

        public static bool GetGroupStyle(ListView source)
        {
            return (bool)source.GetValue(GroupStyleProperty);
        }

        public static void SetGroupStyle(ListView source, bool value)
        {
            source.SetValue(GroupStyleProperty, value);
        }

        private static void OnGroupStylePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView == null)
            {
                return;
            }
            var behaviour = default(GroupStyleBehaviour);
            if (GetGroupStyle(listView))
            {
                if (!GroupStyleBehaviours.TryGetValue(listView, out behaviour))
                {
                    behaviour = new GroupStyleBehaviour(listView);
                    GroupStyleBehaviours.Add(listView, behaviour);
                    behaviour.Enable();
                }
            }
            else
            {
                if (GroupStyleBehaviours.TryGetValue(listView, out behaviour))
                {
                    GroupStyleBehaviours.Remove(listView);
                    behaviour.Disable();
                    behaviour.Dispose();
                }
            }
        }

        public static readonly DependencyProperty GroupScriptProperty = DependencyProperty.RegisterAttached(
            "GroupScript",
            typeof(string),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnGroupScriptPropertyChanged))
        );

        public static string GetGroupScript(ListView source)
        {
            return (string)source.GetValue(GroupScriptProperty);
        }

        public static void SetGroupScript(ListView source, string value)
        {
            source.SetValue(GroupScriptProperty, value);
        }

        private static void OnGroupScriptPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView == null)
            {
                return;
            }
            var behaviour = default(GroupStyleBehaviour);
            if (!GroupStyleBehaviours.TryGetValue(listView, out behaviour))
            {
                return;
            }
            behaviour.Refresh();
        }

        public static readonly DependencyProperty GroupHeaderTemplateProperty = DependencyProperty.RegisterAttached(
            "GroupHeaderTemplate",
            typeof(DataTemplate),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnGroupHeaderTemplatePropertyChanged))
        );

        public static DataTemplate GetGroupHeaderTemplate(ListView source)
        {
            return (DataTemplate)source.GetValue(GroupHeaderTemplateProperty);
        }

        public static void SetGroupHeaderTemplate(ListView source, DataTemplate value)
        {
            source.SetValue(GroupHeaderTemplateProperty, value);
        }

        private static void OnGroupHeaderTemplatePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView == null)
            {
                return;
            }
            var behaviour = default(GroupStyleBehaviour);
            if (!GroupStyleBehaviours.TryGetValue(listView, out behaviour))
            {
                return;
            }
            behaviour.Refresh();
        }

        public static readonly DependencyProperty GroupContainerStyleProperty = DependencyProperty.RegisterAttached(
           "GroupContainerStyle",
           typeof(Style),
           typeof(ListViewExtensions),
           new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnGroupContainerStylePropertyChanged))
       );

        public static Style GetGroupContainerStyle(ListView source)
        {
            return (Style)source.GetValue(GroupContainerStyleProperty);
        }

        public static void SetGroupContainerStyle(ListView source, Style value)
        {
            source.SetValue(GroupContainerStyleProperty, value);
        }

        private static void OnGroupContainerStylePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView == null)
            {
                return;
            }
            var behaviour = default(GroupStyleBehaviour);
            if (!GroupStyleBehaviours.TryGetValue(listView, out behaviour))
            {
                return;
            }
            behaviour.Refresh();
        }

        private class GroupStyleBehaviour : UIBehaviour
        {
            public static IScriptingRuntime ScriptingRuntime = ComponentRegistry.Instance.GetComponent<IScriptingRuntime>();

            public GroupStyleBehaviour(ListView listView)
            {
                this.ListView = listView;
                this.ScriptingContext = ScriptingRuntime.CreateContext();
                BindingHelper.AddHandler(
                    this.ListView,
                    ItemsControl.ItemsSourceProperty,
                    typeof(ListView),
                    this.OnItemsSourceChanged
                );
            }

            public ListView ListView { get; private set; }

            public IScriptingContext ScriptingContext { get; private set; }

            public string Script
            {
                get
                {
                    return GetGroupScript(this.ListView);
                }
            }

            public DataTemplate HeaderTemplate
            {
                get
                {
                    return GetGroupHeaderTemplate(this.ListView);
                }
            }

            public Style ContainerStyle
            {
                get
                {
                    return GetGroupContainerStyle(this.ListView);
                }
            }

            public CollectionView CollectionView
            {
                get
                {
                    return CollectionViewSource.GetDefaultView(this.ListView.ItemsSource) as CollectionView;
                }
            }

            public virtual void Enable()
            {
                if (string.IsNullOrEmpty(this.Script) || this.HeaderTemplate == null || this.CollectionView == null)
                {
                    return;
                }
                this.CollectionView.GroupDescriptions.Add(
                    new PlaylistGroupDescription(
                        this.ScriptingContext,
                        this.Script
                    )
                );
                this.ListView.GroupStyle.Add(
                    new GroupStyle()
                    {
                        HeaderTemplate = this.HeaderTemplate,
                        ContainerStyle = this.ContainerStyle
                    }
                );
            }

            public virtual void Disable()
            {
                this.ListView.GroupStyle.Clear();
                if (this.CollectionView != null)
                {
                    this.CollectionView.GroupDescriptions.Clear();
                }
            }

            public void Refresh()
            {
                this.Disable();
                this.Enable();
            }

            protected virtual void OnItemsSourceChanged(object sender, EventArgs e)
            {
                this.Refresh();
            }

            protected override void Dispose(bool disposing)
            {
                if (this.ListView != null)
                {
                    BindingHelper.RemoveHandler(
                        this.ListView,
                        ItemsControl.ItemsSourceProperty,
                        typeof(ListView),
                        this.OnItemsSourceChanged
                    );
                }
                if (this.ScriptingContext != null)
                {
                    this.ScriptingContext.Dispose();
                    this.ScriptingContext = null;
                }
                base.Dispose(disposing);
            }

            private class PlaylistGroupDescription : GroupDescription
            {
                public PlaylistGroupDescription(IScriptingContext scriptingContext, string script)
                {
                    this.ScriptingContext = scriptingContext;
                    this.Script = script;
                }

                public IScriptingContext ScriptingContext { get; private set; }

                public string Script { get; private set; }

                public override object GroupNameFromItem(object item, int level, CultureInfo culture)
                {
                    var playlistItem = item as PlaylistItem;
                    if (playlistItem != null)
                    {
                        var runner = new PlaylistItemScriptRunner(this.ScriptingContext, playlistItem, this.Script);
                        runner.Prepare();
                        return runner.Run();
                    }
                    return item;
                }
            }
        }
    }
}