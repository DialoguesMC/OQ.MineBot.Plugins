using System;
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
using OQ.MineBot.PluginBase.Classes.Entity.Filter;
using OQ.MineBot.PluginBase.Classes.Entity.Lists;
using OQ.MineBot.PluginBase.Movement.Maps;
using OQ.MineBot.Protocols.Classes.Base;
using TunnelPlugin.Pattern;

namespace TunnelPlugin
{
    public class PluginCore : IStartPlugin
    {
        public MapOptions PATH = new MapOptions() { Look = false, Quality = SearchQuality.MEDIUM, Mine = true, Strict = true, NoCost = true };
        public MapOptions PATH_ORE = new MapOptions() { Look = false, Quality = SearchQuality.MEDIUM, Mine = true};
        public MapOptions PATH_HOME = new MapOptions() { Look = false, Quality = SearchQuality.HIGHEST, Mine = true };

        public static SharedData SHARED = new SharedData();

        public static IPattern PATTERN_SELECTED = new GapTwo();
        public static IPattern[] PATTERNS =
        {
            new GapOne(), 
            new GapTwo(),
            new GapThree(),
        };

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return "Tunneler";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription()
        {
            return "\"Digs tunnles and mine ores\" - Temm, 2017";
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
        public IPluginSetting[] Setting { get; set; } =
        {
            new NumberSetting("Height", "Height level that the bots should mine at", 12, 1, 256),
            new ComboSetting("Pattern", "", new []{PATTERNS[0].GetName(), PATTERNS[1].GetName(), PATTERNS[2].GetName()}, 1),
            new StringSetting("Macro on inventory full", "Starts the macro when the bots inventory is full.", ""),
            new BoolSetting("Diamond ore", "", true),
            new BoolSetting("Emerald ore", "", true),
            new BoolSetting("Iron ore", "", true),
            new BoolSetting("Gold ore", "", true),
            new BoolSetting("Redstone ore", "", false),
            new BoolSetting("Lapis Lazuli ore", "", false),
            new BoolSetting("Coal ore", "", false),
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
        public void OnEnabled() {
            PATTERN_SELECTED = PATTERNS[Setting[1].Get<int>()];
            stopToken.Reset();
        }

        /// <summary>
        /// Called once the plugin is disabled.
        /// Meaning the start methods will not be
        /// called.
        /// </summary>
        public void OnDisabled() { SHARED.Reset(); }

        /// <summary>
        /// The plugin should be stopped.
        /// </summary>
        public void Stop() { stopToken.Stop(); }
        
        /// <summary>
        /// Return an instance of this plugin.
        /// </summary>
        /// <returns></returns>
        public IPlugin Copy() { return (IStartPlugin)MemberwiseClone(); }

        private OreBulk ores;

        private ILocation directionOffset;
        private bool reachedHome;
        private bool moving;
        private int mult;
        private CancelToken stopToken = new CancelToken();

        private Task macroTask;
        private IAsyncMap currentMap;

        private int step = 0;
        private int fails = 0;

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
                Console.WriteLine("[AreaMiner] 'Load world' must be enabled.");
                return new PluginResponse(false, "'Load world' must be enabled.");
            }
            if (player.settings.staticWorlds) { // Warning
                Console.WriteLine("[AreaMiner] 'Shared worlds' should be disabled.");
            }

            // Register new stop token.
            stopToken = new CancelToken();

            // Retrieve information from shared data.
            PickDirection();
            PickHome(player);

            // Register events.
            player.physicsEngine.onPhysicsPreTick += Tick;

            return new PluginResponse(true);
        }

        private void Tick(IPlayer player) {
            
            //Check if we should stop the plugin.
            if (stopToken.stopped) {
                player.physicsEngine.onPhysicsPreTick -= Tick;
                return;
            }
            
            //Check if we are not busy at the moment.
            if (player.status.entity.isDead || player.status.eating
                || moving || IsMacroRunning() || currentMap?.IsValidMoving() == true)
                return;

            //Check if we are full.
            if (Setting[2].value != null && !string.IsNullOrWhiteSpace(Setting[2].Get<string>()) &&
                player.status.containers.inventory.hotbar.FindFreeSlot() == -1 &&
                player.status.containers.inventory.inner.FindFreeSlot() == -1) {
                //Inventory is full, do full events.
                macroTask = player.functions.StartMacro(Setting[2].Get<string>());
                return;
            }


            // Check if we need to move home.
            if (MoveHome(player))
                return;

            // If we can't branch, then we should dig further.
            if (MineOres(player))
                return;
            if (!DigBranch(player)) {
                DigTunnel(player);
            }
        }

