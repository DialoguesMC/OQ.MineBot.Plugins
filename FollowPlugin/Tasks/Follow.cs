using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity;
using OQ.MineBot.PluginBase.Classes.Entity.Lists;
using OQ.MineBot.PluginBase.Classes.Entity.Player;

namespace FollowPlugin.Tasks
{
    public class Follow : ITask, ITickListener
    {
        private static readonly MapOptions HMO = new MapOptions() { Look = true, Quality = SearchQuality.HIGH };
        private static readonly MapOptions LMO = new MapOptions() { Look = true, Quality = SearchQuality.LOW };

        private string  ownerName;
        private UUID    ownerUuid;
        private ILiving ownerEntity;

        private bool busy;
        private MovementState state;

        public Follow(string ownerName) {
            this.ownerName = ownerName;
            this.state     = MovementState.HIGH_NOTFOUND;
        }

        public override bool Exec() {
            return !status.entity.isDead && !busy;
        }

        private int ticks;
        public void OnTick() {

            ticks++;
            if (ticks < 4) return;
            ticks = 0;

            if (ownerEntity == null) this.ownerEntity = GetOwner();
            if (ownerEntity == null || ownerEntity.unloaded) return;

            this.busy = true;
            var currentOptions = state != MovementState.FOUND ? LMO : HMO;

            var map = actions.AsyncMoveToEntity(ownerEntity, token, currentOptions);
            map.Completed += areaMap => {

                state = MovementState.FOUND;
                this.busy = false;
            };
            map.Cancelled += (areaMap, cuboid) => {

                if (state == MovementState.FOUND)             state = MovementState.LOW_NOTFOUND;
                else if (state == MovementState.LOW_NOTFOUND) state = MovementState.HIGH_NOTFOUND;
                this.ticks = -(int)state;
                this.busy = false;
            };
            map.Start();
        }

        private ILiving GetOwner() {

            if (ownerUuid == null) {
                if (ownerName.Length == 32 || ownerName.Length == 36) this.ownerUuid = new UUID(ownerName, null);
                else {
                    this.ownerUuid = player.entities.FindUuidByName(ownerName);
                    if (ownerUuid != null)
                        Targeter.IgnoreList.Add(ownerUuid.Uuid);
                }
            }

            if (ownerUuid == null) return null;
            
            foreach (var entity in player.entities.playerList) {
                if (entity.Value == null) continue;
                if (((IPlayerEntity)entity.Value).uuid == ownerUuid.Uuid.Replace("-", ""))
                    return entity.Value;
            }

            return null;
        }
    }

    public enum MovementState
    {
        FOUND = 5,
        LOW_NOTFOUND = 16,
        HIGH_NOTFOUND = 32,
    }
}