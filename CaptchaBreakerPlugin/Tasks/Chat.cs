using System;
using System.Collections.Generic;
using System.Linq;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Base;

namespace CaptchaBreakerPlugin.Tasks
{
    public class Chat : ITask
    {
        public readonly string trigger, pattern, command;

        public Chat(string trigger, string pattern, string command) {
            this.trigger = trigger;
            this.pattern = pattern;
            this.command = command;
        }

        public override bool Exec() {
            return true;
        }

        public override void Start() {
            player.events.onChat += OnChat;   
        }
        public override void Stop() {
            player.events.onChat -= OnChat;
        }

        private void OnChat(IPlayer player, IChat message, byte position) {
            if (!message.Parsed.Contains(trigger))
                return;
            Console.WriteLine("[DCaptchaBreaker] - Captcha request DETECTED. Solving.");
            string[] strArray1 = pattern.Split(Convert.ToChar(" "));
            int index1 = 0;
            string[] strArray2 = strArray1;
            for (int index2 = 0; index2 < strArray2.Length; ++index2)
                if (strArray2[index2] == "%captcha%")
                    break;
                else index1++;

            Console.WriteLine("[DCaptchaBreaker] - Captcha found at position : " + (object)index1 + " .");
            string[] strArray3 = message.Parsed.Split(Convert.ToChar(" "));
            Console.WriteLine("[DCaptchaBreaker] - Captcha found, it should be : " + ((IEnumerable<string>)strArray3).ElementAt<string>(index1) + " . Sending captcha to server..");
            if (string.IsNullOrWhiteSpace(command)) {
                Console.WriteLine("[DCaptchaBreaker] - No captcha command found. Sending.");
                player.functions.Chat(((IEnumerable<string>)strArray3).ElementAt<string>(index1));
            }
            else {
                Console.WriteLine("[DCaptchaBreaker] - Captcha command found. Sending captcha command + captcha.");
                player.functions.Chat(command + " " + strArray3.ElementAt<string>(index1));
            }
        }
    }
}