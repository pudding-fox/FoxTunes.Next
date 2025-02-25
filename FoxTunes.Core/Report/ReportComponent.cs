using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class ReportComponent : BaseComponent, IReportComponent
    {
        public const string ACCEPT = "A2B9D0A2-239B-4ACB-9A6F-3AB031188A09";

        public abstract string Title { get; }

        protected virtual void OnTitleChanged()
        {
            if (this.TitleChanged != null)
            {
                this.TitleChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Title");
        }

        public event EventHandler TitleChanged;

        public abstract string Description { get; }

        protected virtual void OnDescriptionChanged()
        {
            if (this.DescriptionChanged != null)
            {
                this.DescriptionChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Description");
        }

        public event EventHandler DescriptionChanged;

        public abstract string[] Headers { get; }

        protected virtual void OnHeadersChanged()
        {
            if (this.HeadersChanged != null)
            {
                this.HeadersChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Headers");
        }

        public event EventHandler HeadersChanged;

        public abstract IEnumerable<IReportComponentRow> Rows { get; }

        protected virtual void OnRowsChanged()
        {
            if (this.RowsChanged != null)
            {
                this.RowsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Rows");
        }

        public event EventHandler RowsChanged;

        public virtual bool IsDialog
        {
            get
            {
                return false;
            }
        }

        public virtual IEnumerable<string> InvocationCategories
        {
            get
            {
                return Enumerable.Empty<string>();
            }
        }

        public virtual IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                return Enumerable.Empty<IInvocationComponent>();
            }
        }

        public virtual Task InvokeAsync(IInvocationComponent component)
        {
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public abstract class ReportComponentRow : BaseComponent, IReportComponentRow
        {
            public const string SELECT = "6631B189-1D2B-42F6-9984-87ECEA24D7FB";

            public const string ACTIVATE = "4F1B5CF2-8302-42A9-B7CB-D4306FF124D2";

            public abstract string[] Values { get; }

            protected virtual void OnValuesChanged()
            {
                if (this.ValuesChanged != null)
                {
                    this.ValuesChanged(this, EventArgs.Empty);
                }
                this.OnPropertyChanged("Values");
            }

            public event EventHandler ValuesChanged;

            public virtual IEnumerable<string> InvocationCategories
            {
                get
                {
                    return Enumerable.Empty<string>();
                }
            }

            public virtual IEnumerable<IInvocationComponent> Invocations
            {
                get
                {
                    return Enumerable.Empty<IInvocationComponent>();
                }
            }

            public virtual Task InvokeAsync(IInvocationComponent component)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
        }
    }
}
