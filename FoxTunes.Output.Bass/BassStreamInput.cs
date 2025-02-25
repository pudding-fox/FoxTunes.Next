using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public abstract class BassStreamInput : BassStreamComponent, IBassStreamInput
    {
        public const int POSITION_INDETERMINATE = -1;

        protected BassStreamInput(IBassStreamPipeline pipeline, BassFlags flags) : base(pipeline, flags)
        {

        }

        public virtual IEnumerable<Type> SupportedProviders
        {
            get
            {
                return new[]
                {
                    typeof(IBassStreamProvider)
                };
            }
        }

        public abstract IEnumerable<int> Queue { get; }

        public virtual bool PreserveBuffer
        {
            get
            {
                return false;
            }
        }

        public override bool IsActive
        {
            get
            {
                return true;
            }
        }

        public bool CheckFormat(BassOutputStream stream)
        {
            var inputType = this.GetType();
            var providerType = stream.Provider.GetType();
            if (!stream.Provider.SupportedInputs.Any(type => type.IsAssignableFrom(inputType)))
            {
                Logger.Write(this, LogLevel.Debug, "Provider \"{0}\" does not support input \"{1}\".", providerType.Name, inputType.Name);
                return false;
            }
            if (!this.SupportedProviders.Any(type => type.IsAssignableFrom(providerType)))
            {
                Logger.Write(this, LogLevel.Debug, "Input \"{0}\" does not support provider \"{1}\".", inputType.Name, providerType.Name);
                return false;
            }
            return this.OnCheckFormat(stream);
        }

        protected virtual bool OnCheckFormat(BassOutputStream stream)
        {
            var rate = default(int);
            var channels = default(int);
            var flags = default(BassFlags);
            this.GetFormat(out rate, out channels, out flags);
            return rate == stream.Rate && channels == stream.Channels;
        }

        public override void Connect(IBassStreamComponent previous)
        {
            throw new NotImplementedException();
        }

        public abstract void Connect(BassOutputStream stream);

        public abstract bool Contains(BassOutputStream stream);

        public abstract int Position(BassOutputStream stream);

        public abstract bool Add(BassOutputStream stream, Action<BassOutputStream> callBack);

        public abstract bool Remove(BassOutputStream stream, Action<BassOutputStream> callBack);

        public abstract void Reset();
    }
}
