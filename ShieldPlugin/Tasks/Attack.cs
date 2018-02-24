using System;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Entity.Lists;

namespace ShieldPlugin.Tasks
{
    public class Attack : ITask, ITickListener
    {
        private static readonly Random rnd = new Random();

        private int hitTicks;

        private readonly int  CPS;
        private readonly int  MS;
        private readonly bool autoWeapon;

        public Attack(int CPS, int MS, bool autoWeapon) {
            this.CPS        = CPS;
            this.MS         = MS;
            this.autoWeapon = autoWeapon;
        }

        public override bool Exec() {
            return !status.entity.isDead && !status.eating;
        }

        public void OnTick() {
            Hit();
        }

        private void Hit() {
            
            var closestPlayer = player.entities.FindClosestTarget(player.status.entity.location.ToLocation(), Targeter.DefaultFilter);
            if (closestPlayer != null) {

                if(autoWeapon) actions.EquipWeapon();
                actions.LookAt(closestPlayer.location, true);
                
                // 1 hit tick is about 50 ms.
                hitTicks++;
                int ms = hitTicks * 50;

                if (ms >= (1000 / CPS)) {
                    
                    hitTicks = 0; //Hitting, reset tick counter.
                    if (rnd.Next(1, 101) < MS) actions.PerformSwing(); //Miss.
                    else actions.EntityAttack(closestPlayer.entityId); //Hit.
                }
            }
        }
    }
}