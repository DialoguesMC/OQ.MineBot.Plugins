using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Blocks;

namespace OreMinerPlugin.Tasks
{
    public class Mine : ITask, ITickListener
    {
        private static readonly MapOptions MO = new MapOptions() { Look = false, Quality = SearchQuality.HIGHEST, Mine = true };

        private readonly ushort[]  ids;
        private readonly MacroSync macro;

        private bool      busy;
        private ILocation location;

        public Mine(bool diamondOre, bool emeraldOre, bool ironOre, bool goldOre, bool redstoneOre, bool lapisOre, bool coalOre, MacroSync macro) {
            this.macro = macro;

            var list = new List<ushort>();
            if (diamondOre) list.Add(56);
            if(emeraldOre)  list.Add(129);
            if(ironOre)     list.Add(15);
            if(goldOre)     list.Add(14);
            if(redstoneOre) { list.Add(73); list.Add(74); }
            if(lapisOre)    list.Add(21);
            if(coalOre)     list.Add(16);
            ids = list.ToArray();
        }

        public override bool Exec() {
            return !status.entity.isDead && !status.eating && !busy && !macro.IsMacroRunning() && !inventory.IsFull();
        }

        public void OnTick() {

            if (location == null) {
                FindClosestOre();
                return;
            }
            
            var token = new CancelToken();
            var map = actions.AsyncMoveToLocation(location, token, MO);
            map.Completed += areaMap => {
                TaskCompleted();
            };
            map.Cancelled += (areaMap, cuboid) => {
                if(!token.stopped) { 
                    token.Stop();
                    InvalidateBlock(location);
                    TaskCompleted();
                }
            };

            if (!map.Start()) {
                if (!token.stopped) {
                    token.Stop();
                    InvalidateBlock(location);
                    TaskCompleted();
                }
            }
            else busy = true;
        }

        private void FindClosestOre() {

            busy = true;
            world.FindFirstAsync(player, player.status.entity.location.ToLocation(0), 64, 256, ids, IsSafe, loc =>
            {
                beingMined.TryAdd(loc, null);
                this.location = loc.Offset(-1);
                this.busy     = false;
            });
        }

        private bool IsSafe(ILocation location) {

            if (beingMined.ContainsKey(location) || personalBlocks.ContainsKey(location)) return false;
            return IsTunnelable(location);
        }

        private bool IsTunnelable(ILocation pos)
        {

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

        private void TaskCompleted() {
            this.location = null;
            this.busy = false;
        }

        private void InvalidateBlock(ILocation location) {
            if(location != null) personalBlocks.TryAdd(location, null);
        }

        #region Shared work.

        /// <summary>
        /// Blocks that are taken already.
        /// </summary>
        public static ConcurrentDictionary<ILocation, object> beingMined = new ConcurrentDictionary<ILocation, object>();
        private readonly ConcurrentDictionary<ILocation, object> personalBlocks = new ConcurrentDictionary<ILocation, object>(); 

        #endregion
    }
}