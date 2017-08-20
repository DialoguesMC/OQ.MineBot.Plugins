using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Blocks;
using OQ.MineBot.PluginBase.Classes.Items;
using OQ.MineBot.PluginBase.Classes.Materials;
using OQ.MineBot.Protocols.Classes.Base;

namespace SandPrinterPlugin
{
    public class PluginCore : IStartPlugin
    {
        private static int[] Sands = new[] {12, 13};
        private MapOptions PathOptions = new MapOptions() { Look = false, Quality = SearchQuality.LOW, AntiStuck = false };
        private const float REACH = 4;
        private static ushort[] UNWALKABLE = new[] {(ushort) 12};
        private const float MAX_RANGE = 5;

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return "Sand printer";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription()
        {
            return "Places sand in the specified area.";
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
            return "2.00.00";
        }

        /// <summary>
        /// All settings should be stored here.
        /// (NULL if there shouldn't be any settings)
        /// </summary>
        public IPluginSetting[] Setting { get; set; } =
        {
            new StringSetting("Start x y z", "", "0 0 0"),
            new StringSetting("End x y z", "", "0 0 0"),
            new ComboSetting("Mode", null, new string[] { "Fast", "Accurate" }, 1),
            new BoolSetting("Sand walking", "Can the bot walk on sand (might cause it falling off with multiple bots)", false),
            new BoolSetting("No movement", "Should the bot place all the sand from the spot it is in?", false),
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
            Taken.Clear();
        }

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
        /// <summary>
        /// Radius that we should be placing the blocks in.
        /// </summary>
        public IRadius totalRadius;

        /// <summary>
        /// Current location we are placing blocks at.
        /// </summary>
        public ILocation target;
        public ILocation[] targets;

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
                Console.WriteLine("[SandPrinter] 'Load world' must be enabled.");
                return new PluginResponse(false, "'Load world' must be enabled.");
            }
            if (!player.settings.loadInventory)
            {
                Console.WriteLine("[SandPrinter] 'Load inventory' must be enabled.");
                return new PluginResponse(false, "'Load inventory' must be enabled.");
            }
            //Check if settings are valid.
            if (string.IsNullOrWhiteSpace(Setting[0].Get<string>()) ||
                string.IsNullOrWhiteSpace(Setting[1].Get<string>()))
            {
                Console.WriteLine("[SandPrinter] No coordinates have been entered.");
                return new PluginResponse(false, "No coordinates have been entered.");
            }
            if (!Setting[0].Get<string>().Contains(' ') || !Setting[1].Get<string>().Contains(' '))
            {
                Console.WriteLine("[SandPrinter] Invalid coordinates (does not contain ' ').");
                return new PluginResponse(false, "Invalid coordinates (does not contain ' ').");
            }
            var startSplit = Setting[0].Get<string>().Split(' ');
            var endSplit = Setting[1].Get<string>().Split(' ');
            if (startSplit.Length != 3 || endSplit.Length != 3)
            {
                Console.WriteLine("[SandPrinter] Invalid coordinates (must be x y z).");
                return new PluginResponse(false, "Invalid coordinates (must be x y z).");
            }
            totalRadius = new IRadius(new Location(int.Parse(startSplit[0]), int.Parse(startSplit[1]), int.Parse(startSplit[2])),
                                      new Location(int.Parse(endSplit[0]), int.Parse(endSplit[1]), int.Parse(endSplit[2])));
            
            //Set the player.
            this.player = player;
            
