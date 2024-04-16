using System;
using System.IO;
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
            about.ConfigurationPanelHeight = 20;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            Config.Instance.Load(Path.Combine(dataPath, about.Name + ".xml"));
            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            Config.Instance.Load(Path.Combine(dataPath, about.Name + ".xml"));
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                Panel configPanel = (Panel)Panel.FromHandle(panelHandle);
                Label prompt = new Label
                {
                    AutoSize = true,
                    Location = new Point(0, 0),
                    Text = "FadeOut Time (ms):"
                };
                TextBox textBox = new TextBox
                {
                    Bounds = new Rectangle(prompt.Width + 10, 0, 120, prompt.Height),
                    Text = Config.Instance.FadeOutTimeMills.ToString(),
                    ShortcutsEnabled = false,
                };

                textBox.MouseClick += (sender, e) =>
                {
                    textBox.SelectAll();
                };

                textBox.TextChanged += (sender, e) =>
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(textBox.Text, "  ^ [0-9]"))
                    {
                        textBox.Text = Config.Instance.DefaultFadeOutTimeMills.ToString();
                    }
                    else if (Convert.ToInt32(textBox.Text) == 0)
                    {
                        textBox.Text = Config.Instance.DefaultFadeOutTimeMills.ToString();
                    }
                    Config.Instance.FadeOutTimeMills = Convert.ToInt32(textBox.Text);
                };
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
            Config.Instance.Save(Path.Combine(dataPath, about.Name + ".xml"));
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
            string dataPath = mbApiInterface.Setting_GetPersistentStoragePath();
            File.Delete(Path.Combine(dataPath, about.Name + ".xml"));
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
                    mbApiInterface.MB_RegisterCommand("Player: Volume FadeOut", (object sender, EventArgs e) => { FadeOut().ContinueWith(_ => {; }); });
                    mbApiInterface.MB_RegisterCommand("Player: Volume FadeOut and Play Next", (object sender, EventArgs e) => { FadeOutAndPlayNext().ContinueWith(_ => {; }); });
                    break;
            }
        }

        private async Task FadeOut()
        {
            float volume = mbApiInterface.Player_GetVolume();
            if (volume == 0) { return; }
            int delay = Convert.ToInt32(Math.Round(Config.Instance.FadeOutTimeMills / (volume * 100)));
            for (float i = volume; i > 0; i -= 0.01F)
            {
                if (!mbApiInterface.Player_GetPlayState().Equals(PlayState.Playing))
                {
                    mbApiInterface.Player_SetVolume(volume);
                    return;
                }
                mbApiInterface.Player_SetVolume(i);
                await Task.Delay(delay);
            }
            mbApiInterface.Player_Stop();
            mbApiInterface.Player_SetVolume(volume);
        }

        private async Task FadeOutAndPlayNext()
        {
            await FadeOut();
            mbApiInterface.Player_PlayNextTrack();
        }
    }
}