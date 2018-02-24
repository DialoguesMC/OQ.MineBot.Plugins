using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;

namespace AreaMiner.Tasks
{
    public class Path : ITask, ITickListener
    {
        private static readonly MapOptions ZMO = new MapOptions() { Look = true, Quality = SearchQuality.HIGH, Mine = true };

        private readonly IRadius      radius;
        private readonly ShareManager shareManager;
        private readonly PathMode     pathMode;
        private readonly MacroSync    macro;

        private bool busy;

        public Path(ShareManager shareManager, ILocation start, ILocation end, PathMode pathMode, MacroSync macro) {
            this.shareManager = shareManager;
            this.pathMode     = pathMode;
            this.macro        = macro;

            if (pathMode == PathMode.Advanced) ZMO.Mine = true;
            else                               ZMO.Mine = false;
            radius = new IRadius(start, end);
            this.shareManager.SetArea(radius);
        }

        public override void Start() {
            this.shareManager.Add(player, radius);
        }

        public override bool Exec() {
            return !status.entity.isDead && !status.eating && !busy && !shareManager.MeReached(player) && !macro.IsMacroRunning();
        }

        public void OnTick() {

            var zone = shareManager.Get(this.player);
            if (zone == null) return;
            ILocation center = zone.GetClosestWalkable(player.world, player.status.entity.location.ToLocation(), true);
            if (center == null) return;

            var map = player.functions.AsyncMoveToLocation(center, token, ZMO);
            map.Completed += areaMap => {
                shareManager.RegisterReached(this.player);
                busy = false;
            };
            map.Cancelled += (areaMap, cuboid) => {
                busy = false;
            };
            map.Start();
            busy = true;
        }
    }
}