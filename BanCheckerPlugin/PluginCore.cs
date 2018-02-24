using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Permissions;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Utility;

namespace BanCheckerPlugin
{
    [Plugin(1, "Ban checker", "Checks if an account is banned on the server.")]
    public class PluginCore : IStartPlugin
    {
        private static Dictionary<string, string> AccountsSaved = new Dictionary<string, string>();

        public override void OnLoad(int version, int subversion, int buildversion) {
            this.Setting = new IPluginSetting[3];
            Setting[0] = new PathSetting("Banned accounts", "Outputs banned accounts to this file.", "");
            Setting[1] = new PathSetting("Unbanned accounts", "Outputs unbanned accounts to this file.", "");
            Setting[2] = new ComboSetting("Format", "Format that the accounts will be save in.", new string[] { "Email:Password", "Email", "Username" }, 0);
        }

        public override PluginResponse OnEnable(IBotSettings botSettings) {
            
            // CLear the list in-case
            // it was restarted.
            AccountsSaved.Clear();
            
            // Request permissions to hook low level events.
            var permission = EventPermissions.CheckPermissions("low-level");
            if (permission == false) return new PluginResponse(false, "Not enough permissions, plugin requires 'All permissions'.");
            EventPermissions.LowLevelHook("Ban checker", LowLevelEvents.OnServerInitialResponse, ServerResponse);

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
            return new PluginResponse(true);
        }

        private void ServerResponse(IPermittedCredentials permittedCredentials, IPermittedServer permittedServer, IPermittedConnection connection) {
            
            // Check if this account has been
            // already checked.
            if (AccountsSaved.ContainsKey(permittedCredentials.Email)) return; // Account already saved.

            AccountsSaved.Add(permittedCredentials.Email, permittedCredentials.Password); // Add to list, so we don't save it twice.

            if (!connection.Connected) { if(File.Exists(Setting[0].Get<string>())) File.AppendAllText(Setting[0].Get<string>(), Environment.NewLine + Format(permittedCredentials));}
            else if(File.Exists(Setting[1].Get<string>())) File.AppendAllText(Setting[1].Get<string>(), Environment.NewLine + Format(permittedCredentials));
        }

        private string Format(IPermittedCredentials permittedCredentials) {
            if (Setting[2].Get<int>() == 0)
                return permittedCredentials.Email + ":" + permittedCredentials.Password;
            if (Setting[2].Get<int>() == 1)
                return permittedCredentials.Email;
            if (Setting[2].Get<int>() == 2)
                return permittedCredentials.Username;
            throw new ArgumentException();
        }
    }
}
