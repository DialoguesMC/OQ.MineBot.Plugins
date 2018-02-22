using System;
using System.Threading;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;

namespace OreCounterPlugin
{
    public class PluginCore : IStartPlugin
    {
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Ore counter";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription() {
            return "Keeps track of how many ores each bot has mined.";
        }

        /// <summary>
        /// Author of the plugin.
        /// </summary>
        /// <returns></returns>
        public PluginAuthor GetAuthor() {
            return new PluginAuthor("OnlyQubes");
        }

        /// <summary>
        /// Version of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetVersion() {
            return "1.00.00";
        }

        /// <summary>
        /// All settings should be stored here.
        /// (NULL if there shouldn't be any settings)
        /// </summary>
        public IPluginSetting[] Setting { get; set; }

        /// <summary>
        /// Called once the plugin is loaded.
        /// (Params are the version of the program)
        /// </summary>
        /// <param name="version"></param>
        /// <param name="subversion"></param>
        /// <param name="buildversion"></param>
        public void OnLoad(int version, int subversion, int buildversion) { }

        /// <summary>
        /// Called once the plugin is enabled.
        /// Meaning the start methods will be
        /// called when needed.
        /// </summary>
        public void OnEnabled() {
            ShowForm();
        }
        
        /// <summary>
        /// Called once the plugin is disabled.
        /// Meaning the start methods will not be
        /// called.
        /// </summary>
        public void OnDisabled() {
            CloseForm();
        }

        /// <summary>
        /// The plugin should be stopped.
        /// </summary>
        public void Stop() {
            RegisterDisconnect(_player);
        }

        /// <summary>
        /// Return an instance of this plugin.
        /// </summary>
        /// <returns></returns>
        public IPlugin Copy() {
            return (IStartPlugin)MemberwiseClone();
        }

        /// <summary>
        /// Called once a "player" logs
        /// in to the server.
        /// (This does not start a new thread, so
        /// if you want to do any long temn functions
        /// please start your own thread!)
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PluginResponse OnStart(IPlayer player) {

            //Check if bot settings are valid.
            if (!player.settings.loadInventory) {
                Console.WriteLine("[ShieldAura] 'Load inventory' must be enabled.");
                return new PluginResponse(false, "'Load inventory' must be enabled.");
            }

            _player = player;
            RegisterPlayer(player);
            return new PluginResponse(true);
        }
        private IPlayer _player;

        #region Form

        private OreCounterForm _form;
        public void ShowForm() {

            if (IsFormValid()) return;
            _form = new OreCounterForm();

            bool loaded = false;
            _form.Shown += (sender, args) => loaded = true; 
            _form.Show();

            while(loaded) Thread.Sleep(1);
        }

        public void CloseForm() {

            if (!IsFormValid()) return;
            _form.Close();
            _form = null;
        }

        public void RegisterPlayer(IPlayer player) {
            if (!IsFormValid()) return;
            _form.AddPlayer(player);
            player.events.onDisconnected += (player1, reason) => RegisterDisconnect(player);
            player.events.onInventoryChanged += (player1, changed, removed, id, difference) =>
            {
                if (!changed && !removed && difference > 0) {
                    InventoryIncrease(player, id, difference);
                }
            };
        }

        public void RegisterDisconnect(IPlayer player) {
            if (!IsFormValid()) return;
            _form.DisconnectedPlayer(player);
        }

        private void InventoryIncrease(IPlayer player, ushort id, int countDifference) {
            if (!IsFormValid()) return;
            _form.InventoryIncrease(player, id, countDifference);
        }

        private bool IsFormValid() {
            return _form != null && !_form.Disposing && !_form.IsDisposed;
        }


        #endregion
    }
}