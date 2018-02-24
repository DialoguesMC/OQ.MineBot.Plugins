using OQ.MineBot.PluginBase.Base.Plugin.Tasks;

namespace AreaMiner.Tasks
{
    public class InventoryMonitor : ITask, ITickListener
    {
        private readonly MacroSync macro;
        private readonly string    macroName;

        public InventoryMonitor(string name, MacroSync macro) {
            this.macro     = macro;
            this.macroName = name;
        }

        public override bool Exec() {
            return !status.entity.isDead && !status.eating && !macro.IsMacroRunning() && inventory.IsFull();
        }

        public void OnTick() {
            macro.Run(player, macroName);
        }
    }
}