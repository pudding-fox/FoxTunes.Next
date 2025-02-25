﻿using System;
using System.ComponentModel;
using System.Windows;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for PlaylistManagerWindow.xaml
    /// </summary>
    public partial class PlaylistManagerWindow : WindowBase
    {
        public const string ID = "4CFF69B2-62F8-4689-BC02-6FD843E73BBC";

        public PlaylistManagerWindow()
        {
            if (!global::FoxTunes.Properties.Settings.Default.PlaylistManagerWindowBounds.IsEmpty())
            {
                if (ScreenHelper.WindowBoundsVisible(global::FoxTunes.Properties.Settings.Default.PlaylistManagerWindowBounds))
                {
                    this.Left = global::FoxTunes.Properties.Settings.Default.PlaylistManagerWindowBounds.Left;
                    this.Top = global::FoxTunes.Properties.Settings.Default.PlaylistManagerWindowBounds.Top;
                }
                this.Width = global::FoxTunes.Properties.Settings.Default.PlaylistManagerWindowBounds.Width;
                this.Height = global::FoxTunes.Properties.Settings.Default.PlaylistManagerWindowBounds.Height;
            }
            else
            {
                this.Width = 600;
                this.Height = 400;
            }
            if (double.IsNaN(this.Left) || double.IsNaN(this.Top))
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            this.InitializeComponent();
        }

        public override string Id
        {
            get
            {
                return ID;
            }
        }

        protected virtual void OnCommandExecuted(object sender, ButtonExtensions.CommandExecutedEventArgs e)
        {
            if (string.Equals(e.Behaviour, ButtonExtensions.COMMAND_BEHAVIOUR_DISMISS, StringComparison.OrdinalIgnoreCase))
            {
                this.Close();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            global::FoxTunes.Properties.Settings.Default.PlaylistManagerWindowBounds = this.RestoreBounds;
            global::FoxTunes.Properties.Settings.Default.Save();
            base.OnClosing(e);
        }
    }
}