        private void DigTunnel(IPlayer player) {

            // Calculate the next position.
            var pos = SHARED.Home.Offset(directionOffset.Multiply(mult));
            if (!IsTunnelable(player, pos)) {
                mult += PATTERN_SELECTED.gap;
                return;
            }
            moving = true;

            currentMap = player.functions.AsyncMoveToLocation(pos, stopToken, fails < 8 ? PATH: PATH_HOME);
            currentMap.Completed += areaMap => {
                fails = 0;
                mult += PATTERN_SELECTED.gap;
                moving = false;
            };
            currentMap.Cancelled += (areaMap, cuboid) => {
                fails++;
                moving = false;
            };

            if (!currentMap.Start() || (currentMap.Searched && currentMap.Complete && currentMap.Valid))
                moving = false;
        }
        private bool DigBranch(IPlayer player) {

            if (mult == 0) return false;

            // Calculate current position
            // where we should branch from.
            var pos = SHARED.Home.Offset(directionOffset.Multiply(mult - PATTERN_SELECTED.gap));
            if (!IsTunnelable(player, pos)) {
                return false;
            }

            if (step == 1 || step == 3) { // Reset.

                moving = true;
                currentMap = player.functions.AsyncMoveToLocation(pos, stopToken, PATH_HOME);
                currentMap.Completed += areaMap =>
                {
                    if (step == 3) {
                        step = 0;
                    }
                    else step++;
                    moving = false;
                };
                currentMap.Cancelled += (areaMap, cuboid) => {
                    moving = false;
                };
                if (!currentMap.Start() || (currentMap.Searched && currentMap.Complete && currentMap.Valid))
                    moving = false;

                return true;
            }

            // Swap x and z to get offset
            // to walls instead of tunenl.
            ILocation wallOffset = new Location(directionOffset.z, directionOffset.y, directionOffset.x);
            if (step == 2) { // If we are on step 1 then we should mine the other side. 
                wallOffset = wallOffset.Multiply(-1);
            }

            // Calculate the position we should check for a valid branch.
            var start = pos.Offset(wallOffset);
            if (!BlocksGlobal.blockHolder.IsSolid(player.world.GetBlockId(start.Offset(1))) ||
                !BlocksGlobal.blockHolder.IsSolid(player.world.GetBlockId(start.Offset(2))))
                return false;

            // Calculate the position we should mine to.
            var temp =wallOffset.Multiply(PATTERN_SELECTED.lenght);
            var end = pos.Offset(temp);
            if (!IsTunnelable(player, end)) {
                //step++;
                step = 0;
                return false;
            }
            moving = true;

            currentMap = player.functions.AsyncMoveToLocation(end, stopToken, PATH);
            currentMap.Completed += areaMap => {
                step++;

                // Collect each location that we just mined.
                ILocation[] locations = new ILocation[PATTERN_SELECTED.lenght*2];
                for (int i = 0; i < PATTERN_SELECTED.lenght; i++) {
                    locations[i * 2]     = pos.Offset(wallOffset.Multiply(i + 1).Offset(1));
                    locations[i * 2 + 1] = pos.Offset(wallOffset.Multiply(i + 1).Offset(2));
                }

                // Scan the branch that we just mined.
                var ores = new OreBulk();
                ScanNeighbourChunk(player, locations, ores);
                if(!ores.IsEmpty()) this.ores = ores;
                moving = false;
            };
            currentMap.Cancelled += (areaMap, cuboid) => {
                step++;
                moving = false;
            };
            if (!currentMap.Start() || (currentMap.Searched && currentMap.Complete && currentMap.Valid))
                moving = false;
            return true;
        }

        private bool MineOres(IPlayer player) {

            // Check if we have an ore set, if we don't
            // then we still haven't dug out a valid branch.
            if (ores == null) return false;

            // Check if the ore list is empty, if it's
            // not empty take out locations and use them
            // until we run out.
            var loc = ores.Take();
            if (loc == null) {
                ores = null;
                return false;
            }
            // Check if the ore is still the same block
            // (if not we have already mined it)
            if (player.world.GetBlockId(loc.location) != loc.id) {
                return true; // Ignore this ore.
            }
            
            // By moving to the ores location we 
            // guarantee to mine it and pick it up.
            moving = true;

            // Offset Y by -1 so that the bot would stand on the location
            // instead of stand above it.
            var map = player.functions.AsyncMoveToLocation(loc.location.Offset(-1), stopToken, PATH_ORE);
            map.Completed += areaMap => {
                ScanNeighbourOres(player, loc.location, ores); // Scan the block that we are standing on.
                ScanNeighbourOres(player, loc.location.Offset(2), ores); // Scan the block that our head is in.
                moving = false;
            };
            map.Cancelled += (areaMap, cuboid) => {
                moving = false; // Remove the current ore and
                                // ignore it (by not readding it)
            };
            //Check if the path is instantly completed.
            if(!map.Start() || (map.Searched && map.Complete && map.Valid))
                moving = false;
                return true;
        }

