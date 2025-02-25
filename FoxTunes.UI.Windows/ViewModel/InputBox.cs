using FoxTunes.Interfaces;
using System;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class InputBox : ViewModelBase
    {
        private string _Prompt { get; set; }

        public string Prompt
        {
            get
            {
                return this._Prompt;
            }
            set
            {
                this._Prompt = value;
                this.OnPromptChanged();
            }
        }

        protected virtual void OnPromptChanged()
        {
            if (this.PromptChanged != null)
            {
                this.PromptChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Prompt");
        }

        public event EventHandler PromptChanged;

        private InputBoxPrompt _Result { get; set; }

        public InputBoxPrompt Result
        {
            get
            {
                return this._Result;
            }
            set
            {
                this._Result = value;
                this.OnResultChanged();
            }
        }

        protected virtual void OnResultChanged()
        {
            if (this.ResultChanged != null)
            {
                this.ResultChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Result");
        }

        public event EventHandler ResultChanged;

        private UserInterfacePromptFlags _Flags { get; set; }

        public UserInterfacePromptFlags Flags
        {
            get
            {
                return this._Flags;
            }
            set
            {
                this._Flags = value;
                this.OnFlagsChanged();
            }
        }

        protected virtual void OnFlagsChanged()
        {
            if (this.Flags.HasFlag(UserInterfacePromptFlags.Password))
            {
                this.Result = new InputBoxPasswordPrompt();
            }
            else
            {
                this.Result = new InputBoxTextPrompt();
            }
            if (this.FlagsChanged != null)
            {
                this.FlagsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Flags");
        }

        public event EventHandler FlagsChanged;

        protected override Freezable CreateInstanceCore()
        {
            return new InputBox();
        }
    }

    public abstract class InputBoxPrompt : ViewModelBase
    {
        public abstract string GetResult();

        public abstract void SetResult(string result);
    }

    public class InputBoxTextPrompt : InputBoxPrompt
    {
        private string _Value { get; set; }

        public string Value
        {
            get
            {
                return this._Value;
            }
            set
            {
                this._Value = value;
                this.OnValueChanged();
            }
        }

        protected virtual void OnValueChanged()
        {
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Value");
        }

        public event EventHandler ValueChanged;

        public override string GetResult()
        {
            return this.Value;
        }

        public override void SetResult(string result)
        {
            this.Value = result;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new InputBoxTextPrompt();
        }
    }

    public class InputBoxPasswordPrompt : InputBoxPrompt
    {
        private string _Value { get; set; }

        public string Value
        {
            get
            {
                return this._Value;
            }
            set
            {
                this._Value = value;
                this.OnValueChanged();
            }
        }

        protected virtual void OnValueChanged()
        {
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Value");
        }

        public event EventHandler ValueChanged;

        public override string GetResult()
        {
            return this.Value;
        }

        public override void SetResult(string result)
        {
            this.Value = result;
        }

        protected override Freezable CreateInstanceCore()
        {
            return new InputBoxPasswordPrompt();
        }
    }
}
