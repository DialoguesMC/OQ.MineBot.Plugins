using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;

namespace ContainerViewerPlugin
{
    [Plugin(1, "Container Viewer", "Allows you to inspect the opened inventories of an individual bot.")]
    public class PluginCore : IRequestPlugin {
        public override void OnLoad(int version, int subversion, int buildversion) {
            ContainerForm.LoadImages();
        }

        public override IRequestFunction[] GetFunctions() {
            return new IRequestFunction[]
            {
                new EnableFunction(),
            };
        }
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

        private void ShowForm(IPlayer player) {
            var chatForm = new ContainerForm(player);

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
