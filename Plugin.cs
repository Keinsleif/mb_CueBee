using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private PluginInfo about = new PluginInfo();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApiInterface = new MusicBeeApiInterface();
            mbApiInterface.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "CueBee";
            about.Description = "Plugin for using MusicBee for PA usage";
            about.Author = "Keinsleif";
            about.TargetApplication = "";   //  the name of a Plugin Storage device or panel header for a dockable panel
            about.Type = PluginType.General;
            about.VersionMajor = 1;  // your plugin version
            about.VersionMinor = 0;
            about.Revision = 1;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = ReceiveNotificationFlags.PlayerEvents;
            about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function
            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label();
                prompt.AutoSize = true;
                prompt.Location = new Point(0, 0);
                prompt.Text = "prompt:";
                TextBox textBox = new TextBox();
                textBox.Bounds = new Rectangle(60, 0, 100, textBox.Height);
                configPanel.Controls.AddRange(new Control[] { prompt, textBox });
            }
            return false;
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:
                    // perform startup initialisation
                    mbApiInterface.MB_RegisterCommand("Player: Volume FadeOut", (object sender, EventArgs e) => { FadeOut(sender, e).ContinueWith(_ => {; }); });
                    mbApiInterface.MB_RegisterCommand("Player: Volume FadeOut and Play Next", (object sender, EventArgs e) => { FadeOutAndPlayNext(sender, e).ContinueWith(_ => {; }); });
                    break;
            }
        }

        private async Task FadeOut(object sender, EventArgs e)
        {
            float volume = mbApiInterface.Player_GetVolume();
            if (volume == 0) { return; }
            int delay = Convert.ToInt32(Math.Round(3000 / (volume * 100)));
            for (float i = volume; i > 0; i -= 0.01F)
            {
                mbApiInterface.Player_SetVolume(i);
                await Task.Delay(delay);
            }
            mbApiInterface.Player_Stop();
            mbApiInterface.Player_SetVolume(volume);
        }

        private async Task FadeOutAndPlayNext(object sender, EventArgs e)
        {
            await FadeOut(sender, e);
            mbApiInterface.Player_PlayNextTrack();
        }
    }
}