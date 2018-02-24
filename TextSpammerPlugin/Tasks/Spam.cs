using System;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;

namespace TextSpammerPlugin.Tasks
{
    public class Spam : ITask, ITickListener
    {
        private static readonly Random random = new Random();

        private readonly string[] messages;
        private readonly int minDelay;
        private readonly int maxDelay;
        private readonly bool antiSpam;
        private readonly bool linear;

        private DateTime m_next;
        private int line;

        public Spam(string[] messages, int minDelay, int maxDelay, bool antiSpam, bool linear) {
            this.messages = messages;
            this.m_next   = DateTime.Now;
            this.minDelay = minDelay;
            this.maxDelay = maxDelay;
            this.antiSpam = antiSpam;
            this.linear   = linear;
        }

        public override bool Exec() {
            return DateTime.Now.Subtract(m_next).TotalMilliseconds > 0;
        }

        public void OnTick() {

            // Schedule next message.
            m_next =
                DateTime.Now.AddMilliseconds(maxDelay == -1 // Check if Max delay is disabled.
                    ? minDelay // Max delay disabled, use min delay.
                    : random.Next(Math.Min(minDelay, maxDelay),
                                  Math.Max(minDelay, maxDelay))
                    );

            // Post chat message.
            string message = null;
            if (!linear) message = messages[random.Next(0, messages.Length)];
            else {
                message = messages[line++];
                if (line >= messages.Length) line = 0;
            }

            if (!string.IsNullOrWhiteSpace(message))
                player.functions.Chat(message +
                                     (antiSpam ? random.Next(0, 9999).ToString() : ""));

        }
    }
}