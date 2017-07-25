using System;
using System.Collections.Concurrent;
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
using OQ.MineBot.PluginBase.Classes.Blocks;
using OQ.MineBot.PluginBase.Classes.Entity;
using OQ.MineBot.PluginBase.Classes.Entity.Player;
using OQ.MineBot.PluginBase.Pathfinding;
using OQ.MineBot.PluginBase.Pathfinding.Sub;
using OQ.MineBot.Protocols.Classes.Base;

namespace CropFarmerPlugin
{
    public class PluginCore : IStartPlugin
    {
        public static int[] Food = { 364, 412, 320, 424, 366, 393, 297 };
        public static ushort[] Farmable = { 59, 141, 142, 207 };
        public static int[] Plantable = { 295, 391, 392, 435 };

        /// <summary>
        /// How accurate pathing should be.
        /// </summary>
        public MapOptions PathOptions = new MapOptions() { Look = false, Quality = SearchQuality.LOW };

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Crop farmer";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription() {
            return "Auto farms crops";
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
            return "1.00.01";
        }

        /// <summary>
        /// All settings should be stored here.
        /// (NULL if there shouldn't be any settings)
        /// </summary>
        public IPluginSetting[] Setting { get; set; } = new IPluginSetting[]
        {
            new NumberSetting("Radius (crop, x-radius):", "Radius around the initial bot spawn position that it will look around.", 64, 1, 1000, 1),
            new NumberSetting("Radius (crop, y-radius):", "What can be the Y difference for the bot for it to find valid crops.", 4, 1, 256, 1),
            new ComboSetting("Speed mode", null, new string[] { "Accurate", "Fast" }, 0),
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
        public void OnDisabled()
        {
        }

        /// <summary>
        /// The plugin should be stopped.
        /// </summary>
        public void Stop()
        {
            beingMined.Clear();
            stopToken.Stop();
        }
        private CancelToken stopToken = new CancelToken();
        private IPlayer player;

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
            if (!player.settings.loadWorld)
            {
                Console.WriteLine("[CropFarmer] 'Load world' must be enabled.");
                return new PluginResponse(false, "'Load world' must be enabled.");
            }
            
            this.player = player;

            player.physicsEngine.onPhysicsPreTick += PhysicsEngine_onPhysicsPreTick;
            player.events.onInventoryChanged += Events_onInventoryUpdate;
            player.events.onWorldReload += player1 => reload = true;
            
            return new PluginResponse(true);
        }

        private int passedTicks = 0;

