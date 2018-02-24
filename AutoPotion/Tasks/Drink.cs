using System;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Entity;

namespace AutoPotion.Tasks
{
    public class Drink : ITask, ITickListener
    {
        private const  int            POTION      = 373;
        private static readonly int[] STRENGTH    = { 8201, 8233, 8265 };
        private static readonly int[] STRENGTHS   = { 16393, 16425, 16457 };
        private static readonly int[] SPEED       = { 8194, 8226, 8258 };
        private static readonly int[] SPEEDS      = { 16386, 16418, 16450 };
        private static readonly int[] FIRERES     = { 8227, 8259 };
        private static readonly int[] FIRERESS    = { 16378, 16451 };
        private static readonly int[] HEALTHS     = { 16421, 16389, 16453 };

        private readonly bool strenght, speed, fire;
        private readonly int  health;

        private bool busy;
        private DateTime m_lastEat;
        private DateTime m_lastThrow;

        public Drink(bool strenght, bool speed, bool fire, int health) {
            this.strenght = strenght;
            this.speed    = speed;
            this.fire     = fire;
            this.health   = health;
        }

        public override bool Exec() {
            return !status.entity.isDead && !status.eating && !busy &&
                   DateTime.Now.Subtract(m_lastThrow).TotalMilliseconds > 500;
        }

        public void OnTick() {

            if (health > 0 && CheckHealth()) return;

            if (DateTime.Now.Subtract(m_lastEat).TotalSeconds < 4)     return;
            if (strenght && CheckEffect(Effects.Strength, STRENGTH))   return;
            if (speed && CheckEffect(Effects.Speed, SPEED))            return;
            if (fire && CheckEffect(Effects.Fire_Resistance, FIRERES)) return;
        }

        private bool CheckEffect(Effects effect, int[] meta) {

            var currentEffect = player.status.entity.effects.Get(effect);
            if (currentEffect != null && !currentEffect.Expired) return false; // Still have potion effect.

            if (!inventory.Select(POTION, meta)) return false;
            else {
                Eat();
                return true;
            }
        }

        private bool CheckHealth() {

            if (player.status.entity.health > health) return false;

            if (!inventory.Select(POTION, HEALTHS)) return false;
            else {
                Throw();
                return true;
            }
        }

        private void Eat() {
            m_lastEat = DateTime.Now;
            player.tickManager.Register(1, () => { // Give time for the server to register slot swich.
                actions.EatAsync();
            });
        }

        private void Throw() {

            //Look at the ground.
            busy = true;
            var rotation = player.status.entity.rotation;
            rotation.pitch = 90;
            player.functions.Look(new IRotation[] { rotation }, true);

            player.tickManager.Register(2, () => {
                player.functions.UseSelectedItem(); // Throw potion.
                m_lastThrow = DateTime.Now;
                busy = false;
            });
        }
    }
}