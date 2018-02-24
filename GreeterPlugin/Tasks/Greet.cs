using System;
using System.Linq;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity;
using OQ.MineBot.PluginBase.Classes.Physics;

namespace GreeterPlugin.Tasks
{
    public class Greet : ITask
    {
        private DateTime start;
        private DateTime last;

        private readonly string message;
        private readonly int    minDelay;
        private readonly int    chance;

        public Greet(string message, int minDelay, int chance) {
            this.start    = DateTime.Now;
            this.last     = DateTime.MinValue;

            this.message  = message;
            this.minDelay = minDelay;
            this.chance   = chance;
        }

        public override bool Exec() { return true; }
        public override void Start() {
            player.entities.onNameAdded += OnNameAdded;
        }
        public override void Stop() {
            player.entities.onNameAdded -= OnNameAdded;
        }

        private void OnNameAdded(UUID uuid) {
            if (start.Subtract(DateTime.Now).TotalSeconds > -10) return;

            if (uuid.Name.Contains('§') || string.IsNullOrWhiteSpace(uuid.Name)) return;
            if (rnd.Next(0, 101) > chance) return;
            if (last.Subtract(DateTime.Now).TotalSeconds > minDelay) return; // Type only every X seconds
            last = DateTime.Now;

            player.functions.Chat(message.Replace("%new_player%", uuid.Name));
        }
        private static readonly Random rnd = new Random();
    }
}