        private void PhysicsEngine_onPhysicsPreTick(IPlayer player) {

            //Check if we should stop the plugin.
            if (stopToken.stopped) {
                player.physicsEngine.onPhysicsPreTick -= PhysicsEngine_onPhysicsPreTick;
                player.events.onInventoryChanged -= Events_onInventoryUpdate;
                return;
            }
            
            //Check if we are eating currently.
            if (player.status.eating) return;

            //Check if we are waiting for the next physics tick 
            //(because rotation is only sent after the physics tick)
            if (this.awaitingLook)
                this.awaitingLook = false;
            else if (this.toMine != null) {

                //Finished a rotation, attempt to mine.
                if (grown && player.functions.BlockDig(this.toMine).valid) {

                    //Remove the block.
                    object obj;
                    beingMined.TryRemove(toMine, out obj);
                    //Look at the block to replant it.
                    this.awaitingLook = true;
                    player.functions.LookAtBlock(toReplant, true);
                }
                else {

                    object obj;
                    beingMined.TryRemove(toMine, out obj);
                    moving = false;
                    grown = false;
                    this.toReplant = null; //Reset as we are not replanting.
                }

                this.toMine = null;

            }
            else if (this.toReplant != null) {

                //Replant the block.
                Replant(this.toReplant);

                //Reset replanting.
                this.toReplant = null;

                moving = false;
                grown = false;
            }

            //Increment tick count, so that
            //we could do a full update when
            //needed.
            passedTicks++;
            if (passedTicks < 2 || moving || storing) return; //Do not overrflow.
            //Check if we need to reload blocks.
            if (reload) {
                if (player.world.chunks?.Length >= 1) {
                    GetBlocks(player);
                    reload = false;
                }
                else passedTicks = 0;
            }
            if (growBlocks == null) return;

            //Check inventory.
            Events_onInventoryUpdate(player, false, false);
            if (storing) return;

            //Check if there is a block that has grown.
            ILocation nextMove = null;
            double distance = int.MaxValue;
            for (int i = 0; i < growBlocks.Length; i++) {
                var block = player.world.GetBlock(growBlocks[i].x, (int) growBlocks[i].y, growBlocks[i].z);
                if (Farmable.Contains((ushort) (block >> 4)) && (block & 15) >= 7 ||
                    (Farmable[3] == (ushort)(block >> 4) && (block & 15) >= 3)) { // Beetroot
                    //Create the location.
                    var loc = growBlocks[i];

                    //Check if already being mined.
                    if (beingMined.ContainsKey(loc)) continue;

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
            }
            //Check if we found a block to move to.
            if (nextMove == null) return;


            //Add the position.
            beingMined.TryAdd(nextMove, null);

            //Start pathing to player.
            grown = true;
            blockId = this.player.world.GetBlockId(nextMove.x, (int) nextMove.y, nextMove.z);

            //Check if our target is in our range.
            if (Setting[2].Get<int>() == 1 && player.world.InRange(player.status.entity.location, nextMove)) {

                //We are in range, mine instantly.
                moving = true;
                OnPathUpdate(true, nextMove, player);
            }
            else {
                if (player.physicsEngine.path == null ||
                    (player.physicsEngine.path.Complete || !player.physicsEngine.path.Valid)) {
                    moving = true;
                    var map = player.functions.AsyncMoveToLocation(nextMove.Offset(0, -1, 0), stopToken, PathOptions);

                    map.Completed += areaMap => OnPathUpdate(true, nextMove, player);
                    map.Cancelled += (areaMap, cuboid) => OnPathUpdate(false, nextMove, player);

                    map.Start();

                    //Check if the path is instantly completed.
                    if (map.Searched && map.Complete && map.Valid) {
                        OnPathUpdate(true, nextMove, player);
                    }
                }
            }

            //Reset ticks.
            passedTicks = 0;
        }

        private void Events_onInventoryUpdate(IPlayer player, bool changed, bool removed)
        {

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
                if (window != null)
                {
                    //Deposit items.
                    player.status.containers.inventory.Deposite(window, Food);
                    //Close storage.
                    player.functions.CloseContainer(window.id);
                }

                //Done storing process.
                storing = false;
            });
        }

        private void OnPathUpdate(bool success, ILocation mine, IPlayer player)
        {
            //Check if we finished moving.
            if (success) {
                //Look at the block.
                player.functions.LookAtBlock(mine);

                //Add the block to the mining queue
                //once we have looked.
                this.awaitingLook = true;
                this.toMine = mine;
                this.toReplant = mine;
            }
            else {
                object obj;
                beingMined.TryRemove(new Location(mine.x, mine.y, mine.z), out obj);
                moving = false;
                grown = false;
            }
        }

        private void Replant(ILocation location) {
            
            //Wait a littble bit before replanting.
            Thread.Sleep(25);

            //Set up priority for planting objects.
            //(prefferably plant the same block it just mined)
            var priority = FarmableToPlantable(blockId);
            var prioritizedList = Plantable.ToList();
            prioritizedList.Insert(0, priority);
            var prioritizedArray = prioritizedList.ToArray();
            //We should eat, check if we have food.
            if (player.status.containers.inventory.hotbar.FindId(prioritizedArray) == -1)
            { //We should find it in inventory.
              //Find the slot.
                var slot = player.status.containers.inventory.inner.FindId(prioritizedArray);
                if (slot != -1)
                {
                    player.status.containers.inventory.hotbar.BringToHotbar(6, slot, null); //Slot 6 farm slot.
                }
            }

            //Check if we have any food in the hotbar.
            var plantSlot = player.status.containers.inventory.hotbar.FindId(prioritizedArray);
            //Check if found any healing food.
            if (plantSlot != -1)
            {
                //Select the item in hotbar.
                player.functions.SetHotbarSlot((short)(plantSlot - 36));
                //Wait for the server to notice the call.
                Thread.Sleep(50);
                //Start eating.
                var data = player.functions.FindValidNeighbour(location);
                player.functions.BlockPlaceOnBlockFace(data.location, data.face);
            }
        }

        /// <summary>
        /// Positions of the blocks that can grow are
        /// stored here.
        /// </summary>
        private ILocation[] growBlocks { get; set; }
        private int blockId = 0; //Block that should be replanted.
        private bool grown = false;
        private bool moving = false;
        private bool storing = false;
        private bool reload = true;
        private IChestMap chestMap { get; set; }

        private ILocation toMine = null;
        private ILocation toReplant = null;
        private bool awaitingLook= false;

        private void GetBlocks(IPlayer player)
        {
            //Find the sugar cane blocks in
            //a radius of 64 blocks (4 chunks).
            var location = player.status.entity.location;

            ILocation[] tempBlocks = new ILocation[0];
            for (int i = 0; i < Farmable.Length; i++) {
                List<ILocation> temp =
                    player.world.GetBlockLocations(location.X, location.Y, location.Z, Setting[0].Get<int>(),
                        Setting[1].Get<int>(), Farmable[i]).ToList();

                tempBlocks = tempBlocks.Concat(temp).ToArray();
            }
            chestMap = player.functions.CreateChestMap();
            chestMap.UpdateChestList(player);

            //Assign the positions.
            growBlocks = tempBlocks.ToArray();
        }

        private int FarmableToPlantable(int id) {
            
            //Loop trough the farmables and use the same
            //enumerator on the plantables as both arrays
            //are "linked".
            for(int i = 0;i < Farmable.Length; i++)
                if (Farmable[i] == id)
                    return Plantable[i];
            return -1;
        }

        // These classes allow the bots
        // to share work.
        // (E.g.: mining different blocks.)
        #region Shared work.

        /// <summary>
        /// Blocks that are taken already.
        /// </summary>
        public static ConcurrentDictionary<ILocation, object> beingMined = new ConcurrentDictionary<ILocation, object>();

        #endregion
    }
}