            //Hook start events.
            player.physicsEngine.onPhysicsPreTick += PhysicsEngine_onPhysicsPreTick;
            player.events.onDisconnected += Events_onDisconnected;
            return new PluginResponse(true);
        }

        private void PhysicsEngine_onPhysicsPreTick(IPlayer player)
        {

            //Check if we should stop the plugin.
            if (stopToken.stopped) {
                player.physicsEngine.onPhysicsPreTick -= PhysicsEngine_onPhysicsPreTick;
                ClearTarget();
                return;
            }

            //Don't do anything if dead.
            //or if we are already mining.
            if (player.status.entity.isDead || player.status.eating || (inProgress && moving)) return;
            if (passTick) {
                passTick = false;
                return;
            }
            ticks++;
            if (ticks < 5) {
                ticks++;
                return;
            }
            ticks = 0;

            //We stopped moving, but haven't finished
            //the progress, meaning that we still have
            //to place the block.
            if (!moving && inProgress && target != null) {

                //Reselect sand in case we ran out.
                SelectSand();

                player.functions.BlockPlaceOnBlockFace(neighbour.location, neighbour.face);
                passTick = true; // Give time for the block to start faling.

                //Check if we hould use the fast mode,
                //if so then get all the surrounding reachable
                //blocks that we could place in.
                if (this.Setting[2].Get<int>() == 0) {
                    if (targets == null || targets.Length == 0) {
                        this.targets = FindSurroundingTargets();
                    }
                    //Check if we got any targets.
                    int placed = 0;
                    if (targets != null && targets.Length != 0) {

                        for (int i = 0; i < this.targets.Length; i++) {

                            if (this.targets[i].Compare(this.target)) continue;
                            if (placed > 4) break;

                            //Attempt to find a neighbour block
                            //and place on it if found.
                            var tneighbour = player.functions.FindValidNeighbour(this.targets[i]);
                            if (tneighbour != null) {
                                player.functions.BlockPlaceOnBlockFace(tneighbour.location, tneighbour.face);
                                placed++;
                            }
                        }
                    }
                }
                
                //Check if the row is complete.
                if (IsRowDone()) {
                    this.ClearTarget();
                    this.inProgress = false;

                        //// Calculat the position that isn't on the sand anymore.
                        //var offsand = new Location((int)Math.Ceiling(this.player.status.entity.location.X-0.5),
                        //                           (int)this.player.status.entity.location.Y,
                        //                           (int)Math.Ceiling(this.player.status.entity.location.Z - 0.5));

                        //var tempmap = player.functions.AsyncMoveToLocation(offsand, stopToken, PathOptions); // Make it move over the block a litte bit.
                        //moving = true;

                        ////Hook the callbacks.
                        //tempmap.Completed += areaMap => { this.moving = false; this.ClearTarget(); };
                        //tempmap.Cancelled += (areaMap, cuboid) => { this.moving=false; this.ClearTarget(); };

                        //// Go forward, to not be
                        //// standing on the sand.
                        //tempmap.Start();
                }

                return;
            }

            //Find the target.
            if (Setting[4].Get<bool>()) this.target = FindTarget();
            else this.target = FindTargetClosest();

            if (this.target == null) return;
            if (!Setting[4].Get<bool>()) //Don't add if we are not moving.
                Taken.TryAdd(this.target, player);

            //Update each tick, in case the user
            //updates the setting runtime.
            if (!Setting[3].Get<bool>()) PathOptions.Unwalkable = UNWALKABLE;
            else PathOptions.Unwalkable = null;

            //Get a neighbouring block.
            neighbour = player.functions.FindValidNeighbour(this.target, false, PathOptions.Unwalkable);
            if (neighbour == null) {
                
                ClearTarget();
                return;
            }

            //We will start the movement process.
            inProgress = true;
            moving = true;

            //Select sand in same tick 
            //so that once pathing is done
            //we have enough ticks passed to 
            //register the change.
            SelectSand();

            //Attempt to path to the location of the neighbour 
            //plus a bit over it so we can place blocks on it's side.
            var map = player.functions.AsyncMoveToLocation(neighbour.location, stopToken, PathOptions); // Make it move over the block a litte bit.
            map.Offset = CalculateOffset(target, neighbour.location);

            //Check if we should not move
            //and just start placing sand.
            if (Setting[4].Get<bool>()) {
                moving = false;
                OnPathReached(map.Offset);
                return;
            }

            //Hook the callbacks.
            map.Completed += areaMap => OnPathReached(map.Offset);
            map.Cancelled += (areaMap, cuboid) => OnPathFailed();

            //Start pathing.
            map.Start();
        }
        private bool inProgress = false;
        private bool moving = false;
        private bool passTick = true;
        private int ticks = 0;
        private IFacedLocation neighbour;

        private void SelectSand() {

            //We should eat, check if we have food.
            if (player.status.containers.inventory.hotbar.FindId(Sands) == -1) {
                //We should find it in inventory.
                //Find the slot.
                var slot = player.status.containers.inventory.inner.FindId(Sands);
                if (slot != -1)
                    player.status.containers.inventory.hotbar.BringToHotbar(7, slot, null);
            }

            //Check if we have any block in the hotbar.
            var blockSlot = player.status.containers.inventory.hotbar.FindId(Sands);
            //Check if found any healing food.
            if (blockSlot != -1) 
                //Select the item in hotbar.
                player.functions.SetHotbarSlot((short) (blockSlot - 36));
        }

        private ILocation FindTarget() {
            
            //Search from top to bottom.
            //(As that is easier to manager)
            for (int y = (int)totalRadius.start.y + totalRadius.height; y >= (int)totalRadius.start.y - totalRadius.height; y--)
                for (int x = totalRadius.start.x; x < totalRadius.start.x + totalRadius.xSize; x++)
                    for (int z = totalRadius.start.z; z < totalRadius.start.z + totalRadius.zSize; z++)
                    {
                        //Check if the block is valid for mining.
                        if (Taken.ContainsKey(new Location(x, y, z)) || player.world.GetBlockId(x, y, z) != 0 || player.status.entity.location.Distance(new Position(x + 0.5f, y, z + 0.5f)) > MAX_RANGE)
                            continue;

                        return new Location(x, y, z);
                    }

            return null;
        }

        private ILocation FindTargetClosest() {

            List<ILocation> locations = new List<ILocation>();
            //Search from top to bottom.
            //(As that is easier to manager)
            for (int y = (int)totalRadius.start.y + totalRadius.height; y >= (int)totalRadius.start.y - totalRadius.height; y--)
                for (int x = totalRadius.start.x; x < totalRadius.start.x + totalRadius.xSize; x++)
                    for (int z = totalRadius.start.z; z < totalRadius.start.z + totalRadius.zSize; z++)
                    {
                        //Check if the block is valid for mining.
                        if (Taken.ContainsKey(new Location(x, y, z)) || player.world.GetBlockId(x, y, z) != 0)
                            continue;

                        locations.Add(new Location(x, y,z));
                    }

            return locations.OrderBy(x => x.Distance(player.status.entity.location.ToLocation(-1))).FirstOrDefault();
        }
        private ILocation[] FindSurroundingTargets() {

            List<ILocation> list = new List<ILocation>();
            for (int y = (int)totalRadius.start.y + totalRadius.height; y >= (int)totalRadius.start.y - totalRadius.height; y--)
                for (int x = totalRadius.start.x; x < totalRadius.start.x + totalRadius.xSize; x++)
                    for (int z = totalRadius.start.z; z < totalRadius.start.z + totalRadius.zSize; z++)
                    {
                        //Check if the block is valid for mining.
                        if (Taken.ContainsKey(new Location(x, y, z)) || player.world.GetBlockId(x, y, z) != 0 || player.status.entity.location.Distance(new Position(x,y,z)) > REACH)
                            continue;

                        list.Add(new Location(x, y, z));
                    }

            return list.ToArray();
        }

        private void Events_onDisconnected(IPlayer player, string reason) {
            ClearTarget();
        }
        
        private void ClearTarget() {

            if (this.target != null) {
                IPlayer obj;
                Taken.TryRemove(target, out obj);
                this.target = null;
            }
            if (this.targets != null && this.targets.Length > 0) {

                IPlayer obj;
                for (int i = 0; i < targets.Length; i++)
                    Taken.TryRemove(targets[i], out obj);
                targets = null;
            }
        }

        private IPosition CalculateOffset(ILocation target, ILocation neighbour) {

            var position = new Position(target.x - neighbour.x, 0, target.z - neighbour.z);
            if (position.X > 0.75) position.X = 0.75;
            if (position.X < -0.75) position.X = -0.75;
            if (position.Z > 0.75) position.Z = 0.75;
            if (position.Z < -0.75) position.Z = -0.75;

            return position;
        }

        private void LookAtSide(IPosition offset) {

            double x = 0, z = 0;
            if (offset.X > 0.7) x = 0.5;
            else if (offset.X < -0.7) x = -0.5;
            if (offset.Z > 0.7) z = 0.5;
            else if (offset.Z < -0.7) z = -0.5;

            var temp = new Position(neighbour.location.x + x + 0.5, neighbour.location.y + 0.3f, neighbour.location.z + 0.5 + z);
            player.functions.LookAt(temp, true);
        }


        private void OnPathReached(IPosition position) {

            if (this.target == null) {
                this.inProgress = false;
                this.moving = false;
                return;
            }
            
            this.LookAtSide(position);
            this.moving = false;
            this.passTick = true; //Pass the tick, so the look could register.
        }
        private void OnPathFailed() {

            this.inProgress = false;
            this.moving = false;
            this.passTick = true;
            ClearTarget(); // Clear target, as other bots might be able to walk to it.
            return;
        }

        private bool IsRowDone() {

            if (target == null) return true;

            var id = player.world.GetBlockId(target.x, (int) Math.Round(target.y), target.z);
            if(Sands.Contains(id)) // Check if it's a falling block.
                return BlocksGlobal.blockHolder.IsSolid(player.world.GetBlockId(target.x, (int)Math.Round(target.y-1), target.z));
            return BlocksGlobal.blockHolder.IsSolid(player.world.GetBlockId(target.x, (int)Math.Round(target.y), target.z));
        }

        #region Shared work

        /// <summary>
        /// Positions that are already taken.
        /// </summary>
        private static ConcurrentDictionary<ILocation, IPlayer> Taken = new ConcurrentDictionary<ILocation, IPlayer>(); 

        #endregion
    }
}
