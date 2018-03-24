using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Classes.Base;

namespace ChatSpyPlugin
{
    public partial class ChatForm : Form
    {
        private IPlayer player;
        public ChatForm(string name, IPlayer player) {
            InitializeComponent();

            this.AcceptButton = Chat_Send;
            this.KeyDown += (sender, args) => {
                args.SuppressKeyPress = true; // Disable sound.
            };
            Chat_Box.KeyDown += (sender, args) => {
                args.SuppressKeyPress = true; // Disable sound.
            };

            this.player = player;

            player.events.onChat += OnChatMessage;
            player.events.onDisconnected +=
                (player1, reason) =>
                {
                    if (!this.IsDisposed && !this.Disposing)
                        this.BeginInvoke((MethodInvoker) delegate
                        {
                            if (!this.IsDisposed && !this.Disposing) this.Close();
                        });
                };

            this.Text = name + "'s chat";
        }

        private void OnChatMessage(IPlayer player, IChat message, byte position) {

            //If this isn't the chatbox then ignore it.
            if (position > 1) return;

            //Try-cathc in case the form is already
            //disposed.
            try
            {
                this.BeginInvoke((MethodInvoker) delegate
                {
                    if (this.IsDisposed || this.Disposing || !this.Log(message.Parsed)) {
                        if (player.events != null)
                            //Unhook, form closed.
                            player.events.onChat -= OnChatMessage;
                    }
                    else {

                        // set the current caret position to the end
                        Chat_Box.SelectionStart = Chat_Box.Text.Length;
                        // scroll it automatically
                        Chat_Box.ScrollToCaret();
                    }
                });
            }
            catch {

                //The form is already disposed, unhook.
                player.events.onChat -= OnChatMessage;
            }
        }

        private Queue<string> logQueue = new Queue<string>();
        private const int logMax = 100;

        public bool Log(string logText) {

            if (Chat_Box.IsDisposed || Chat_Box.Disposing) return false;

            // this should only ever run for 1 loop as you should never go over logMax
            // but if you accidentally manually added to the logQueue - then this would
            // re-adjust you back down to the desired number of log items.
            while (logQueue.Count > logMax - 1)
                logQueue.Dequeue();

            logQueue.Enqueue(logText);
            Chat_Box.Text = string.Join(Environment.NewLine,
                logQueue.ToArray());
            
            return true;
        }

        private void Chat_Box_KeyDown(object sender, KeyEventArgs e)
        {
            //Check if the user hit the "submit" key.
            if (e.KeyCode != Keys.Enter) return;

            //Do not send empty messages.
            if (string.IsNullOrWhiteSpace(Chat_Message.Text)) return;

            player?.functions?.Chat(Chat_Message.Text);
            Chat_Message.Clear();
        }

        private void Chat_Send_Click(object sender, EventArgs e) {
            Chat_Box_KeyDown(sender, new KeyEventArgs(Keys.Enter));
        }
    }
}
