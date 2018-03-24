using System.Net.Configuration;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity;
using OQ.MineBot.PluginBase.Classes.Entity.Lists;
using OQ.MineBot.PluginBase.Classes.Entity.Player;

namespace ShieldPlugin.Tasks
{
    public class Follow : ITask, ITickListener
    {
        private static readonly MapOptions LMO = new MapOptions() { Look = true, Quality = SearchQuality.LOWEST };

        private ILiving ownerEntity;
        private UUID    ownerUuid;
        private string  ownerInput;

        private readonly ResolvableNameCollection names;
        private readonly Mode                     mode;

        private bool busy;

        public Follow(string ownerInput, Mode mode, ResolvableNameCollection names) {
            this.ownerInput = ownerInput;
            this.mode       = mode;
            this.names      = names;
        }

        public override bool Exec() {
            return !status.entity.isDead && !busy;
        }

        private int ticks = 0;
        public void OnTick() {

            if (ownerEntity == null || ownerEntity.unloaded) this.ownerEntity = GetOwner();
            if (names.HasUnresolved())                       this.names.Resolve(player.entities);

            if (ownerEntity == null || ownerEntity.unloaded) return;

            ticks++;
            if (ticks < 5) return;
            ticks = 0;

            ILiving target = GetClosestFollowTarget();
            if (target == null) return;

            busy = true;
            var map = actions.AsyncMoveToEntity(target, token, LMO);
            map.Completed += areaMap => {
                ticks = 0;
                busy  = false;
            };
            map.Cancelled += (areaMap, cuboid) => {
                ticks = -10;
                busy  = false;
            };
            map.WaypointReached += areaMap => {
                var next = GetClosestFollowTarget();
                if (next != null && next.location.Distance(player.status.entity.location) < 1 && player.physicsEngine.path?.Complete == false)
                    areaMap.CalculateFromNext(player.world, next);
                else // Calculate path to the newest owners location.
                    areaMap.CalculateFromNext(player.world, ownerEntity);
            };

            map.Start();

            if (map.Searched && map.Complete && map.Valid)
                busy = false;
        }

        private ILiving GetClosestFollowTarget() {
            ILiving moveTarget = ownerEntity;
            if (ownerEntity == null) return null;

            ILiving enemy = player.entities.FindClosestTarget(status.entity.location.ToLocation(), Targeter.DefaultFilter);
            if (enemy != null && mode == Mode.Aggresive && enemy.location.Distance(ownerEntity.location) < 5) moveTarget = enemy;
            else if (ownerEntity.location.Distance(status.entity.location) < 1) return null;
            return moveTarget;
        }

        private ILiving GetOwner() {

            if (ownerUuid == null) {
                if (ownerInput.Length == 32 || ownerInput.Length == 36) {
                    this.ownerUuid = new UUID(ownerInput, null);
                }
                else {
                    this.ownerUuid = player.entities.FindUuidByName(ownerInput);
                    if(ownerUuid != null)
                        Targeter.IgnoreList.Add(ownerUuid.Uuid);
                }
            }
            if (ownerUuid == null) return null;

            foreach (var entity in player.entities.playerList) {
                if(entity.Value == null) continue;
                if (((IPlayerEntity)entity.Value).uuid == ownerUuid.Uuid.Replace("-", ""))
                    return entity.Value;
            }
            
            return null;
        }
    }
}