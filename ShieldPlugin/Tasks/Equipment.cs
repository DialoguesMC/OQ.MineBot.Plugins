using System.Threading;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Items;
using OQ.MineBot.PluginBase.Classes.Window.Containers.Subcontainers;

namespace ShieldPlugin.Tasks
{
    public class Equipment : ITask, IInventoryListener
    {
        private readonly bool autoGear;
        private bool busy;

        public Equipment(bool autoGear) {
            this.autoGear = autoGear;
        }
        
        public override bool Exec() {
            return autoGear && !busy && !status.entity.isDead && !status.eating;
        }

        public override void Start() {
            OnInventoryChanged();
        }

        public void OnInventoryChanged() {

            if (autoGear) {
                busy = true;
                player.functions.OpenInventory();

                ThreadPool.QueueUserWorkItem(state => {
                    Thread.Sleep(250);
                    if (player.functions.EquipBest(EquipmentSlots.Head,
                        ItemsGlobal.itemHolder.helmets))
                        Thread.Sleep(250);
                    if (player.functions.EquipBest(EquipmentSlots.Chest,
                        ItemsGlobal.itemHolder.chestplates))
                        Thread.Sleep(250);
                    if (player.functions.EquipBest(EquipmentSlots.Pants,
                        ItemsGlobal.itemHolder.leggings))
                        Thread.Sleep(250);
                    player.functions.EquipBest(EquipmentSlots.Boots,
                        ItemsGlobal.itemHolder.boots);
                    Thread.Sleep(250);
                    player.functions.CloseInventory();
                    busy = false;
                });
            }
        }
        public void OnSlotChanged(ISlot slot) { }
        public void OnItemAdded(ISlot slot) { }
        public void OnItemRemoved(ISlot slot) { }
    }
}