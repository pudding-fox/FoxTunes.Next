using FoxTunes.Interfaces;
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace FoxTunes
{
    public static partial class ListViewExtensions
    {
        private static readonly ConditionalWeakTable<ListView, SearchBehaviour> SearchBehaviours = new ConditionalWeakTable<ListView, SearchBehaviour>();

        public static readonly DependencyProperty SearchProperty = DependencyProperty.RegisterAttached(
                "Search",
                typeof(bool),
                typeof(ListViewExtensions),
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
            var listView = sender as ListView;
            if (listView == null)
            {
                return;
            }
            if (GetSearch(listView))
            {
                var behaviour = default(SearchBehaviour);
                if (!SearchBehaviours.TryGetValue(listView, out behaviour))
                {
                    SearchBehaviours.Add(listView, new SearchBehaviour(listView));
                }
            }
            else
            {
                var behaviour = default(SearchBehaviour);
                if (SearchBehaviours.TryRemove(listView, out behaviour))
                {
                    behaviour.Dispose();
                }
            }
        }

        public class SearchBehaviour : UIBehaviour<ListView>
        {
            const int INTERVAL = 1000;

            public static readonly IPlaylistManager PlaylistManager = ComponentRegistry.Instance.GetComponent<IPlaylistManager>();

            public SearchBehaviour(ListView listView) : base(listView)
            {
                this.ListView = listView;
                this.ListView.PreviewTextInput += this.OnPreviewTextInput;
                this.ListView.PreviewKeyDown += this.OnPreviewKeyDown;
                this.Timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(INTERVAL)
                };
                this.Timer.Tick += this.OnTick;
                this.Text = string.Empty;
            }

            public ListView ListView { get; private set; }

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
                PlaylistManager.Filter = this.Text;
                this.Text = string.Empty;
            }
        }
    }
}
