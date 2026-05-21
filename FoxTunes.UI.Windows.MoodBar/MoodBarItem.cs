using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    [Serializable]
    public class MoodBarItem
    {
        const int ERROR_CAPACITY = 10;

        private MoodBarItem()
        {
            this.Id = Guid.NewGuid();
            this._Errors = new List<string>(ERROR_CAPACITY);
        }

        public const int PROGRESS_NONE = 0;

        public const int PROGRESS_COMPLETE = 100;

        public Guid Id { get; private set; }

        public string FileName { get; private set; }

        public MoodBarGenerator.MoodBarGeneratorData Data { get; set; }

        private IList<string> _Errors { get; set; }

        public IEnumerable<string> Errors
        {
            get
            {
                return this._Errors;
            }
            set
            {
                this._Errors = new List<string>(value);
            }
        }

        public int Progress { get; set; }

        public MoodBarItemStatus Status { get; set; }

        public void AddError(string error)
        {
            this._Errors.Add(error);
            if (this._Errors.Count > ERROR_CAPACITY)
            {
                this._Errors.RemoveAt(0);
            }
        }

        public static MoodBarItem FromFileData(IFileData fileData)
        {
            var moodBarItem = new MoodBarItem()
            {
                FileName = fileData.FileName
            };
            return moodBarItem;
        }
    }

    public enum MoodBarItemStatus : byte
    {
        None,
        Processing,
        Complete,
        Cancelled,
        Failed
    }
}
