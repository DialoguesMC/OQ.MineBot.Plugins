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
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity.Mob;
using OQ.MineBot.PluginBase.Classes.Entity.Player;
using OQ.MineBot.PluginBase.Utility;
using OQ.MineBot.Protocols.Classes.Base;

namespace RaidAlertsPlugin
{
    public class PluginCore : IStartPlugin
    {
        private const int DisabledLamp = 123; //Disabled lamp.
        private const int EnabledLamp = 124; //Enabled lamp.

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Raid alerts";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription() {
            return "Notifies the user on discord when an explosion occurs/mobs appear/players get close.";
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
            return "1.03.00";
        }

        /// <summary>
        /// All settings should be stored here.
        /// (NULL if there shouldn't be any settings)
        /// </summary>
        public IPluginSetting[] Setting { get; set; } = {
            new StringSetting("Discord name (E.g.: OnlyQubes#8234)", "OR Server name and the channel name split by > (E.g.: OQ.Support>general).", ""),
            new BoolSetting("Local notifications", "", true),
            new BoolSetting("Explosion notifications", "", true),
            new BoolSetting("Wither notifications", "", true),
            new BoolSetting("Creeper notifications", "", true),
            new BoolSetting("Player notifications", "", true),
            new StringSetting("Friendly uuid(s)/name(s)", "Uuids/name(s) split by space.", ""),
            new StringSetting("Lamp coordinates", "Coordinates in the [X Y Z] format, split by a space", "[-1 -1 -1] [0 0 0] [1 1 1]"),
            new LinkSetting("Add bot", "Adds the bot to your discord channel (you must have administrator permissions).", "https://discordapp.com/oauth2/authorize?client_id=299708378236583939&scope=bot&permissions=6144"),

            new StringSetting("Player message", "Message sent to discord when a player is detected.", "[MEDIUM] Player(%name%) has been detected."),
            new StringSetting("Explosion message", "Message sent to discord when an explosion is detected.", "[HIGH] Explosion occured at %location%"),
            new StringSetting("Lamp message", "Message sent to discord when a lamp is deactivated.", "[HIGH] Lamp at %location% has been deactivated!"),
            new StringSetting("Wither message", "Message sent to discord when a wither is detected.", "[HIGH] A wither has been detected!"),
            new StringSetting("Creeper message", "Message sent to discord when a creeper is detected.", "[MEDIUM] A creeper has been detected."),
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
        public void Stop() {
            stopToken.Stop();

            if (player != null) {
                player.events.onExplosion -= Events_onExplosion;
                player.entities.onEntityAdded -= Entities_onEntityAdded;
                player.events.onBlockChanged -= Events_onBlockChanged;
            }
        }
        private CancelToken stopToken = new CancelToken();
        private List<ILocation> lampLocations =new List<ILocation>();

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
            if (!player.settings.loadEntities || !player.settings.loadPlayers)
            {
                Console.WriteLine("[RaidAlerts] 'Load entities & load players' must be enabled.");
                return new PluginResponse(false, "'Load entities & load players' must be enabled.");
            }

            //Parse the lamp coordinates.
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

            stopToken.Reset();
            player.events.onExplosion += Events_onExplosion;
            player.entities.onEntityAdded += Entities_onEntityAdded;
            player.events.onBlockChanged += Events_onBlockChanged;
            this.player = player;
            return new PluginResponse(true);
        }

        private void Events_onBlockChanged(IPlayer player, ILocation location, ushort oldId, ushort newId)
        {
            //Check if we should stop the plugin.
            if (stopToken.stopped)
            {
                player.events.onExplosion -= Events_onExplosion;
                player.entities.onEntityAdded -= Entities_onEntityAdded;
                player.events.onBlockChanged -= Events_onBlockChanged;
                return;
            }

            //Check if it is a lamp.
            if (newId != EnabledLamp) {
                //Check if this location is tracked.
                var tracked = lampLocations.Any(x => x.Compare(location));
                if (!tracked) return;

                //Notify the user as the this block is a disabled lamp
                //and is tracked.
                NotifyUser(
                    ApplyVariables(Setting[11].Get<string>(), player.status.entity.location.ToLocation(0), location, ""), 10,
                    4);
            }
        }

