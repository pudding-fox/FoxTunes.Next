using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    [Serializable]
    public class EncoderItem
    {
        const int ERROR_CAPACITY = 10;

        private EncoderItem()
        {
            this.Id = Guid.NewGuid();
            this._Errors = new List<string>(ERROR_CAPACITY);
        }

        public const int PROGRESS_NONE = 0;

        public const int PROGRESS_COMPLETE = 100;

        public Guid Id { get; private set; }

        public string InputFileName { get; private set; }

        public string OutputFileName { get; private set; }

        public string Profile { get; private set; }

        public int Bitrate { get; private set; }

        public int Channels { get; private set; }

        public int SampleRate { get; private set; }

        public int BitsPerSample { get; private set; }

        private IList<string> _Errors { get; set; }

        public IEnumerable<string> Errors
        {
            get
            {
                return this._Errors;
            }
        }

        public int Progress { get; set; }

        public EncoderItemStatus Status { get; set; }

        public void AddError(string error)
        {
            this._Errors.Add(error);
            if (this._Errors.Count > ERROR_CAPACITY)
            {
                this._Errors.RemoveAt(0);
            }
        }

        public static EncoderItem Create(string inputFileName, string outputFileName, IList<MetaDataItem> metaDatas, string profile)
        {
            var encoderItem = new EncoderItem()
            {
                InputFileName = inputFileName,
                OutputFileName = outputFileName,
                Profile = profile
            };
            if (metaDatas != null)
            {
                var metaData = default(IDictionary<string, string>);
                lock (metaDatas)
                {
                    metaData = metaDatas.ToDictionary2();
                }
                encoderItem.Bitrate = Convert.ToInt32(metaData.GetValueOrDefault(CommonProperties.AudioBitrate));
                encoderItem.Channels = Convert.ToInt32(metaData.GetValueOrDefault(CommonProperties.AudioChannels));
                encoderItem.SampleRate = Convert.ToInt32(metaData.GetValueOrDefault(CommonProperties.AudioSampleRate));
                encoderItem.BitsPerSample = Convert.ToInt32(metaData.GetValueOrDefault(CommonProperties.BitsPerSample));
            }
            return encoderItem;
        }

        public static bool WasSkipped(EncoderItem encoderItem)
        {
            if (encoderItem.Status != EncoderItemStatus.Failed)
            {
                return false;
            }
            if (encoderItem.Errors == null)
            {
                return false;
            }
            var errors = encoderItem.Errors.ToArray();
            if (errors.Length != 1 || string.IsNullOrEmpty(errors[0]))
            {
                return false;
            }
            if (!errors[0].Contains("already exists", true))
            {
                return false;
            }
            return true;
        }
    }

    public enum EncoderItemStatus : byte
    {
        None,
        Processing,
        Complete,
        Cancelled,
        Failed
    }
}
