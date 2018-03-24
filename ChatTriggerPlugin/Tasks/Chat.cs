using System;
using System.Linq;
using System.Threading.Tasks;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Base;

namespace ChatTriggerPlugin.Tasks
{
    public class Chat : ITask
    {
        private readonly string[] users, keywords;
        private readonly string   macro;
        private readonly MacroSync macroData = new MacroSync();

        public Chat(string[] users, string[] keywords, string macro) {
            this.users = users;
            this.keywords = keywords;
            this.macro = macro;
        }

        public override bool Exec() {
            return !macroData.IsMacroRunning();
        }

        public override void Start() { player.events.onChat += OnChat; }
        public override void Stop() { player.events.onChat -= OnChat; }

        private void OnChat(IPlayer player, IChat message, byte position) {
            
            for(int i = 0; i < users.Length; i++)
                if (message.Parsed.Contains(users[i])) {
                    var rightSide = message.Parsed.Substring(message.Parsed.LastIndexOf(users[i]) + users[i].Length);
                    if (keywords.Any(rightSide.Contains) && !macroData.IsMacroRunning()) {
                        macroData.Run(player, macro);
                    }
                }
        }
    }
}

public class MacroSync
{
    private Task macroTask;

    public bool IsMacroRunning() {
        //Check if there is an instance of the task.
        if (macroTask == null) return false;
        //Check completion state.
        return !macroTask.IsCompleted && !macroTask.IsCanceled && !macroTask.IsFaulted;
    }

    public void Run(IPlayer player, string name) {
        macroTask = player.functions.StartMacro(name);
    }
}