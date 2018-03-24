using System;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Blocks;
using OQ.MineBot.Protocols.Classes.Base;

namespace DiscoliWanderPlugin.Tasks
{
    public class Wander : ITask, ITickListener
    {
        private static readonly Random random = new Random();
        private static readonly MapOptions MO = new MapOptions() {Look = true, Quality = SearchQuality.LOWEST};

        private bool busy;

        public override bool Exec() {
            return !status.entity.isDead && !busy;
        }

        public void OnTick() {
            // Generate random move locations.
            int x   = (int)status.entity.location.X + random.Next(-10, 20);
            float y = (float)status.entity.location.Y;
            int z   = (int) status.entity.location.Z + random.Next(-10, 20);
            ILocation location = new Location(x, y, z);
            location = ToGround(location);
            

            busy = true;
            var map = actions.AsyncMoveToLocation(location, token, MO);
            map.Completed += areaMap => {
                busy = false;
            };
            map.Cancelled += (areaMap, cuboid) => {
                busy = false;
            };

            if (!map.Start()) busy = false;
            if (!map.Valid) actions.LookAtBlock(location);
        }

        // This function is also under location.ToGround(world).
        // Added here to not require bot update to work with plugin.
        private ILocation ToGround(ILocation location) {

            int y = (int)location.y;
            while (!BlocksGlobal.blockHolder.IsSolid(world.GetBlockId(location.x, (int)y, location.z))) {
                if (y <= 0) return null;
                y--;
            }
            return new Location(location.x, y, location.z);
        }
    }
}