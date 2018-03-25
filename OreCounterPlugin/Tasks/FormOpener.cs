using System;
using System.Threading;
using System.Windows.Forms;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;

namespace OreCounterPlugin.Tasks
{
    public class FormOpener : ITask
    {
        public override bool Exec() { return true; }

        public override void Start() {
            RegisterPlayer();
        }

        private static OreCounterForm _form;
        public static void ShowForm() {

            if (IsFormValid()) return;
            _form = new OreCounterForm();

            bool loaded = false;
            _form.Shown += (sender, args) => loaded = true;
            var thread = new Thread(() => {
                _form.Show();
                Application.Run(_form);
            });
            thread.IsBackground = true;
            thread.Start();

            while(loaded) Thread.Sleep(1);
        }

        public static void CloseForm() {

            if (!IsFormValid()) return;
            _form.Invoke(new Action(() => {
                _form.Close();
            }));
            _form = null;
        }

        public void RegisterPlayer() {
            if (!IsFormValid()) {Console.WriteLine("Form is invalid"); return;}
            _form.AddPlayer(player);
            player.events.onDisconnected += (player1, reason) => RegisterDisconnect();
            player.events.onInventoryChanged += (player1, changed, removed, id, difference, slot) =>
            {
                if (!removed && difference > 0) {
                    InventoryIncrease(id, difference);
                }
            };
        }

        public void RegisterDisconnect() {
            if (!IsFormValid()) return;
            _form.DisconnectedPlayer(player);
        }

        private void InventoryIncrease(ushort id, int countDifference) {
            if (!IsFormValid()) return;
            _form.InventoryIncrease(player, id, countDifference);
        }

        private static bool IsFormValid() {
            return _form != null && !_form.Disposing && !_form.IsDisposed;
        }

    }
}