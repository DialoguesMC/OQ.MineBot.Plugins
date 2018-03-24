using System;
using System.Collections.Concurrent;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes;

namespace SugarcaneFarmerPlugin.Tasks
{
    public class Farm : ITask, ITickListener
    {
        private const int SUGARCANE     = 83;
        private readonly MapOptions MO  = new MapOptions() { Look = false, Quality = SearchQuality.LOW, AntiStuck = false };

        private readonly int x, z;
        private readonly Mode mode;

        private bool scan;
        private bool scanning;
        private ILocation[] locations;
        private bool busy;

        public Farm(int x, int z, Mode mode) {
            this.x = x;
            this.z = z;
            this.mode = mode;
            this.scan = true;
            this.personalBlocks.Clear();
        }

        public void OnStart() {
            player.events.onWorldReload += player1 => scan = true;
        }

        public override bool Exec() {
            return !status.entity.isDead && !inventory.IsFull() && !status.eating &&
                   !scanning && !busy && player.status.containers.GetWindow("minecraft:chest") == null;
        }

        private int tick = 0;
        public void OnTick() {

            // Check if we should be re-scanning
            // the world for new sugarcane blocks.
            if (scan) {
                ScanArea();
                return;
            }

            tick++;
            if (tick > (mode == Mode.Accurate ? 6 : 3)) tick = 0;
            else                                        return;

            var location = FindNext();
            if(location == null) return;

            busy = true;
            beingMined.TryAdd(location, null);
            if (mode == Mode.Fast && player.world.InRange(status.entity.location, location)) {
                Mine(location.Offset(1));
            }
            else {
                var map = actions.AsyncMoveToLocation(location, token, MO);
                map.Completed += areaMap => {
                    Mine(location.Offset(1));
                };
                map.Cancelled += (areaMap, cuboid) => {
                    object obj; beingMined.TryRemove(location, out obj);
                    busy = false;
                    personalBlocks.AddOrUpdate(location, new BlockTimer(), (location1, timer) => {
                        timer.Block();
                        return timer;
                    });
                };
                map.Start();
            }
        }

        // Scans the area and finds all the
        // sugarcane blocks.
        private void ScanArea() {
            scanning = true;
            scan = false;

            world.FindAsync(player, status.entity.location.ToLocation(), x, z, SUGARCANE, tempBlocks => {

                for (int i = tempBlocks.Length - 1; i >= 0; i--) {
                    // If the block below isn't sugarcane, then this
                    // is the origin growing point.
                    if (player.world.GetBlockId(tempBlocks[i].x, (int)tempBlocks[i].y - 1, tempBlocks[i].z) == SUGARCANE)
                        tempBlocks[i] = null;
                }

                locations = tempBlocks;
                scanning = false;
            });
        }

        private void Mine(ILocation target) {
            actions.LookAtBlock(target, true);
            player.tickManager.Register(3, () => {
                actions.BlockDig(target, action => {
                    busy = false;
                    object obj; beingMined.TryRemove(target.Offset(-1), out obj);
                });
            });
        }

        private ILocation FindNext() {
            if (locations == null) return null;
            ILocation nextMove = null;
            double distance = int.MaxValue;
            for (int i = 0; i < locations.Length; i++)
                if (locations[i] != null && player.world.GetBlockId(locations[i].x, (int)locations[i].y + 1, locations[i].z) == SUGARCANE) {

                    //Create the location.
                    var loc = locations[i];

                    //Check if already being mined.
                    if (beingMined.ContainsKey(loc)) continue;
                    if (personalBlocks.ContainsKey(loc))
                        if (personalBlocks[loc].Blocked()) continue;
            
                    //Check by difference.
                    double tempDistance = loc.Distance(player.status.entity.location.ToLocation());
                    if (nextMove == null) {
                        distance = tempDistance;
                        nextMove = loc;
                    }
                    else if (tempDistance < distance) {
                        distance = tempDistance;
                        nextMove = loc;
                    }
                }
            return nextMove;
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
        readonly ConcurrentDictionary<ILocation, BlockTimer> personalBlocks = new ConcurrentDictionary<ILocation, BlockTimer>();
        
        #endregion
    }

    public enum Mode
    {
        Accurate,
        Tick,
        Fast
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