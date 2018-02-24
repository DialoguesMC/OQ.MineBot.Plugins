using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Blocks;
using OQ.MineBot.PluginBase.Movement.Maps;
using OQ.MineBot.Protocols.Classes.Base;
using TunnelPlugin.Pattern;

namespace TunnelPlugin.Tasks
{
    public class Tunnel : ITask, ITickListener
    {
        private static readonly MapOptions MO  = new MapOptions() { Look = false, Quality = SearchQuality.MEDIUM, Mine = true, Strict = true, NoCost = true };
        private static readonly MapOptions MOO = new MapOptions() { Look = false, Quality = SearchQuality.MEDIUM, Mine = true };
        private static readonly MapOptions MOH = new MapOptions() { Look = false, Quality = SearchQuality.HIGHEST, Mine = true };

        public static readonly SharedData SHARED = new SharedData();

        private readonly int      height;
        private readonly IPattern pattern;
        private readonly bool     diamondOre;
        private readonly bool     emeraldOre;
        private readonly bool     ironOre;
        private readonly bool     goldOre;
        private readonly bool     redstoneOre;
        private readonly bool     lapisOre;
        private readonly bool     coalOre;
        private readonly MacroSync macro;

        private OreBulk   ores;
        private IAsyncMap currentMap;
        private ILocation directionOffset;
        private bool      reachedHome;
        private bool      moving;
        private int       mult;
        private int       step;
        private int       fails;

        public Tunnel(int height, IPattern pattern, bool diamondOre, bool emeraldOre, bool ironOre, bool goldOre, bool redstoneOre, bool lapisOre, bool coalOre, MacroSync macro) {
            this.height      = height;
            this.pattern     =  pattern;
            this.diamondOre  = diamondOre;
            this.emeraldOre  = emeraldOre;
            this.ironOre     = ironOre;
            this.goldOre     = goldOre;
            this.redstoneOre = redstoneOre;
            this.lapisOre    = lapisOre;
            this.coalOre     = coalOre;
            this.macro       = macro;
        }

        public override bool Exec() {
            return !status.entity.isDead && !status.eating && !moving && (currentMap == null || !currentMap.IsValidMoving()) && !macro.IsMacroRunning() && !inventory.IsFull();
        }

        public override void Start() {
            // Retrieve information from shared data.
            PickDirection();
            PickHome();
        }

        public void OnTick() {

            // Check if we need to move home.
            if (MoveHome())
                return;

            // If we can't branch, then we should dig further.
            if (MineOres())
                return;
            if (!DigBranch()) {
                DigTunnel();
            }
        }