        private IPlayer player;

        private void Entities_onEntityAdded(OQ.MineBot.PluginBase.Classes.Entity.IEntity entity, OQ.MineBot.PluginBase.Classes.Physics.EventCancelToken token) {

            //Check if we should stop the plugin.
            if (stopToken.stopped) {
                player.events.onExplosion -= Events_onExplosion;
                player.entities.onEntityAdded -= Entities_onEntityAdded;
                player.events.onBlockChanged -= Events_onBlockChanged;
                return;
            }

            // Check for errors.
            if (entity == null) return;

            //Check if it's a player.
            var isPlayer = entity.GetType().GetInterfaces().Contains(typeof (IPlayerEntity));
            if (isPlayer && Setting[5].Get<bool>()) {
                var playerEntity = (IPlayerEntity) entity;
                var friendlyFixed = Setting[6].Get<string>().Replace("-", "");
                var friendly = friendlyFixed.Split(' ');
                if (!friendly.Contains(playerEntity.uuid, StringComparer.CurrentCultureIgnoreCase)) {
                    var name = player.entities.FindNameByUuid(playerEntity?.uuid);
                    if(name?.Name != null && !friendly.Contains(name.Name))
                        NotifyUser(ApplyVariables(Setting[9].Get<string>(), player.status.entity.location.ToLocation(0), playerEntity.location.ToLocation(0), name.Name), 4, 3);
                }
            }
            else {
                var mobEntity = entity as IMobEntity;
                if (mobEntity == null) return;

                if(mobEntity.type == MobType.Wither && Setting[3].Get<bool>())
                    NotifyUser(ApplyVariables(Setting[12].Get<string>(), player.status.entity.location.ToLocation(0), mobEntity.location.ToLocation(0), "Wither"), 10, 2);
                else if(mobEntity.type == MobType.Creeper && Setting[4].Get<bool>())
                    NotifyUser(ApplyVariables(Setting[13].Get<string>(), player.status.entity.location.ToLocation(0), mobEntity.location.ToLocation(0), "Creeper"), 4, 1);
            }
        }

        private void Events_onExplosion(IPlayer player, float X, float Y, float Z) {

            //Check if we should stop the plugin.
            if (stopToken.stopped)
            {
                player.events.onExplosion -= Events_onExplosion;
                player.entities.onEntityAdded -= Entities_onEntityAdded;
                player.events.onBlockChanged -= Events_onBlockChanged;
                return;
            }

            if (Setting[2].Get<bool>())
                NotifyUser(ApplyVariables(Setting[10].Get<string>(), player.status.entity.location.ToLocation(0), new Location((int)Math.Round(X), Y, (int)Math.Round(Z)), ""), 10, 0);
        }

        private void NotifyUser(string message, int priority, int id) {

            //Check if this id is on delay.
            if (IdLimits.ContainsKey(id) && IdLimits[id].Subtract(DateTime.Now).TotalMilliseconds > 0)
                return; //Still on delay.

            //add the delay.
            IdLimits.AddOrUpdate(id, i => DateTime.Now.AddSeconds(30/priority), (i, time) => DateTime.Now.AddSeconds(30/priority));

            //Send notifications.
            if (Setting[1].Get<bool>())
                DiscordHelper.AlertMessage(Setting[0].Get<string>(), message, priority);
            else
                DiscordHelper.SendMessage(Setting[0].Get<string>(), message);
        }

        /// <summary>
        /// Each id has a limit according to 
        /// it's priority.
        /// </summary>
        private static ConcurrentDictionary<int, DateTime> IdLimits = new ConcurrentDictionary<int, DateTime>();

        private static string ApplyVariables(string text, ILocation playerLocation, ILocation targetLocation, string name) {
            if (text.Contains("%name%"))
                text = text.Replace("%name%", name);
            if (text.Contains("%location%"))
                text = text.Replace("%location%", targetLocation.ToString());
            if (text.Contains("%distance%"))
                text = text.Replace("%distance%", Math.Round(playerLocation.Distance(targetLocation)).ToString(CultureInfo.CurrentCulture));
            return text;
        }
    }
}
