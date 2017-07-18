using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity;
using OQ.MineBot.PluginBase.Classes.Entity.Lists;
using OQ.MineBot.PluginBase.Classes.Entity.Player;
using OQ.MineBot.PluginBase.Classes.Items;
using OQ.MineBot.PluginBase.Classes.Window.Containers.Subcontainers;
using OQ.MineBot.PluginBase.Pathfinding;
using OQ.MineBot.PluginBase.Utility;
using OQ.MineBot.Protocols.Classes.Base;

namespace FollowPlugin
{
    public class PluginCore : IStartPlugin
    {
        /// <summary>
        /// How accurate pathing should be.
        /// </summary>
        public MapOptions HighDetailOption = new MapOptions() { Look = true, Quality = SearchQuality.HIGH };
        public MapOptions LowDetailOption = new MapOptions() { Look = true, Quality = SearchQuality.LOW };

        /// <summary>
        /// Last result state of
        /// our movement.
        /// </summary>
        public MovementState State { get; set; } = MovementState.HIGH_NOTFOUND;

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Follow";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription() {
            return "Follows the owner!";
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
            return "1.04.00";
        }

        /// <summary>
        /// All settings should be stored here.
        /// (NULL if there shouldn't be any settings)
        /// </summary>
        public IPluginSetting[] Setting { get; set; } =
        {
            new StringSetting("Owner name/uuid", "Player that the bots will follow.", "")
        };
        /// <summary>
        /// UUID info of the owner.
        /// </summary>
        public UUID ownerUuid;
        /// <summary>
        /// Player entity of the owner.
        /// </summary>
        private ILiving ownerEntity { get; set; }
        
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

            ownerUuid = null;
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
            if (!player.settings.loadWorld) {
                Console.WriteLine("[Follow] 'Load world' must be enabled.");
                return new PluginResponse(false, "'Load world' must be enabled.");
            }
            //Check if bot settings are valid.
            if (!player.settings.loadEntities || !player.settings.loadPlayers) {
                Console.WriteLine("[Follow] 'Load players' must be enabled.");
                return new PluginResponse(false, "'Load players' must be enabled.");
            }

            //Check if the variables are valid.
            if (string.IsNullOrWhiteSpace(Setting[0].Get<string>())) {
                Console.WriteLine("[Follow] Invalid owner name/uuid.");
                return new PluginResponse(false, "Invalid owner name/uuid.");
            }

            //Hook start events.
            player.physicsEngine.onPhysicsPreTick += PhysicsEngine_onPhysicsPreTick;
            return new PluginResponse(true);
        }

        private bool moving { get; set; }
        private int  ticks { get; set; }

        private void PhysicsEngine_onPhysicsPreTick(IPlayer player) {

            //Check if we should stop the plugin.
            if (stopToken.stopped) {
                player.physicsEngine.onPhysicsPreTick -= PhysicsEngine_onPhysicsPreTick;
                return;
            }
            
            //Don't do anything if dead.
            if (player.status.entity.isDead || moving) return;

            //Calculate how much ticks have passed
            //since the last full tick.
            ticks++;
            if (ticks < 4) return;
            ticks = 0;

            //Check if we need to find the owner.
            if (ownerEntity == null || ownerEntity.unloaded) {
                //Attempt to find a new owner.
                this.ownerEntity = GetOwner(player);
            }
            //Check if we have an owner entity.
            if (this.ownerEntity == null || ownerEntity.unloaded) return; //No player found.
            
            //Update the movement state.
            this.moving = true;

            //Check fwhich detail should be selected.
            var currentOptions = State != MovementState.FOUND ? LowDetailOption : HighDetailOption;

            //Create the map and hook all the
            //callbacks.
            var map = player.functions.AsyncMoveToEntity(ownerEntity, stopToken, currentOptions);
            //Hook the callbacks.
            map.Completed += areaMap => OnPathReached();
            map.Cancelled += (areaMap, cuboid) => OnPathFailed();

            //Start the pathing process.
            map.Start();

            //Check if the path is instantly completed.
            if (map.Searched && map.Complete && map.Valid)
                OnPathReached();
        }
        
        private void OnPathReached() {

            //We found a valid path, reset the old
            //not valid path states.
            State = MovementState.FOUND;

            //Reset the ticks.
            this.ticks = 0;
            //Unblock the ticking loop.
            this.moving = false;
        }

        private void OnPathFailed() {

            //Check how long we should block
            //the ticking for the cpu to take a rest.
            if (State == MovementState.FOUND)
                State = MovementState.LOW_NOTFOUND;
            else if (State == MovementState.LOW_NOTFOUND)
                State = MovementState.HIGH_NOTFOUND;

            //Reset the ticks.
            this.ticks = -(int)State;
            //Unblock the ticking loop.
            this.moving = false;
        }

        private ILiving GetOwner(IPlayer player)
        {

            //Attempt to get the uuid if it's null.
            if (ownerUuid == null)
            {

                //Check if we have the name or the uuid.
                if (Setting[0].Get<string>().Length == 32 || Setting[0].Get<string>().Length == 36)
                {

                    //The user inputed a uuid.
                    this.ownerUuid = new UUID(Setting[0].Get<string>(), null);
                }
                else
                {
                    //Attempt to find a name.
                    this.ownerUuid = player.entities.FindUuidByName(Setting[0].Get<string>());
                    if(ownerUuid != null)
                        Targeter.IgnoreList.Add(ownerUuid.Uuid);
                }
            }

            //Do not progres if the uuid is null.
            if (ownerUuid == null) return null;

            //Loop through all players.
            foreach (var entity in player.entities.playerList)
            {

                //Check if entity is valid.
                if (entity.Value == null) continue;

                //Find the player with the owners user id.
                if (((IPlayerEntity)entity.Value).uuid == ownerUuid.Uuid.Replace("-", ""))
                    return entity.Value;
            }

            //Owner entity was not found.
            return null;
        }
    }

    public enum MovementState
    {
        FOUND=5,
        LOW_NOTFOUND=16,
        HIGH_NOTFOUND=32,
    }
}
