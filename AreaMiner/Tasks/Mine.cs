using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Blocks;
using OQ.MineBot.Protocols.Classes.Base;

namespace AreaMiner.Tasks
{
    public class Mine : ITask, ITickListener
    {
        private static readonly MapOptions MO  = new MapOptions() { Look = false, Quality = SearchQuality.MEDIUM, Mine = true };

        private readonly ShareManager shareManager;
        private readonly Mode         mode;
        private readonly PathMode     pathMode;
        private readonly ushort[]     ignore;
        private readonly MacroSync    macro;

        private bool       busy;

        public Mine(ShareManager shareManager, Mode mode, PathMode pathMode, ushort[] ignore, MacroSync macro) {
            this.shareManager = shareManager;
            this.mode         = mode;
            this.pathMode     = pathMode;
            this.ignore       = ignore;
            this.macro        = macro;

            if (pathMode == PathMode.Advanced) MO.Mine  = true;
            else                               MO.Mine  = false;
        }

        public override bool Exec() {
            return !status.entity.isDead && !status.eating && !busy && shareManager.AllReached() && !macro.IsMacroRunning();
        }

        public void OnTick() {

            var target = FindNext();
            if (target == null) return;

            busy = true;

            ThreadPool.QueueUserWorkItem(state =>
            {
                var success = actions.WaitMoveToRange(target, token, MO);
                if (!success) {
                    broken.TryAdd(target, DateTime.Now);
                    busy = false;
                }
                else {
                    if (!IsSafe(target)) {
                        broken.TryAdd(target, DateTime.Now);
                        busy = false;
                    }
                    else {

                        actions.SelectBestTool(target);
                        actions.LookAtBlock(target, true);
                        player.tickManager.Register(2, () =>
                        {
                            if (pathMode == PathMode.Advanced) broken.TryAdd(target, DateTime.Now);
                            actions.BlockDig(target, action => {
                                busy = false;
                            });
                        });
                    }
                }
            });
        }

        private ILocation FindNext()
        {

            //Get the area from the radius.
            IRadius playerRadius = shareManager.Get(player);
            if (playerRadius == null) return null;

            //Search from top to bottom.
            //(As that is easier to manager)
            ILocation closest = null;
            double distance = int.MaxValue;
            for (int y = (int)playerRadius.start.y + playerRadius.height; y >= (int)playerRadius.start.y; y--)
                if (closest == null)
                    for (int x = playerRadius.start.x; x <= playerRadius.start.x + playerRadius.xSize; x++)
                        for (int z = playerRadius.start.z; z <= playerRadius.start.z + playerRadius.zSize; z++)
                        {

                            var tempLocation = new Location(x, y, z);
                            //Check if the block is valid for mining.
                            if(player.world.GetBlockId(x, y, z) == 0) continue;
                            if (broken.ContainsKey(tempLocation) &&
                                broken[tempLocation].Subtract(DateTime.Now).TotalSeconds < -15)
                                continue;
                            if (ignore?.Contains(player.world.GetBlockId(x, y, z)) == true)
                                continue;

                            // Check if this block is safe to mine.
                            if (!IsSafe(tempLocation)) continue;

                            if (closest == null)
                            {
                                distance = tempLocation.Distance(player.status.entity.location.ToLocation(0));
                                closest = new Location(x, y, z);
                            }
                            else if (tempLocation.Distance(player.status.entity.location.ToLocation(0)) < distance)
                            {
                                distance = tempLocation.Distance(player.status.entity.location.ToLocation(0));
                                closest = tempLocation;
                            }
                        }

            return closest;
        }
        private bool IsSafe(ILocation location) {

            if (player.world.IsStandingOn(location, player.status.entity.location)) {
                if (!BlocksGlobal.blockHolder.IsSafeToMine(player.world, location, true))
                    return false;
            }
            else if (!BlocksGlobal.blockHolder.IsSafeToMine(player.world, location, false))
                return false;

            return true;
        }

        #region Shared work.

        /// <summary>
        /// Blocks that are having a hard time being mined.
        /// </summary>
        private static readonly ConcurrentDictionary<ILocation, DateTime> broken = new ConcurrentDictionary<ILocation, DateTime>();
        
        #endregion
    }

    public enum Mode
    {
        Accuare,
        Fast
    }

    public enum PathMode
    {
        Advanced,
        Basic
    }
}