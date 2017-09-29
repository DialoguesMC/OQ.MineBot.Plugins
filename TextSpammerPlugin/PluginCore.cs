using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity;

namespace TextSpammerPlugin
{
    public class PluginCore : IStartPlugin
    {
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Text file spammer";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription() {
            return "Spams messages from a text file.";
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
        public IPluginSetting[] Setting { get; set; } =
        {
            new PathSetting("Text file path", "Picks lines from the selected file to spam.", ""),
            new NumberSetting("Min delay", "", 0, 1000, 60*60*60),
            new NumberSetting("Max delay", "(-1 to always use 'Min delay')", -1, -1, 60*60*60),
            new BoolSetting("Anti-spam", "Should random numbers be added at the end?", false),
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
        public void OnLoad(int version, int subversion, int buildversion) {
        }

        /// <summary>
        /// Called once the plugin is enabled.
        /// Meaning the start methods will be
        /// called when needed.
        /// </summary>
        public void OnEnabled() {
            stopToken.Reset();
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
            stopToken.Stop();
        }

        private CancelToken stopToken = new CancelToken();

        /// <summary>
        /// Return an instance of this plugin.
        /// </summary>
        /// <returns></returns>
        public IPlugin Copy() {
            return (IStartPlugin) MemberwiseClone();
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
            
            player.events.onTick += OnTick;

            // Attempt to get the messages.
            if(string.IsNullOrWhiteSpace(Setting[0].Get<string>()) || !File.Exists(Setting[0].Get<string>()))
                return new PluginResponse(false, "Invalid text file selected.");
            Messages = File.ReadAllLines(Setting[0].Get<string>());
            if(Messages.Length == 0)
                return new PluginResponse(false, "Invalid text file selected.");

            return new PluginResponse(true);
        }

        private static Random rnd = new Random();
        private DateTime NextMessage = DateTime.Now;
        private string[] Messages;

        private void OnTick(IPlayer player) {
            //Check if we should un-hook, in case the
            //plugin is stopped.
            if (stopToken.stopped) {
                player.events.onTick -= OnTick;
                return;
            }

            if (DateTime.Now.Subtract(NextMessage).TotalMilliseconds > 0) {

                // Schedule next message.
                NextMessage =
                    DateTime.Now.AddMilliseconds(Setting[2].Get<int>() == -1 // Check if Max delay is disabled.
                        ? Setting[2].Get<int>() // Max delay disabled, use min delay.
                        : rnd.Next(Setting[1].Get<int>(), Setting[2].Get<int>()));

                // Post chat message.
                player.functions.Chat(Messages[rnd.Next(0, Messages.Length)] + // Pick random message.
                                      (Setting[3].Get<bool>() ? rnd.Next(0, 9999).ToString() : ""));
            }
        }
    }
}