        /// Attempts to move to the shared home location
        /// if the bot has not reached it before.
        /// <returns>True if we are moving to the location.</returns>
        private bool MoveHome(IPlayer player) {

            if (reachedHome) return false;
            if (moving) return true;

            currentMap = player.functions.AsyncMoveToLocation(SHARED.Home, stopToken, PATH_HOME);
            currentMap.Completed += areaMap => {
                reachedHome = true;
                moving = false;
            };
            currentMap.Cancelled += (areaMap, cuboid) => {
                reachedHome = false;
                moving = false;
            };

            //Start moving.
            this.moving = true;
            currentMap.Start();
            return true;
        }

        private void PickDirection() {
            directionOffset = SHARED.DirectionToOffset(SHARED.PickDirection());
        }
        private void PickHome(IPlayer player) {
            var loc = player.status.entity.location.ToLocation();
            SHARED.SetHome(new Location(loc.x, Setting[0].Get<int>() - 1, loc.z));
        }

        private bool IsTunnelable(IPlayer player, ILocation pos) {

            return !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(1))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(2))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(3))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(1, 1, 0))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(1, 2, 0))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(0, 1, 1))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(0, 2, 1))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(-1, 1, 0))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(-1, 2, 0))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(0, 1, -1))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(0, 2, -1)))
                   ;
        }
        private void ScanNeighbourOres(IPlayer player, ILocation location, OreBulk ores) {
            
            // Scan neighbours in an 3x3 area.
            for(int x = -1; x < 2; x++)
                for(int y = -1; y < 2; y++)
                    for (int z = -1; z < 2; z++) {
                        var loc = location.Offset(x, y, z);
                        var id = player.world.GetBlockId(loc);
                        if(IsOre(player, id, loc)) ores.Add(new OreLocation(loc, id));
                    }
        }
        private void ScanNeighbourChunk(IPlayer player, ILocation[] location, OreBulk ores) {
            for(int i = 0; i < location.Length; i++)
                ScanNeighbourOres(player, location[i], ores);
        }
        // Also checks if the block is minable.
        private bool IsOre(IPlayer player, ushort id, ILocation location) {
            if (!BlocksGlobal.blockHolder.IsSafeToMine(player.world, location, true)) return false;
            
            if ((Setting[3].Get<bool>() && id == 56) ||
                (Setting[4].Get<bool>() && id == 129) ||
                (Setting[5].Get<bool>() && id == 15) ||
                (Setting[6].Get<bool>() && id == 14) ||
                (Setting[7].Get<bool>() && (id == 73 || id == 74)) || // Active and inactive redstone.
                (Setting[8].Get<bool>() && id == 21) ||
                (Setting[9].Get<bool>() && id == 16))
            {
                return true;
            }
            return false;
        }

        private bool IsMacroRunning() {
            //Check if there is an instance of the task.
            if (macroTask == null) return false;
            //Check completion state.
            return !macroTask.IsCompleted && !macroTask.IsCanceled && !macroTask.IsFaulted;
        }
    }

    public class SharedData
    {
        // Position of where all bots
        // will move before starting to tunnel.
        public ILocation Home { get; set; }
        // Keeps track of what direction
        // the next bot should mine.
        private int m_direction;

        // Index - direction
        private ILocation[] OFFSET_TABLE = new[]
        {
            new Location(1, 0, 0),
            new Location(0, 0, 1),
            new Location(-1, 0, 0),
            new Location(0, 0, -1),
        };

        // Attempts to set the Home variable.
        public bool SetHome(ILocation location) {
            if (Home != null) return false;
            Home = location;
            return true;
        }

        // Returns a direction and increments it.
        public int PickDirection() {
            var dir = m_direction++;
            if (m_direction >= 4) m_direction = 0; // 0-3 directions only, if it's more then reset.
            return dir;
        }

        // Converts a direction input
        // directional offset.
        public ILocation DirectionToOffset(int dir) {
            return OFFSET_TABLE[dir];
        }

        // Resets all values to default.
        public void Reset() {
            m_direction = 0;
            Home = null;
        }
    }

    public class OreBulk
    {
        /// <summary>
        /// Locations of ores that are 
        /// connected to one another.
        /// </summary>
        private readonly List<OreLocation> locations = new List<OreLocation>();

        public bool IsEmpty() {
            return locations.Count == 0;
        }
        public void Add(OreLocation location) {
            // Check if this location still doesn't exist.
            for(int i = 0; i < locations.Count; i++)
                if (locations[i].location.Compare(location.location))
                    return; // Same location already exists.
            locations.Add(location);
        }
        public OreLocation Take() {
            if (locations.Count == 0) return null;

            var loc = locations[0];
            locations.RemoveAt(0);

            return loc;
        }
    }

    public class OreLocation
    {
        public ushort id;
        public ILocation location;

        public OreLocation(ILocation location, ushort id) {
            this.location = location;
            this.id = id;
        }
    }
}
