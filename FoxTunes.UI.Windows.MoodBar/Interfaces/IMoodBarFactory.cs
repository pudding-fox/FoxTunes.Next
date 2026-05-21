using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    public interface IMoodBarFactory : IStandardComponent
    {
        IMoodBar CreateMoodBar(IEnumerable<MoodBarItem> moodBarItems);
    }
}
