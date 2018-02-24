using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Blocks;
using OQ.MineBot.Protocols.Classes.Base;

namespace SandPrinterPlugin.Tasks
{
    public class Print : ITask, ITickListener
    {
        private static readonly int[]      SAND       = new[] { 12, 13 };
        private static readonly MapOptions MO         = new MapOptions() { Look = false, Quality = SearchQuality.LOW, AntiStuck = false };
        private static readonly ushort[]   UNWALKABLE = new[] { (ushort)12 };
        private const float REACH     = 4;
        private const float MAX_RANGE = 5;
        
        private readonly Mode mode;
        private readonly bool swalk;
        private readonly bool nmovement;

        private IFacedLocation neighbour;
        private IRadius        totalRadius;
        private ILocation      target;
        private ILocation[]    targets;
        private bool           busy;


        public Print(Mode mode, IRadius radius, bool swalk, bool nmovmenet) {
            this.totalRadius = radius;
            this.mode        = mode;
            this.swalk       = swalk;
            this.nmovement   = nmovmenet;
        }

        public override bool Exec() {
            return !status.entity.isDead && !busy && !status.eating && inventory.FindId(SAND) != -1;
        }

        public void OnTick() {

            if (nmovement) this.target = FindTarget();
            else           this.target = FindTargetClosest();

            if (this.target == null) return;
            if (!nmovement) Taken.TryAdd(this.target, player); //Don't add if we are not moving.

            if (!swalk) MO.Unwalkable = UNWALKABLE;
            else        MO.Unwalkable = null;

            neighbour = actions.FindValidNeighbour(target, false, MO.Unwalkable);
            if (neighbour == null) {
                ClearTarget();
                return;
            }

            busy = true;
            var map = player.functions.AsyncMoveToLocation(neighbour.location, token, MO); // Make it move over the block a litte bit.
            map.Offset = CalculateOffset(target, neighbour.location);

            if (nmovement) {
                PathReached(map.Offset);
                return;
            }

            map.Completed += areaMap => PathReached(map.Offset);
            map.Cancelled += (areaMap, cuboid) => {
                this.busy = false;
                ClearTarget();
            };

            map.Start();
        }

        private void PathReached(IPosition position) {

            LookAtSide(position);
            IStopToken token = null;
            token = player.tickManager.RegisterReocurring(3, () => {
                
                if(this.token.stopped) {
                    token.Stop();
                    return;
                }

                inventory.Select(SAND.Select(x=>(ushort)x).ToArray()); // Scuffed.
                actions.BlockPlaceOnBlockFace(neighbour.location, neighbour.face);

                if (mode == Mode.Fast) {
                    if (targets == null || targets.Length == 0) targets = FindSurroundingTargets();
                    int placed = 0;
                    if (targets != null && targets.Length != 0) {
                        for (int i = 0; i < this.targets.Length; i++) {

                            if (this.targets[i].Compare(this.target)) continue;
                            if (placed > 4) break;
                            
                            var tneighbour = player.functions.FindValidNeighbour(this.targets[i]);
                            if (tneighbour != null) {
                                actions.BlockPlaceOnBlockFace(tneighbour.location, tneighbour.face);
                                placed++;
                            }
                        }
                    }
                }

                if (IsRowDone()) {
                    token.Stop();
                    this.ClearTarget();
                    busy = false;
                }
            });
        }

        private ILocation FindTarget() {

            var h = (int)totalRadius.start.y + totalRadius.height;
            for (int x = totalRadius.start.x; x < totalRadius.start.x + totalRadius.xSize + 1; x++)
                for (int z = totalRadius.start.z; z < totalRadius.start.z + totalRadius.zSize + 1; z++) {
                    if (Taken.ContainsKey(new Location(x, h, z)) || player.world.GetBlockId(x, h, z) != 0 || player.status.entity.location.Distance(new Position(x + 0.5f, h, z + 0.5f)) > MAX_RANGE)
                        continue;

                    return new Location(x, h, z);
                }

            return null;
        }

        private ILocation FindTargetClosest() {

            var h = (int)totalRadius.start.y + totalRadius.height;

            List<ILocation> locations = new List<ILocation>();
            for (int x = totalRadius.start.x; x < totalRadius.start.x + totalRadius.xSize + 1; x++)
                for (int z = totalRadius.start.z; z < totalRadius.start.z + totalRadius.zSize + 1; z++) {
                    if (Taken.ContainsKey(new Location(x, h, z)) || player.world.GetBlockId(x, h, z) != 0)
                        continue;

                    locations.Add(new Location(x, h, z));
                }

            return locations.OrderBy(x => x.Distance(player.status.entity.location.ToLocation(-1))).FirstOrDefault();
        }

        private ILocation[] FindSurroundingTargets() {

            var h = (int)totalRadius.start.y + totalRadius.height;
            List<ILocation> list = new List<ILocation>();
            for (int x = totalRadius.start.x; x < totalRadius.start.x + totalRadius.xSize + 1; x++)
                    for (int z = totalRadius.start.z; z < totalRadius.start.z + totalRadius.zSize + 1; z++) {
                        //Check if the block is valid for mining.
                        if (Taken.ContainsKey(new Location(x, h, z)) || player.world.GetBlockId(x, h, z) != 0 || player.status.entity.location.Distance(new Position(x,h,z)) > REACH)
                            continue;

                        list.Add(new Location(x, h, z));
                    }

            return list.ToArray();
        }

        private IPosition CalculateOffset(ILocation target, ILocation neighbour) {

            var position = new Position(target.x - neighbour.x, 0, target.z - neighbour.z);
            if (position.X > 0.75) position.X = 0.75;
            if (position.X < -0.75) position.X = -0.75;
            if (position.Z > 0.75) position.Z = 0.75;
            if (position.Z < -0.75) position.Z = -0.75;

            return position;
        }

        private bool IsRowDone() {

            if (target == null) return true;

            var id = player.world.GetBlockId(target.x, (int) Math.Round(target.y), target.z);
            if(SAND.Contains(id)) // Check if it's a falling block.
                return BlocksGlobal.blockHolder.IsSolid(player.world.GetBlockId(target.x, (int)Math.Round(target.y-1), target.z));
            return BlocksGlobal.blockHolder.IsSolid(player.world.GetBlockId(target.x, (int)Math.Round(target.y), target.z));
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

        #region Shared work

        /// <summary>
        /// Positions that are already taken.
        /// </summary>
        private static ConcurrentDictionary<ILocation, IPlayer> Taken = new ConcurrentDictionary<ILocation, IPlayer>();

        #endregion
    }

    public enum Mode
    {
        Fast,
        Accurate
    }
}