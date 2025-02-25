namespace FoxTunes.Interfaces
{
    public interface ICoreScripts : IBaseComponent
    {
        string Utils { get; }

        string Artist { get; }

        string Artist_Album { get; }

        string Disk_Track_Title { get; }

        string Duration { get; }

        string Codec { get; }

        string Genre { get; }

        string Title_Performer { get; }

        string Track { get; }

        string Year_Album { get; }

        string Rating { get; }

        string Artist_Title_BPM { get; }
    }
}
