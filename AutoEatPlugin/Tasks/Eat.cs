using System;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;

namespace AutoEatPlugin.Tasks
{
    public class Eat : ITask, IHungerListener, IHealthListener, ITickListener
    {

        private static readonly ushort[] HFOOD = { 322 };
        private static readonly ushort[] HSOUP = { 282 };
        private static readonly ushort[] FOOD = { 260, 297, 319, 320, 350, 357, 360, 364, 366, 391, 393, 400, 424 };

        private readonly int m_minHealth;
        private readonly int m_minFood;
        private readonly bool m_soup;
        private readonly bool m_active;

        private DateTime m_last;

        public Eat(int minHealth, int minFood, bool soup, bool active) {
            this.m_minHealth = minHealth;
            this.m_minFood   = minFood;
            this.m_soup      = soup;
            this.m_active    = active;
            this.m_last = DateTime.MinValue;
        }

        public override bool Exec() {
            return !status.entity.isDead && !status.eating && 
                   DateTime.Now.Subtract(m_last).TotalSeconds > 4 &&
                   ((m_minFood != -1 && status.entity.food <= m_minFood) || 
                   (m_minHealth != -1 && status.entity.health <= m_minHealth));
        }

        public void OnHungerChanged(int hunger) {
            if (m_minFood != -1 && hunger > m_minFood) return;

            if (inventory.Select(FOOD) == -1) return;
            EatFood(false);
        }

        public void OnHealthChanged(int health) {
            if (m_minHealth != -1 && health > m_minHealth) return;

            if (m_soup && inventory.Select(HSOUP) != -1) EatFood(true);
            else if(inventory.Select(HFOOD) != -1)       EatFood(false);
        }


        public void OnTick() {
            if (!m_active) return;
            OnHealthChanged((int)status.entity.health);
            OnHungerChanged(status.entity.food);
        }

        private void EatFood(bool instant) {
            m_last = DateTime.Now;
            player.tickManager.Register(1, () => { // Give time for server to register the item change.
                if(!instant) actions.EatAsync();
                else player.functions.UseSelectedItem();
            });
        }
    }
}