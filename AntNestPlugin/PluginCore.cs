using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntNestPlugin.Nest;
using AntNestPlugin.Workers;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Classes.Base;

namespace AntNestPlugin
{
    public class PluginCore: IStartPlugin
    {
        /// <summary>
        /// The global nest network, that each
        /// account has to join.
        /// </summary>
        public static NestManager Nest;
        /// <summary>
        /// Working role for this bot.
        /// </summary>
        private INestWorker Role;

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return "AntNest";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription()
        {
            return "Mines tunnels underground.";
        }

        /// <summary>
        /// Author of the plugin.
        /// </summary>
        /// <returns></returns>
        public PluginAuthor GetAuthor()
        {
            return new PluginAuthor("OnlyQubes");
        }

        /// <summary>
        /// Version of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetVersion()
        {
            return "1.00.00";
        }

        /// <summary>
        /// All settings should be stored here.
        /// (NULL if there shouldn't be any settings)
        /// </summary>
        public IPluginSetting[] Setting { get; set; } = {
        };

        /// <summary>
        /// Called once the plugin is loaded.
        /// (Params are the version of the program)
        /// (This is not reliable as if "Load plugins" 
        /// isn't enabled this will not be called)
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
            // Create a new nest each time
            // the plugin is enabled.
            Nest = new NestManager();
        }

        /// <summary>
        /// Called once the plugin is disabled.
        /// Meaning the start methods will not be
        /// called.
        /// </summary>
        public void OnDisabled() { }

        /// <summary>
        /// The plugin should be stopped.
        /// </summary>
        public void Stop()
        {
            stopToken.Stop();
        }
        private CancelToken stopToken = new CancelToken();

        /// <summary>
        /// Return an instance of this plugin.
        /// </summary>
        /// <returns></returns>
        public IPlugin Copy()
        {
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
        public PluginResponse OnStart(IPlayer player)
        {

            //Check if bot settings are valid.
            if (!player.settings.loadWorld) {
                Console.WriteLine("[AntNest] 'Load world' must be enabled.");
                return new PluginResponse(false, "'Load world' must be enabled.");
            }
            stopToken.Reset();

            // Attempt to join the nest.
            this.Role = Nest.JoinNest(player);

            // Hook events.
            player.events.onDisconnected += EventDisconnected;
            player.events.onTick += Tick;

            return new PluginResponse(true);
        }

        private void Tick(IPlayer player) {

            // Check if the plugin was 
            // stopped on this bot.
            if (stopToken.stopped) {
                player.events.onTick -= Tick;
                Nest.LeaveNest(player);
                return;
            }

            // Do work.
            this.Role.Work();
        }

        private void EventDisconnected(IPlayer player, string reason) {
            // Depart nest.
            Nest.LeaveNest(player);
        }
    }
}
