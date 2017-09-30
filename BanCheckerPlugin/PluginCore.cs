using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Permissions;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Utility;

namespace BanCheckerPlugin
{
    public class PluginCore: IStartPlugin
    {
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Ban checker";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription() {
            return "Checks if an account is banned on the server.";
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
            new PathSetting("Banned accounts", "Outputs banned accounts to this file.", ""),
            new PathSetting("Unbanned accounts", "Outputs unbanned accounts to this file.", ""),
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

            // Request permissions to hook low level events.
            var permission = EventPermissions.CheckPermissions("low-level");
            if(permission == false)
                DiscordHelper.Error("Not enough permissions, plugin requires 'All permissions'.", 1);
            EventPermissions.LowLevelHook(LowLevelEvents.OnServerInitialResponse, ServerResponse);

            bool exists = false;
            // Check if the files exist.
            if (!File.Exists(Setting[0].Get<string>())) {
                DiscordHelper.Alert("'Banned accounts' path not set.", 1);
            }
            else exists = true;
            if (!File.Exists(Setting[1].Get<string>())) {
                DiscordHelper.Alert("'Unbanned accounts' path not set.", 2);
            }
            else exists = true;

            if(!exists)
                DiscordHelper.Error("No output paths have been set.", 1);
        }

        private void ServerResponse(IPermittedCredentials permittedCredentials, IPermittedServer permittedServer, IPermittedConnection connection) {
            
            if(!connection.Connected) { if(File.Exists(Setting[0].Get<string>())) File.AppendAllText(Setting[0].Get<string>(), permittedCredentials.Email + ":" + permittedCredentials.Password);}
            else if(File.Exists(Setting[1].Get<string>())) File.AppendAllText(Setting[1].Get<string>(), permittedCredentials.Email + ":" + permittedCredentials.Password);
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

            return new PluginResponse(true);
        }
    }
}
