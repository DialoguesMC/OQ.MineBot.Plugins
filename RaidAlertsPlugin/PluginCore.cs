using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Base.Plugin;
using OQ.MineBot.PluginBase.Bot;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity.Mob;
using OQ.MineBot.PluginBase.Classes.Entity.Player;
using OQ.MineBot.PluginBase.Utility;
using OQ.MineBot.Protocols.Classes.Base;
using RaidAlertsPlugin.Tasks;

namespace RaidAlertsPlugin
{
    [Plugin(1, "Raid alerts", "Notifies the user on discord when an explosion occurs/mobs appear/players get close.")]
    public class PluginCore : IStartPlugin
    {
        public override void OnLoad(int version, int subversion, int buildversion) {
            this.Setting = new IPluginSetting[10];
            Setting[0] = new StringSetting("User or Channel ID", "Enable developer mode: Settings->Appearance->Developer mode. Copy id: right click channel and click 'Copy ID'.", "");
            Setting[1] = new BoolSetting("Local notifications", "", true);
            Setting[2] = new BoolSetting("Explosion notifications", "", true);
            Setting[3] = new BoolSetting("Wither notifications", "", true);
            Setting[4] = new BoolSetting("Creeper notifications", "", true);
            Setting[5] = new BoolSetting("Player notifications", "", true);
            Setting[6] = new StringSetting("Friendly uuid(s)/name(s)", "Uuids/name(s) split by space.", "");
            Setting[7] = new StringSetting("Lamp coordinates", "Coordinates in the [X Y Z] format, split by a space", "[-1 -1 -1] [0 0 0] [1 1 1]");
            Setting[8] = new ComboSetting("Mode", "Notification mode", new []{"none", "@everyone", "@everyone + tts"}, 1);
            Setting[9] = new LinkSetting("Add bot", "Adds the bot to your discord channel (you must have administrator permissions).", "https://discordapp.com/oauth2/authorize?client_id=299708378236583939&scope=bot&permissions=6152");   
        }

        public override PluginResponse OnEnable(IBotSettings botSettings) {
            
            if(!botSettings.loadEntities || !botSettings.loadPlayers) return new PluginResponse(false, "'Load entities & load players' must be enabled.");
            if(!botSettings.loadWorld && !string.IsNullOrWhiteSpace(Setting[7].Get<string>())) return new PluginResponse(false, "'Load worlds' must be enabled.");

            try {
                if(string.IsNullOrWhiteSpace(Setting[0].Get<string>())) return new PluginResponse(false, "Could not parse discord id.");
                ulong.Parse(Setting[0].Get<string>());
            }
            catch (Exception ex) {
                return new PluginResponse(false, "Could not parse discord id.");
            }
            return new PluginResponse(true);
        }

        public override void OnStart() {

            //Parse the lamp coordinates.
            var lampLocations = new List<ILocation>();
            var splitReg = new Regex(@"\[(.*?)\]");
            var split = splitReg.Matches(this.Setting[7].Get<string>());
            foreach (var match in split) {
                //Split into numbers only.
                var numbers = match.ToString().Replace("[", "").Replace("]", "").Split(' ');
                if(numbers.Length != 3) continue;

                //Try-catch in case the user
                //entered an invalid character.
                try {
                    int x = int.Parse(numbers[0]);
                    int y = int.Parse(numbers[1]);
                    int z = int.Parse(numbers[2]);

                    lampLocations.Add(new Location(x, y, z));
                }
                catch { }
            }

            // Add listening tasks.
            RegisterTask(new Alerts(
                            ulong.Parse(Setting[0].Get<string>()),
                            Setting[1].Get<bool>(), Setting[2].Get<bool>(), Setting[3].Get<bool>(), Setting[4].Get<bool>(), Setting[5].Get<bool>(),
                            Setting[6].Get<string>(), lampLocations.ToArray(), (DiscordHelper.Mode)Setting[8].Get<int>()
                        ));
        }
    }
}
