using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;

namespace ChatSpyPlugin
{
    public class PluginCore : IRequestPlugin
    {
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Chat spy";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription() {
            return "Allows you to inspect the chat of an individual bot.";
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
        public void OnLoad(int version, int subversion, int buildversion) {

        }

        /// <summary>
        /// Called once the plugin is enabled.
        /// Meaning the start methods will be
        /// called when needed.
        /// </summary>
        public void OnEnabled() {

        }
        
        /// <summary>
        /// Called once the plugin is disabled.
        /// Meaning the start methods will not be
        /// called.
        /// </summary>
        public void OnDisabled() {

        }

        /// <summary>
        /// The plugin should be stopped.
        /// </summary>
        public void Stop() {

        }

        /// <summary>
        /// Return an instance of this plugin.
        /// </summary>
        /// <returns></returns>
        public IPlugin Copy() {
            return (IRequestPlugin)MemberwiseClone();
        }

        /// <summary>
        /// Called once a "player" logs
        /// in to the server.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PluginResponse OnStart(IPlayer player) {

            ShowForm(player);
            return new PluginResponse(true);
        }

        /// <summary>
        /// Called once the user requested
        /// for the plugin to start on this player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PluginResponse OnRequest(IPlayer player) {

            ShowForm(player);
            return new PluginResponse(true);
        }

        /// <summary>
        /// Called once the user requested
        /// for the plugin to start on these players.
        /// (This should not limit you to handle all player.
        /// You can choose to handle each seperartly)
        /// </summary>
        /// <param name="players"></param>
        /// <returns></returns>
        public PluginResponse OnRequest(IPlayer[] players) {

            for (int i = 0; i < players.Length; i++)
                OnRequest(players[i]);
            return new PluginResponse(true);
        }

        private void ShowForm(IPlayer player) {

            var chatForm = new ChatForm(player.status.username, player);

            Thread thread = new Thread(ApplicationRunProc);
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start(chatForm);
        }

        private static void ApplicationRunProc(object state) {
            Application.Run(state as Form);
        }

        /// <summary>
        /// All requestable functions should
        /// be storedh ere.
        /// </summary>
        public IRequestFunction[] functions { get; set; } = new IRequestFunction[]
        {
            new EnableFunction(),
        };
    }

    public class EnableFunction : IRequestFunction
    {
        /// <summary>
        /// Name of this function.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Enable";
        }

        /// <summary>
        /// Called once the user requested
        /// for the plugin to start on this player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PluginResponse OnRequest(IPlayer player) {
            ShowForm(player);
            return new PluginResponse(true);
        }

        /// <summary>
        /// Called once the user requested
        /// for the plugin to start on these players.
        /// (This should not limit you to handle all player.
        /// You can choose to handle each seperartly)
        /// </summary>
        /// <param name="players"></param>
        /// <returns></returns>
        public PluginResponse OnRequest(IPlayer[] players) {
            for (int i = 0; i < players.Length; i++)
                OnRequest(players[i]);
            return new PluginResponse(true);
        }

        private void ShowForm(IPlayer player)
        {

            var chatForm = new ChatForm(player.status.username, player);

            Thread thread = new Thread(ApplicationRunProc);
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start(chatForm);
        }

        private static void ApplicationRunProc(object state)
        {
            Application.Run(state as Form);
        }
    }
}
