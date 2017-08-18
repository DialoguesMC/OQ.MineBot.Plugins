using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity.Mob;
using OQ.MineBot.PluginBase.Classes.Entity.Player;
using OQ.MineBot.PluginBase.Utility;
using OQ.MineBot.Protocols.Classes.Base;

namespace GreeterPlugin
{
    public class PluginCore : IStartPlugin
    {
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return "Greeter";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription()
        {
            return "Sends a message once a player joins.";
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
            new StringSetting("Message once a player joins", "Use %new_player% for the name of the player", "Welcome %new_player%"),
            new NumberSetting("Min delay", "The minimum amount of time the bot has to wait before sending another message. (seconds)", 4, 1, 120, 1),
            new NumberSetting("Chance", "Chance that a new person will be greeted.", 100, 1, 100, 1),
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
        public void OnEnabled() { }

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
            if (player != null)
                player.entities.onNameAdded -= EntitiesOnOnNameAdded;
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
            if (!player.settings.loadEntities || !player.settings.loadPlayers) {
                Console.WriteLine("[Greeter] 'Load entities & load players' must be enabled.");
                return new PluginResponse(false, "'Load entities & load players' must be enabled.");
            }

            stopToken.Reset();
            this.player = player;
            this.start = DateTime.Now;
            player.entities.onNameAdded += EntitiesOnOnNameAdded;
            return new PluginResponse(true);
        }

        private void EntitiesOnOnNameAdded(UUID uuid) {

            //Do not trigger in the first 10 seconds
            //as we are still joining the server.
            //(all player names will be sent ar first)
            if (start.Subtract(DateTime.Now).TotalSeconds > -10) return;
            
            //Check if we should stop the plugin.
            if (stopToken.stopped || uuid.Name.Contains('§') || string.IsNullOrWhiteSpace(uuid.Name)) return;
            if (rnd.Next(0, 101) > Setting[2].Get<int>()) return;
            if (last.Subtract(DateTime.Now).TotalSeconds > Setting[1].Get<int>()) return; // Type only every X seconds
            last = DateTime.Now;

            player.functions.Chat(Setting[0].Get<string>().Replace("%new_player%", uuid.Name));
        }
        private Random rnd = new Random();
        private DateTime start;
        private DateTime last = DateTime.MinValue;
        private IPlayer player;
    }
}
