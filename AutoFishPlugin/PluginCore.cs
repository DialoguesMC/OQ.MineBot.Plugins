using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Blocks;
using OQ.MineBot.PluginBase.Classes.Objects;
using OQ.MineBot.PluginBase.Classes.Objects.List;
using OQ.MineBot.Protocols.Classes.Base;

namespace AutoFishPlugin
{
    public class PluginCore : IStartPlugin
    {
        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return "Auto fish";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription()
        {
            return "Gets you level 99 in fishing.";
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
            new BoolSetting("Keep rotation", "Should the bot not change it's head rotation?", false),
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
        public void OnLoad(int version, int subversion, int buildversion)
        {
        }

        /// <summary>
        /// Called once the plugin is enabled.
        /// Meaning the start methods will be
        /// called when needed.
        /// </summary>
        public void OnEnabled()
        {
            stopToken.Reset();
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
        /// Instance of the player.
        /// </summary>
        private IPlayer player;

        private bool fishing;
        private DateTime castTime; // When did we start the fishing process.
        private DateTime maxWaitTime; // Maximum time the bot can wait until reeling in.

        private FishingFloatObject lureObject;
        private bool lureSpawned
        {
            get { return lureObject != null; }
            set { if (value == false) lureObject = null; }
        }

        private const double MOTION_Y_TRESHOLD = -0.02;
        private const int CAST_TIME = 6; // How many seconds should we wait before we can reel back in. (seconds) 
        private const int MAX_WAIT_TIME = 60; // How long can we wait before reeling in (and retrying). (seconds) 

        private const int ROD_ID = 346;
        private const int WATER_ID = 9;

        private bool castTick = false;
        private bool lookTick = true;
        private int tick = 0;

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

            //Assign values.
            this.player = player;

            //Hook events.
            player.events.onObjectSpawned += Events_onObjectSpawned;
            player.events.onTick += Events_onTick;
            player.events.onEntityVelocity += Events_onEntityVelocity;

            return new PluginResponse(true);
        }

        private void Events_onEntityVelocity(int entityId, short x, short y, short z) {

            if (stopToken.stopped)
                return;

            // Check if we should care about this
            // velocity change.
            if (!this.fishing || !this.lureSpawned || this.lureObject.Id != entityId)
                return;

            // Check if we are not on the throw timer.
            if (DateTime.Now.Subtract(castTime).TotalSeconds < CAST_TIME)
                return;

            if (x != 0 || z != 0 || y > MOTION_Y_TRESHOLD) return;

            // Reel in, we got a fish probably.
            Recast();
        }

        private void Events_onTick(IPlayer player) {

            // Check if the plugin was stopped.
            if (stopToken.stopped) {
                player.events.onObjectSpawned -= Events_onObjectSpawned;
                player.events.onTick -= Events_onTick;
                player.events.onEntityVelocity -= Events_onEntityVelocity;
                return;
            }

            // Check if we should reset the 
            // fishing state.
            if (!FishingState()) {
                ResetState();
                return;
            }

            // Check ticks.
            if (tick < 0) {
                tick++;
                return;
            }
            tick = -5;

            if (this.lookTick && Setting[0].Get<bool>()) {
                LookAtWater();
                this.lookTick = false;
                return;
            }

            // Check if we have the rod equiped.
            if (!IsRodEquiped()) {
                ResetState();
                EquipRod();
                return;
            }

            // Check if we should cast this tick.
            if (this.castTick) {

                player.functions.UseSelectedItem(); // Right click the rod.
                this.castTime = DateTime.Now;
                this.maxWaitTime = DateTime.Now.AddSeconds(MAX_WAIT_TIME);
                this.fishing = true;
                this.castTick = false;
                this.tick = -20;
                return;
            }

            // Check if we are not fishing, but
            // our lure state is spawned.
            if (!this.fishing && this.lureSpawned) {
                Recast();
                return;
            }

            // Check if we are still fishing.
            if (this.fishing && this.lureSpawned && !ForceReel())
                return; // Wait for fish.

            // Check if we are still on the waiting process.
            if (DateTime.Now.Subtract(castTime).TotalSeconds < CAST_TIME)
                return;

            // Check if the lure didn't spawn in.
            // (CAST_TIME has already passed)
            if (this.fishing && !this.lureSpawned) {
                Recast();
                return;
            }
            
            // Check if we need to force reel in.
            if (this.fishing && ForceReel()) {
                Recast();
                return;
            }

            this.castTick = true; // Cast.
        }

        private void Events_onObjectSpawned(IWorldObject worldObject, double X, double Y, double Z, byte pitch, byte yaw) {

            if (stopToken.stopped)
                return;

            // We only care for fishing hook spawns.
            if (worldObject.GetType() != ObjectTypes.FishingHook) return;

            var hook = (FishingFloatObject)worldObject;
            if (hook.Owner != player.status.entity.entityId) return; // We care only for our hook.
            this.lureObject = hook; // Assign the lure object.
        }

        private bool IsRodEquiped() {
            return player.status.containers.inventory.hotbar.GetSlot((byte)player.status.entity.selectedSlot).id == ROD_ID;
        }
        private bool FishingState() {
            return !player.status.eating && !player.status.entity.isDead;
        }

        private bool ForceReel() {
            return DateTime.Now.Subtract(maxWaitTime).TotalMilliseconds > 0;
        }
        private void EquipRod() {
            player.status.containers.inventory.Select(ROD_ID);
        }

        private void ResetState() {
            this.fishing = false;
            this.lureSpawned = false;
        }
        private void Recast() {
            player.functions.UseSelectedItem(); // Right click the rod.
            this.castTick = true;
            ResetState();
            tick = -10;
        }

        private void LookAtWater() {

            var blocks = player.world.GetBlockLocations(player.status.entity.location.X, player.status.entity.location.Y,
                player.status.entity.location.Z, 16, 6, WATER_ID);
            blocks = blocks.OrderBy(x => x.Distance(player.status.entity.location.ToLocation(0))).ToArray();

            for(int i = 0; i < blocks.Length; i++)
                if (player.world.GetBlockId(blocks[i].x, (int)blocks[i].y+1, blocks[i].z) != WATER_ID && blocks[i].Distance(player.status.entity.location.ToLocation(1)) > 1.75) {
                    player.functions.LookAtBlock(blocks[i].Offset(0, 0.8f, 0));
                    break;
                }
        }
    }
}