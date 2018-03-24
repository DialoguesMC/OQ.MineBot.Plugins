using System;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes;

namespace NoFallPlugin.Tasks
{
    public class CancelUpdate : ITask
    {
        public override bool Exec() {
            return true;
        }

        public override void Start() { player.events.onPlayerUpdate += EventsOnOnPlayerUpdate; }
        public override void Stop() { player.events.onPlayerUpdate -= EventsOnOnPlayerUpdate; }
        private void EventsOnOnPlayerUpdate(IStopToken cancel) { cancel.Stop(); }
    }
}