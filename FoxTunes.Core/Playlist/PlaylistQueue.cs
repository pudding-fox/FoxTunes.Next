﻿using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class PlaylistQueue : StandardComponent, IPlaylistQueue
    {
        public const string QUEUE_LAST = "BBBB";

        public const string QUEUE_NEXT = "BBBC";

        public const string QUEUE_RESET = "BBBD";

        public PlaylistQueue()
        {
            this.Semaphore = new SemaphoreSlim(1, 1);
            this.Queue = new ConcurrentDictionary<Playlist, List<PlaylistItem>>();
        }

        public SemaphoreSlim Semaphore { get; private set; }

        public ConcurrentDictionary<Playlist, List<PlaylistItem>> Queue { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        protected virtual Playlist GetPlaylist(PlaylistItem playlistItem)
        {
            if (playlistItem == null)
            {
                return this.PlaylistManager.CurrentPlaylist ?? this.PlaylistManager.SelectedPlaylist;
            }
            return this.PlaylistBrowser.GetPlaylist(playlistItem) ?? this.PlaylistManager.SelectedPlaylist;
        }

        public override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                PlaylistBehaviourConfiguration.SECTION,
                PlaylistBehaviourConfiguration.QUEUE_ELEMENT
            );
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.Refresh);
        }

        public virtual Task Refresh()
        {
            if (this.Queue.Count > 0)
            {
                var outputStream = this.PlaybackManager.CurrentStream;
                if (outputStream != null)
                {
                    var playlistItem = outputStream.PlaylistItem;
                    if (playlistItem != null)
                    {
                        this.Enqueue(playlistItem, PlaylistQueueFlags.Reset);
                    }
                }
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        private Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.PlaylistUpdated:
                    this.OnPlaylistUpdated(signal.State as PlaylistUpdatedSignalState);
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual void OnPlaylistUpdated(PlaylistUpdatedSignalState state)
        {
            if (state != null && state.Playlists != null && state.Playlists.Any())
            {
                foreach (var playlist in state.Playlists)
                {
                    this.Queue.TryRemove(playlist);
                }
            }
            else
            {
                this.Queue.Clear();
            }
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_PLAYLIST;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.Enabled.Value && this.PlaylistManager.SelectedItems != null && this.PlaylistManager.SelectedItems.Any())
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, QUEUE_LAST, Strings.PlaylistQueue_Add, path: Strings.PlaylistQueue_Path, attributes: InvocationComponent.ATTRIBUTE_SEPARATOR);
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, QUEUE_NEXT, Strings.PlaylistQueue_AddNext, path: Strings.PlaylistQueue_Path);
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_PLAYLIST, QUEUE_RESET, Strings.PlaylistQueue_Reset, path: Strings.PlaylistQueue_Path);
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case QUEUE_LAST:
                    this.Enqueue(this.PlaylistManager.SelectedItems, PlaylistQueueFlags.None);
                    break;
                case QUEUE_NEXT:
                    this.Enqueue(this.PlaylistManager.SelectedItems, PlaylistQueueFlags.Next);
                    break;
                case QUEUE_RESET:
                    this.Enqueue(this.PlaylistManager.SelectedItems, PlaylistQueueFlags.Reset);
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public void Enqueue(IEnumerable<PlaylistItem> playlistItems, PlaylistQueueFlags flags)
        {
            if (flags == PlaylistQueueFlags.Next)
            {
                playlistItems = playlistItems.Reverse();
            }
            foreach (var playlistItem in playlistItems)
            {
                this.Enqueue(playlistItem, flags);
            }
        }

        public void Enqueue(PlaylistItem playlistItem, PlaylistQueueFlags flags)
        {
            var playlist = this.GetPlaylist(playlistItem);
            var queue = default(List<PlaylistItem>);
            if (this.Queue.TryGetValue(playlist, out queue))
            {
                //TODO: We should use WaitAsync for >NET40.
                this.Semaphore.Wait();
                try
                {
                    queue.Remove(playlistItem);
                    if (queue.Count == 0)
                    {
                        this.Queue.TryRemove(playlist);
                    }
                }
                finally
                {
                    this.Semaphore.Release();
                }
            }
            switch (flags)
            {
                case PlaylistQueueFlags.None:
                    this.Queue.GetOrAdd(playlist, key => new List<PlaylistItem>()).Add(playlistItem);
                    break;
                case PlaylistQueueFlags.Next:
                    this.Queue.GetOrAdd(playlist, key => new List<PlaylistItem>()).Insert(0, playlistItem);
                    break;
            }
        }

        public PlaylistItem GetNext(PlaylistItem playlistItem)
        {
            if (this.Queue.Count > 0)
            {
                var playlist = this.GetPlaylist(playlistItem);
                if (playlist == null)
                {
                    return null;
                }
                var queue = default(List<PlaylistItem>);
                if (this.Queue.TryGetValue(playlist, out queue))
                {
                    //TODO: We should use WaitAsync for >NET40.
                    this.Semaphore.Wait();
                    try
                    {
                        if (playlistItem != null)
                        {
                            var position = queue.IndexOf(playlistItem);
                            if (position > 0 && position - 1 < queue.Count)
                            {
                                return queue[position + 1];
                            }
                        }
                        return queue.FirstOrDefault();
                    }
                    finally
                    {
                        this.Semaphore.Release();
                    }
                }
            }
            return null;
        }

        public int GetPosition(PlaylistItem playlistItem)
        {
            var position = -1;
            if (this.Queue.Count > 0)
            {
                var playlist = this.GetPlaylist(playlistItem);
                if (playlist != null)
                {
                    var queue = default(List<PlaylistItem>);
                    if (this.Queue.TryGetValue(playlist, out queue))
                    {
                        //TODO: We should use WaitAsync for >NET40.
                        this.Semaphore.Wait();
                        try
                        {
                            position = queue.IndexOf(playlistItem);
                        }
                        finally
                        {
                            this.Semaphore.Release();
                        }
                    }
                }
            }
            return position;
        }
    }
}
