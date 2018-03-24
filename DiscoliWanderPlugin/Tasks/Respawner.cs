using OQ.MineBot.PluginBase.Base.Plugin.Tasks;

namespace DiscoliWanderPlugin.Tasks
{
    public class Respawner : ITask, IDeathListener
    {
        private bool busy;

        public override bool Exec() {
            return !busy;
        }

        public void OnDeath() {
            // Wait a few ticks before respawning, in case
            // we are in a loop.
            busy = true;
            player.tickManager.Register(5, () => {
                actions.PerformaRespawn();
                busy = false;
            });
        }
    }
}