        private void DigTunnel() {

            // Calculate the next position.
            var pos = SHARED.GetHome(player).Offset(directionOffset.Multiply(mult));
            if (!IsTunnelable(player, pos)) {
                mult += pattern.gap;
                return;
            }
            moving = true;

            currentMap = player.functions.AsyncMoveToLocation(pos, token, fails < 8 ? MO : MOH);
            currentMap.Completed += areaMap => {
                fails = 0;
                mult += pattern.gap;
                moving = false;
            };
            currentMap.Cancelled += (areaMap, cuboid) => {
                fails++;
                moving = false;
            };

            if (!currentMap.Start() || (currentMap.Searched && currentMap.Complete && currentMap.Valid))
                moving = false;
        }
        private bool DigBranch() {

            if (mult == 0) return false;

            // Calculate current position
            // where we should branch from.
            var pos = SHARED.GetHome(player).Offset(directionOffset.Multiply(mult - pattern.gap));
            if (!IsTunnelable(player, pos)) {
                return false;
            }

            if (step == 1 || step == 3) { // Reset.

                moving = true;
                currentMap = player.functions.AsyncMoveToLocation(pos, token, MOH);
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
            var temp =wallOffset.Multiply(pattern.lenght);
            var end = pos.Offset(temp);
            if (!IsTunnelable(player, end)) {
                //step++;
                step = 0;
                return false;
            }
            moving = true;

            currentMap = player.functions.AsyncMoveToLocation(end, token, MO);
            currentMap.Completed += areaMap => {
                step++;

                // Collect each location that we just mined.
                ILocation[] locations = new ILocation[pattern.lenght*2];
                for (int i = 0; i < pattern.lenght; i++) {
                    locations[i * 2]     = pos.Offset(wallOffset.Multiply(i + 1).Offset(1));
                    locations[i * 2 + 1] = pos.Offset(wallOffset.Multiply(i + 1).Offset(2));
                }

                // Scan the branch that we just mined.
                var ores = new OreBulk();
                ScanNeighbourChunk(locations, ores);
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

        private bool MineOres() {

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
            var map = player.functions.AsyncMoveToLocation(loc.location.Offset(-1), token, MOO);
            map.Completed += areaMap => {
                ScanNeighbourOres(loc.location, ores); // Scan the block that we are standing on.
                ScanNeighbourOres(loc.location.Offset(2), ores); // Scan the block that our head is in.
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
        private bool MoveHome() {

            if (reachedHome) return false;
            if (moving) return true;

            currentMap = player.functions.AsyncMoveToLocation(SHARED.GetHome(player), token, MOH);
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
        private void PickHome() {
            var loc = player.status.entity.location.ToLocation();
            SHARED.SetHome(player, new Location(loc.x, height - 1, loc.z));
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
        private void ScanNeighbourOres(ILocation location, OreBulk ores) {
            
            // Scan neighbours in an 3x3 area.
            for(int x = -1; x < 2; x++)
                for(int y = -1; y < 2; y++)
                    for (int z = -1; z < 2; z++) {
                        var loc = location.Offset(x, y, z);
                        var id = player.world.GetBlockId(loc);
                        if(IsOre(id, loc)) ores.Add(new OreLocation(loc, id));
                    }
        }
        private void ScanNeighbourChunk(ILocation[] location, OreBulk ores) {
            for(int i = 0; i < location.Length; i++)
                ScanNeighbourOres(location[i], ores);
        }
        // Also checks if the block is minable.
        private bool IsOre(ushort id, ILocation location) {
            if (!BlocksGlobal.blockHolder.IsSafeToMine(player.world, location, true)) return false;
            
            if ((diamondOre  && id == 56) ||
                (emeraldOre  && id == 129) ||
                (ironOre     && id == 15) ||
                (goldOre     && id == 14) ||
                (redstoneOre && (id == 73 || id == 74)) || // Active and inactive redstone.
                (lapisOre    && id == 21) ||
                (coalOre     && id == 16))
            {
                return true;
            }
            return false;
        }
    }

    public class SharedData
    {
        // Position of where all bots
        // will move before starting to tunnel.
        private ConcurrentDictionary<IPlayer, ILocation> HOME;
        // Keeps track of what direction
        // the next bot should mine.
        private int m_direction;

        public SharedData() {
            HOME = new ConcurrentDictionary<IPlayer, ILocation>();
        }

        // Index - direction
        private ILocation[] OFFSET_TABLE = new[]
        {
            new Location(1, 0, 0),
            new Location(0, 0, 1),
            new Location(-1, 0, 0),
            new Location(0, 0, -1),
        };

        // Attempts to set the Home variable.
        public bool SetHome(IPlayer player, ILocation location) {
            HOME.AddOrUpdate(player, location, (player1, location1) => { return location; });
            return true;
        }

        // Returns a direction and increments it.
        public int PickDirection() {
            var dir = m_direction++;
            if (m_direction >= 4) m_direction = 0; // 0-3 directions only, if it's more then reset.
            return dir;
        }

        public ILocation GetHome(IPlayer player) {
            ILocation loc;
            if (!HOME.TryGetValue(player, out loc)) return null;
            return loc;
        }

        // Converts a direction input
        // directional offset.
        public ILocation DirectionToOffset(int dir) {
            return OFFSET_TABLE[dir];
        }

        // Resets all values to default.
        public void Reset() {
            m_direction = 0;
            HOME = new ConcurrentDictionary<IPlayer, ILocation>();
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