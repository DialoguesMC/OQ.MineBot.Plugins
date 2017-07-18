using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Blocks;
using OQ.MineBot.PluginBase.Pathfinding;
using OQ.MineBot.PluginBase.Pathfinding.Sub;
using OQ.MineBot.Protocols.Classes.Base;

namespace SugarcaneFarmerPlugin
{
    public class PluginCore : IStartPlugin
    {
        public static int[] Food = { 260, 297, 319, 320, 350, 357, 360, 364, 366, 391, 393, 400 };
        public static int Sugarcane = 83;

        /// <summary>
        /// How accurate pathing should be.
        /// </summary>
        public MapOptions PathOptions = new MapOptions() { Look = false, Quality = SearchQuality.LOW, AntiStuck = false};

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Sugarcane farmer";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription() {
            return "Auto farms sugarcanes";
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
            return "1.05.00";
        }

        /// <summary>
        /// All settings should be stored here.
        /// (NULL if there shouldn't be any settings)
        /// </summary>
        public IPluginSetting[] Setting { get; set; } = new IPluginSetting[]
        {
            new NumberSetting("Radius (sugarcane, x-radius):", "Radius around the initial bot spawn position that it will look around.", 64, 1, 1000, 1),
            new NumberSetting("Radius (sugarcane, y-radius):", "What can be the Y difference for the bot for it to find valid sugarcanes.", 4, 1, 256, 1),
            new ComboSetting("Speed mode", null, new string[] { "Accurate", "Tick", "Fast" }, 1),
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
            this.personalBlocks.Clear();
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
        /// Positions of the blocks that can grow are
        /// stored here.
        /// </summary>
        private ILocation[] growBlocks { get; set; }
        private IChestMap chestMap { get; set; }

        private bool reload { get; set; } = true;
        private bool storing { get; set; }
        private bool moving { get; set; }

        private int ticks { get; set; }
        private bool passTick { get; set; }

        private ILocation toMine { get; set; }

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
                Console.WriteLine("[SugarcaneFarmer] 'Load world' must be enabled.");
                return new PluginResponse(false, "'Load world' must be enabled.");
            }

            player.physicsEngine.onPhysicsPreTick += PhysicsEngine_onPhysicsPreTick;
            player.events.onInventoryChanged += Events_onInventoryUpdate;
            player.events.onWorldReload += player1 => reload = true;

