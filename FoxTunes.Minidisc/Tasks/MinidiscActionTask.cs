using FoxTunes.Interfaces;
using MD.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FoxTunes
{
    public abstract class MinidiscActionTask : MinidiscTask
    {
        protected MinidiscActionTask()
        {
            this.Status = new StatusTracker();
            this.Status.Updated += this.OnUpdated;
        }

        public StatusTracker Status { get; private set; }

        public IResult Result { get; private set; }

        protected virtual void OnUpdated(object sender, StatusEventArgs e)
        {
            this.Description = e.Message;
            this.Position = e.Position;
            this.Count = e.Count;
        }

        protected virtual void ApplyActions(IDevice device, IActions actions)
        {
            Logger.Write(this, LogLevel.Debug, "Applying actions..");
            this.Result = this.DiscManager.ApplyActions(device, actions, this.Status, true);
            if (this.Result.Status != ResultStatus.Success)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to apply actions: {0}", this.Result.Message);
                throw new Exception(this.Result.Message);
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Successfully applied actions.");
            }
        }

        public class StatusTracker : BaseComponent, IStatus
        {
            public StatusTracker()
            {
                this.Transfers = new ConcurrentDictionary<string, Tuple<int, int>>(StringComparer.OrdinalIgnoreCase);
                this.Encodes = new ConcurrentDictionary<string, Tuple<int, int>>(StringComparer.OrdinalIgnoreCase);
            }

            public string ActionMessage { get; private set; }

            public int ActionPosition { get; private set; }

            public int ActionCount { get; private set; }

            public ConcurrentDictionary<string, Tuple<int, int>> Transfers { get; private set; }

            public ConcurrentDictionary<string, Tuple<int, int>> Encodes { get; private set; }

            public void Update(string message, int position, int count, StatusType type)
            {
                switch (type)
                {
                    case StatusType.Action:
                        this.OnAction(message, position, count);
                        break;
                    case StatusType.Transfer:
                        this.OnTransfer(message, position, count);
                        break;
                    case StatusType.Encode:
                        this.OnEncode(message, position, count);
                        break;
                }
                this.OnUpdated();
            }

            protected virtual void OnAction(string message, int position, int count)
            {
                this.ActionMessage = message;
                this.ActionPosition = position;
                this.ActionCount = count;
            }

            protected virtual void OnTransfer(string message, int position, int count)
            {
                if (position < count)
                {
                    this.Transfers.AddOrUpdate(message, Tuple.Create(position, count));
                }
                else
                {
                    this.Transfers.TryRemove(message);
                }
            }

            protected virtual void OnEncode(string message, int position, int count)
            {
                if (position < count)
                {
                    this.Encodes.AddOrUpdate(message, Tuple.Create(position, count));
                }
                else
                {
                    this.Encodes.TryRemove(message);
                }
            }

            protected virtual void OnUpdated()
            {
                if (this.Updated == null)
                {
                    return;
                }
                this.Updated(this, this.GetStatus());
            }

            protected virtual StatusEventArgs GetStatus()
            {
                var message = default(string);
                if (this.Transfers.Count > 0)
                {
                    message = string.Format(Strings.MinidiscActionTask_Transferring, this.GetProgress(this.Transfers.Values));
                }
                else if (this.Encodes.Count > 0)
                {
                    message = string.Format(Strings.MinidiscActionTask_Encoding, this.GetProgress(this.Encodes.Values));
                }
                else
                {
                    message = this.ActionMessage;
                }
                var position = this.ActionPosition;
                var count = this.ActionCount;
                return new StatusEventArgs(message, position, count, StatusType.None);
            }

            protected virtual int GetProgress(IEnumerable<Tuple<int, int>> values)
            {
                var position = default(int);
                var count = default(int);
                foreach (var value in values)
                {
                    position += value.Item1;
                    count += value.Item2;
                }
                if (count == 0)
                {
                    return 0;
                }
                return Convert.ToInt32((Convert.ToSingle(position) / count) * 100);
            }

            public event StatusEventHandler Updated;
        }
    }
}
