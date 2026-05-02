using FoxTunes.Interfaces;
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace FoxTunes
{
    public partial class TreeViewExtensions
    {
        private static readonly ConditionalWeakTable<TreeView, SearchBehaviour> SearchBehaviours = new ConditionalWeakTable<TreeView, SearchBehaviour>();

        public static readonly DependencyProperty SearchProperty = DependencyProperty.RegisterAttached(
                "Search",
                typeof(bool),
                typeof(TreeViewExtensions),
                new PropertyMetadata(false, OnSearchChanged)
        );

        public static void SetSearch(DependencyObject element, bool value)
        {
            element.SetValue(SearchProperty, value);
        }

        public static bool GetSearch(DependencyObject element)
        {
            return (bool)element.GetValue(SearchProperty);
        }

        private static void OnSearchChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var TreeView = sender as TreeView;
            if (TreeView == null)
            {
                return;
            }
            if (GetSearch(TreeView))
            {
                var behaviour = default(SearchBehaviour);
                if (!SearchBehaviours.TryGetValue(TreeView, out behaviour))
                {
                    SearchBehaviours.Add(TreeView, new SearchBehaviour(TreeView));
                }
            }
            else
            {
                var behaviour = default(SearchBehaviour);
                if (SearchBehaviours.TryRemove(TreeView, out behaviour))
                {
                    behaviour.Dispose();
                }
            }
        }

        public class SearchBehaviour : UIBehaviour<TreeView>
        {
            const int INTERVAL = 1000;

            public static readonly ILibraryHierarchyBrowser LibraryManager = ComponentRegistry.Instance.GetComponent<ILibraryHierarchyBrowser>();

            public SearchBehaviour(TreeView TreeView) : base(TreeView)
            {
                this.TreeView = TreeView;
                this.TreeView.PreviewTextInput += this.OnPreviewTextInput;
                this.TreeView.PreviewKeyDown += this.OnPreviewKeyDown;
                this.Timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(INTERVAL)
                };
                this.Timer.Tick += this.OnTick;
                this.Text = string.Empty;
            }

            public TreeView TreeView { get; private set; }

            public DispatcherTimer Timer { get; private set; }

            public string Text { get; private set; }

            protected virtual void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
            {
                if (string.IsNullOrEmpty(e.Text))
                {
                    return;
                }
                this.Text += e.Text;
                this.RestartTimer();
                e.Handled = true;
            }

            protected virtual void OnPreviewKeyDown(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Back && this.Text.Length > 0)
                {
                    this.Text = this.Text.Substring(0, this.Text.Length - 1);
                    this.RestartTimer();
                    e.Handled = true;
                }
                else if (e.Key == Key.Space)
                {
                    this.Text = this.Text + ' ';
                    this.RestartTimer();
                    e.Handled = true;
                }
            }

            protected virtual void OnTick(object sender, EventArgs e)
            {
                this.Reset();
            }

            protected virtual void RestartTimer()
            {
                this.Timer.Stop();
                this.Timer.Start();
            }

            protected virtual void Reset()
            {
                this.Timer.Stop();
                LibraryManager.Filter = this.Text;
                this.Text = string.Empty;
            }
        }
    }
}
