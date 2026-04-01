using FoxTunes.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

        public static readonly DependencyProperty PanelTemplateProperty = DependencyProperty.RegisterAttached(
           "PanelTemplate",
           typeof(ItemsPanelTemplate),
           typeof(ListViewExtensions),
           new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnPanelTemplatePropertyChanged))
       );

        public static ItemsPanelTemplate GetPanelTemplate(ListView source)
        {
            return (ItemsPanelTemplate)source.GetValue(PanelTemplateProperty);
        }

        public static void SetPanelTemplate(ListView source, ItemsPanelTemplate value)
        {
            source.SetValue(PanelTemplateProperty, value);
        }

        private static void OnPanelTemplatePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
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
                BindingHelper.AddHandler(
                    this.ListView,
                    ItemsControl.ItemsSourceProperty,
                    typeof(ListView),
                    this.OnItemsSourceChanged
                );
            }

            public ListView ListView { get; private set; }

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

            public ItemsPanelTemplate PanelTemplate
            {
                get
                {
                    return GetPanelTemplate(this.ListView);
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
                var items = this.ListView.ItemsSource;
                if (!(items is PlaylistItem[]))
                {
                    return;
                }
                var playlistItems = (PlaylistItem[])items;
                var script = this.Script;
                var values = new string[playlistItems.Length];
                var groups = new Dictionary<PlaylistItem, string>();
                this.Dispatch(() =>
                {
                    var scriptingContexts = new ThreadLocal<IScriptingContext>(ScriptingRuntime.CreateContext, true);
                    Parallel.For(0, playlistItems.Length, index =>
                    {
                        var playlistItem = playlistItems[index];
                        var scriptingContext = scriptingContexts.Value;
                        var runner = new PlaylistItemScriptRunner(scriptingContext, playlistItem, script);
                        runner.Prepare();
                        var value = runner.Run();
                        values[index] = Convert.ToString(value);
                    });
                    {
                        var previousValue = default(string);
                        var index = 0;
                        for (var a = 0; a < values.Length; a++)
                        {
                            var value = values[a];
                            if (!string.Equals(previousValue, value))
                            {
                                previousValue = value;
                                index++;
                            }
                            groups.Add(playlistItems[a], string.Format("{0}\t{1}", index, value));
                        }
                    }
                    foreach (var scriptingContext in scriptingContexts.Values)
                    {
                        scriptingContext.Dispose();
                    }
                    scriptingContexts.Dispose();
                    return Windows.Invoke(() =>
                    {
                        this.CollectionView.GroupDescriptions.Add(
                            new PlaylistGroupDescription(
                                groups
                            )
                        );
                        this.ListView.GroupStyle.Add(
                            new GroupStyle()
                            {
                                HeaderTemplate = this.HeaderTemplate,
                                ContainerStyle = this.ContainerStyle,
                                Panel = this.PanelTemplate
                            }
                        );
                    });
                });
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
                if (this.HeaderTemplate == null || this.ContainerStyle == null || this.PanelTemplate == null)
                {
                    return;
                }
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
                base.Dispose(disposing);
            }

            private class PlaylistGroupDescription : GroupDescription
            {
                public PlaylistGroupDescription(IDictionary<PlaylistItem, string> groups)
                {
                    this.Groups = groups;
                }

                public IDictionary<PlaylistItem, string> Groups { get; private set; }

                public override object GroupNameFromItem(object item, int level, CultureInfo culture)
                {
                    if (item is PlaylistItem playlistItem)
                    {
                        return this.Groups[playlistItem];
                    }
                    return item;
                }
            }
        }
    }
}