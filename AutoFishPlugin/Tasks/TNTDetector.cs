using System;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Objects;
using OQ.MineBot.PluginBase.Classes.Objects.List;
using OQ.MineBot.Protocols.Classes.Base;

namespace AutoFishPlugin.Tasks
{
    public class TNTDetector : ITask
    {
        public override bool Exec() {
            return !status.entity.isDead;
        }

        public override void Start() { player.events.onObjectSpawned += OnObjectSpawned; }
        public override void Stop()  { player.events.onObjectSpawned -= OnObjectSpawned; }

        private void OnObjectSpawned(IWorldObject worldObject, double d, double d1, double d2, byte pitch, byte yaw) {
            
            var fallingOjbect = worldObject as FallingBlockObject;
            if (fallingOjbect != null && fallingOjbect.BlockType == 46) {
                actions.Disconnect("TNT Detected [Auto fisher]");
            }
        }
    }
}