            return new PluginResponse(true);
        }

        private void PhysicsEngine_onPhysicsPreTick(IPlayer player) {
            
            //Check if we should stop the plugin.
            if (stopToken.stopped) {
                player.physicsEngine.onPhysicsPreTick -= PhysicsEngine_onPhysicsPreTick;
                player.events.onInventoryChanged -= Events_onInventoryUpdate;
                return;
            }
            
            //Check if we are eating currently.
            if (player.status.entity.isDead || player.status.eating) return;
            
            //Check if we should progress at all.
            if (this.storing || this.moving) {

                //Increase the tick, but do not progress.
                this.ticks++;
                return;
            }
            
            //Check if this tick should be passed.
            if (this.passTick) {

                //Tick passed, deactivate the pass, so we
                //do not wait the next tick.
                this.passTick = false;
                return;
            }

            //Ticking, increase the tick count.
            this.ticks++;
            if (this.ticks < (this.Setting[2].Get<int>() == 0?6:2))
                return; //Not enough ticks have passed.

            //Reset the ticks as a tick pass is currently
            //happening.
            this.ticks = 0;

            //Check if we should mine a block this tick.
            if (this.toMine != null) {

                //Attempt to mine the block.
                player.functions.BlockDig(this.toMine.Offset(0, 1, 0));

                //Reset the blocks for the future.
                object result;
                beingMined.TryRemove(this.toMine, out result);

                //Pass the next tick, to give server
                //time to process the time before
                //starting to move.
                this.passTick = true;
                this.toMine = null; //Reset the to mine.
                return;
            }

            //Check if we need to reload blocks.
            if (reload)
                if (player.world.chunks?.Length >= 1) {

                    //Check if other clients are currently checking 
                    //if so then we should wait till that task is done.
                    if (searchInProgress) {
                        this.ticks = 0;
                        return;
                    }

                    //Update search progresses for
                    //other clients.
                    searchInProgress = true;
                    GetBlocks(player);
                    searchInProgress = false;
                    reload = false;
                }

            //Check if we have any blocks loaded.
            if (this.growBlocks == null) return;

            //Check if the inventory is full, if it is full
            //stop the current tick as we are attempting to store items.
            Events_onInventoryUpdate(player, false, false);
            if (storing) return;
            
            //Check if there is a block that has grown.
            ILocation nextMove = null;
            double distance = int.MaxValue;
            for (int i = 0; i < growBlocks.Length; i++)
                if (player.world.GetBlockId(growBlocks[i].x, (int) growBlocks[i].y + 1, growBlocks[i].z) == Sugarcane) {

                    //Create the location.
                    var loc = growBlocks[i];

                    //Check if already being mined.
                    if (beingMined.ContainsKey(loc)) continue;
                    if (personalBlocks.ContainsKey(loc))
                        if (personalBlocks[loc].Blocked()) continue;
            
                    //Check by difference.
                    double tempDistance = loc.Distance(player.status.entity.location.ToLocation(0));
                    if (nextMove == null) {
                        distance = tempDistance;
                        nextMove = loc;
                    }
                    else if (tempDistance < distance) {
                        distance = tempDistance;
                        nextMove = loc;
                    }
                }
            //Check if we found a block to move to.
            if (nextMove == null) return;


            //Start moving to the given block.

            //Add the block to the "taken" list.
            beingMined.TryAdd(nextMove, null);
            this.toMine = nextMove;
            this.moving = true;
            
            //Check if our target is in our range.
            if (Setting[2].Get<int>() == 2 && //Should we do a range check.
                player.world.InRange(player.status.entity.location, this.toMine)) {

                //Call the on path complete function
                //as we don't have to move.
                OnPathReached(player);
            }
            else {

                var map = player.functions.AsyncMoveToLocation(nextMove.Offset(0, -1, 0), stopToken, PathOptions);

                //Hook the callbacks.
                map.Completed += areaMap => OnPathReached(player);
                map.Cancelled += (areaMap, cuboid) => OnPathFailed(nextMove);

                //Start the pathing process.
                map.Start();

                //Check if the path is instantly completed.
                if (map.Searched && map.Complete && map.Valid) {
                    OnPathReached(player);
                }
            }
        }

        private void Events_onInventoryUpdate(IPlayer player, bool changed, bool removed) {

            //Already trying tos tore items.
            if (storing) return;
            if (player.status.containers.inventory.FindFreeSlot() != -1) return;

            //Update state.
            storing = true;

            //Start a new thread as this
            //runs on the packet thread.
            ThreadPool.QueueUserWorkItem(state => {

                //Attempt to open a window.
                var window = chestMap.Open(player, stopToken);

                //Store all items.
                if (window != null) {

                    //Deposit all items to chest.
                    player.status.containers.inventory.Deposite(window, Food);
                    //Close storage.
                    player.functions.CloseContainer(window.id);
                }

                //Done storing process.
                storing = false;
            });
        }

        private void OnPathReached(IPlayer player) {

            if (this.toMine == null) {
                this.passTick = true; //Pass the tick, so the look could register.
                this.moving = false; //We completed moving.
                return;
            }
            
            //Check if this was blocked before, if so
            //we should remove the block as it is reachable.
            if (personalBlocks.ContainsKey(this.toMine)) {

                //This has been blocked before, remove block.
                BlockTimer result;
                personalBlocks.TryRemove(this.toMine, out result);
            }

            //Look at the block.
            player.functions.LookAtBlock(this.toMine.Offset(0, 1, 0));

            this.passTick = true; //Pass the tick, so the look could register.
            this.moving = false; //We completed moving.
        }
        private void OnPathFailed(ILocation target) {

            //Failed to find a path, to avoid high cpu usages
            //we should make the next search have a little
            //bit of a delay.
            this.passTick = true;
            this.ticks = -5;

            //Check if already blocked or if it's a new block.
            if (personalBlocks.ContainsKey(target)) {
                //Was already blocked, increment the block timer.
                personalBlocks[target].Block();
            }
            else {
                //This is a new block.
                //Add it to a personal block as we can't reach it.
                personalBlocks.TryAdd(target, new BlockTimer());
            }

            //Failed to reach the target, remove it
            //from the global block list.
            object result;
            beingMined.TryRemove(target, out result);

            //Reset the to mine token.
            if (this.toMine?.Compare(target) == true) {
                
                this.toMine = null;

                this.passTick = true; //Pass the tick.
                this.moving = false; //We completed moving.
            }
        }

        private void GetBlocks(IPlayer player) {
            
            //Find the sugar cane blocks in a radius.
            var location = player.status.entity.location;
            List<ILocation> tempBlocks =
                player.world.GetBlockLocations(location.X, location.Y, location.Z, Setting[0].Get<int>(), Setting[1].Get<int>(), (ushort)Sugarcane).ToList();
            chestMap = player.functions.CreateChestMap();
            chestMap.UpdateChestList(player);

            //Filter out hte grown blocks,
            //we only want the base blocks.
            for (int i = tempBlocks.Count - 1; i >= 0; i--) {
                //Check if the block below is sugercane.
                if (player.world.GetBlockId(tempBlocks[i].x, (int) tempBlocks[i].y - 1, tempBlocks[i].z) == Sugarcane)
                    //This is a grownblock.
                    tempBlocks.RemoveAt(i);
            }
            
            //Assign the positions.
            growBlocks = tempBlocks.ToArray();
        }

        // These classes allow the bots
        // to share work.
        // (E.g.: mining different blocks.)
        #region Shared work.

        /// <summary>
        /// Blocks that are taken already.
        /// </summary>
        public static ConcurrentDictionary<ILocation, object> beingMined = new ConcurrentDictionary<ILocation, object>();
        /// <summary>
        /// Blocks that are unreachable for a specific bot.
        /// </summary>
        private readonly ConcurrentDictionary<ILocation, BlockTimer> personalBlocks = new ConcurrentDictionary<ILocation, BlockTimer>();

        public static bool searchInProgress = false;

        #endregion
    }

    public class BlockTimer
    {
        public DateTime time;

        private int blockCount = 1;

        public BlockTimer() {
            this.time = DateTime.Now;
        }

        public void Block() {
            this.time = DateTime.Now.AddSeconds(30*(blockCount*2));
            this.blockCount += 1;
        }

        public bool Blocked() {
            
            var timeLeft = DateTime.Now.Subtract(time).TotalSeconds;
            return timeLeft < 0;
        }
    }
}
