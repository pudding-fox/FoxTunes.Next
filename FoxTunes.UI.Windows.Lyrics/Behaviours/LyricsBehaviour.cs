using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class LyricsBehaviour : StandardBehaviour, IInvocableComponent, IConfigurableComponent
    {
        public const string CATEGORY = "BB46B834-5372-440F-B75B-57FF0E473BB4";

        public const string EDIT = "0000";

        public const string LOOKUP = "0001";

        public LyricsBehaviour()
        {
            this.Providers = new List<ILyricsProvider>();
        }

        public IList<ILyricsProvider> Providers { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IOnDemandMetaDataProvider OnDemandMetaDataProvider { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public BooleanConfigurationElement AutoLookup { get; private set; }

        public BooleanConfigurationElement AutoScroll { get; private set; }

        public BooleanConfigurationElement WriteTags { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Providers.AddRange(ComponentRegistry.Instance.GetComponents<ILyricsProvider>());
            this.PlaybackManager = core.Managers.Playback;
            this.OnDemandMetaDataProvider = core.Components.OnDemandMetaDataProvider;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.READ_LYRICS_TAGS
            );
            this.AutoLookup = this.Configuration.GetElement<BooleanConfigurationElement>(
                LyricsBehaviourConfiguration.SECTION,
                LyricsBehaviourConfiguration.AUTO_LOOKUP
            );
            this.AutoScroll = this.Configuration.GetElement<BooleanConfigurationElement>(
                LyricsBehaviourConfiguration.SECTION,
                LyricsBehaviourConfiguration.AUTO_SCROLL
            );
            this.WriteTags = this.Configuration.GetElement<BooleanConfigurationElement>(
                LyricsBehaviourConfiguration.SECTION,
                LyricsBehaviourConfiguration.WRITE_TAGS
            );
            base.InitializeComponent(core);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return CATEGORY;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.Enabled.Value)
                {
                    if (this.PlaybackManager.CurrentStream != null)
                    {
                        yield return new InvocationComponent(
                            CATEGORY,
                            EDIT,
                            "Edit"
                        );
                        foreach (var provider in this.Providers)
                        {
                            yield return new InvocationComponent(
                                CATEGORY,
                                LOOKUP,
                                provider.Name,
                                path: "Lookup"
                            );
                        }
                    }
                    yield return new InvocationComponent(
                        CATEGORY,
                        this.AutoScroll.Id,
                        this.AutoScroll.Name,
                        attributes: (byte)(InvocationComponent.ATTRIBUTE_SEPARATOR | (this.AutoScroll.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE))
                    );
                    yield return new InvocationComponent(
                        CATEGORY,
                        this.AutoLookup.Id,
                        this.AutoLookup.Name,
                        attributes: this.AutoLookup.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                    );
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case EDIT:
                    return this.Edit();
                case LOOKUP:
                    var provider = this.Providers.FirstOrDefault(
                        _provider => string.Equals(_provider.Name, component.Name, StringComparison.OrdinalIgnoreCase)
                    );
                    if (provider != null)
                    {
                        return this.Lookup(provider, MetaDataUpdateType.User);
                    }
#if NET40
                    return TaskEx.FromResult(false);
#else
                    return Task.CompletedTask;
#endif
            }
            if (string.Equals(component.Id, this.AutoScroll.Id, StringComparison.OrdinalIgnoreCase))
            {
                this.AutoScroll.Toggle();
            }
            else if (string.Equals(component.Id, this.AutoLookup.Id, StringComparison.OrdinalIgnoreCase))
            {
                this.AutoLookup.Toggle();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public async Task Edit()
        {
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream == null)
            {
                return;
            }
            var playlistItem = outputStream.PlaylistItem;
            var metaDataItem = default(MetaDataItem);
            lock (playlistItem.MetaDatas)
            {
                metaDataItem = playlistItem.MetaDatas.FirstOrDefault(
                    element => string.Equals(element.Name, CommonMetaData.Lyrics, StringComparison.OrdinalIgnoreCase)
                );
            }
            var fileName = Path.Combine(
                Path.GetTempPath(),
                string.Format("Lyrics-{0}.txt", DateTime.Now.ToFileTimeUtc())
            );
            Logger.Write(this, LogLevel.Debug, "Editing lyrics for file \"{0}\": \"{1}\"", playlistItem.FileName, fileName);
            if (metaDataItem != null)
            {
                File.WriteAllText(fileName, metaDataItem.Value);
            }
            else
            {
                File.WriteAllText(fileName, string.Empty);
            }
            try
            {
                var process = Process.Start(fileName);
                await process.WaitForExitAsync().ConfigureAwait(false);
                if (process.ExitCode != 0)
                {
                    Logger.Write(this, LogLevel.Warn, "Process does not indicate success: Code = {0}", process.ExitCode);
                    return;
                }
                var flags = MetaDataUpdateFlags.None;
                if (this.WriteTags.Value)
                {
                    flags |= MetaDataUpdateFlags.WriteToFiles;
                }
                await this.OnDemandMetaDataProvider.SetMetaData(
                    new OnDemandMetaDataRequest(
                        CommonMetaData.Lyrics,
                        MetaDataItemType.Tag,
                        MetaDataUpdateType.User
                    ),
                    new OnDemandMetaDataValues(
                        new[]
                        {
                            new OnDemandMetaDataValue(playlistItem, File.ReadAllText(fileName))
                        },
                        flags
                    )
                ).ConfigureAwait(false);
            }
            finally
            {
                try
                {
                    File.Delete(fileName);
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to delete temp file \"{0}\":", fileName, e.Message);
                }
            }
        }

        public Task Lookup(ILyricsProvider provider, MetaDataUpdateType updateType)
        {
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var playlistItem = outputStream.PlaylistItem;
            if (this.OnDemandMetaDataProvider.IsSourceEnabled(CommonMetaData.Lyrics, MetaDataItemType.Tag))
            {
                return this.OnDemandMetaDataProvider.GetMetaData(
                    new[] { outputStream.PlaylistItem },
                    new OnDemandMetaDataRequest(
                        CommonMetaData.Lyrics,
                        MetaDataItemType.Tag,
                        updateType,
                        provider
                    )
                );
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return LyricsBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
