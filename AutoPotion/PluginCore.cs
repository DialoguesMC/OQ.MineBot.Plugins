using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity;

namespace AutoPotion
{
    public class PluginCore : IStartPlugin
    {
        public static int PotionId = 373;

        public static int[] Strength = {8201, 8233, 8265};
        public static int[] StrengthSplash = {16393, 16425, 16457};

        public static int[] Speed = {8194, 8226, 8258};
        public static int[] SpeedSplash = {16386, 16418, 16450};

        public static int[] FireResistance = {8227, 8259};
        public static int[] FireResistanceSplash = {16378, 16451};

        public static int[] HealthSplash = { 16421, 16389, 16453 };

        private DateTime lastEat = DateTime.MinValue;

        private int[] AwaitingThrow;

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Auto potion";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription() {
            return "Drinks potions when the effects run out.";
        }

        /// <summary>
        /// Author of the plugin.
        /// </summary>
        /// <returns></returns>
        public PluginAuthor GetAuthor() {
            return new PluginAuthor("OnlyQubes");
        }

        /// <summary>
        /// Version of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetVersion() {
            return "1.00.00";
        }

        /// <summary>
        /// All settings should be stored here.
        /// (NULL if there shouldn't be any settings)
        /// </summary>
        public IPluginSetting[] Setting { get; set; } =
            {
                new BoolSetting("Strength", null, true),
                new BoolSetting("Speed", null, true),
                new BoolSetting("Fire resistance", null, true),
                new NumberSetting("Health", "At how much health should the bot use health potions.", 10, -1, 20, 1),
            };

        /// <summary>
        /// Called once the plugin is loaded.
        /// (Params are the version of the program)
        /// (This is not reliable as if "Load plugins" 
        /// isn't enabled this will not be called)
        /// </summary>
        /// <param name="version"></param>
        /// <param name="subversion"></param>
        /// <param name="buildversion"></param>
        public void OnLoad(int version, int subversion, int buildversion) {
        }

        /// <summary>
        /// Called once the plugin is enabled.
        /// Meaning the start methods will be
        /// called when needed.
        /// </summary>
        public void OnEnabled() {
            stopToken.Reset();
        }

        /// <summary>
        /// Called once the plugin is disabled.
        /// Meaning the start methods will not be
        /// called.
        /// </summary>
        public void OnDisabled() {
        }

        /// <summary>
        /// The plugin should be stopped.
        /// </summary>
        public void Stop() {
            stopToken.Stop();
        }

        private CancelToken stopToken = new CancelToken();

        /// <summary>
        /// Return an instance of this plugin.
        /// </summary>
        /// <returns></returns>
        public IPlugin Copy() {
            return (IStartPlugin) MemberwiseClone();
        }

        /// <summary>
        /// Called once a "player" logs
        /// in to the server.
        /// (This does not start a new thread, so
        /// if you want to do any long temn functions
        /// please start your own thread!)
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PluginResponse OnStart(IPlayer player) {

            //Check if bot settings are valid.
            if (!player.settings.loadInventory) {
                Console.WriteLine("[AutoEater] 'Load inventory' must be enabled.");
                return new PluginResponse(false, "'Load inventory' must be enabled.");
            }

            player.events.onTick += OnTick;
            player.physicsEngine.onRotationChanged += PhysicsEngine_onRotationChanged;

            return new PluginResponse(true);
        }

        private void PhysicsEngine_onRotationChanged(IPlayer player, IRotation rotaion) {

            //Check if we looked down.
            if (rotaion.pitch != 90 || AwaitingThrow == null) return;

            //Get the slot and select it.
            var healingSlot = player.status.containers.inventory.hotbar.FindId(PotionId, AwaitingThrow);
            player.functions.SetHotbarSlot((short)(healingSlot - 36));
            
            player.functions.UseSelectedItem(); // Throw potion.
        }

        private void OnTick(IPlayer player) {
            //Check if we should un-hook, in case the
            //plugin is stopped.
            if (stopToken.stopped) {
                player.events.onTick -= OnTick;
                return;
            }

            //Throw spash potions more often.
            if (player.status.eating || player.status.entity.isDead || DateTime.Now.Subtract(lastEat).TotalMilliseconds < 500) return;
            if (Setting[3].Get<int>() > 0 && CheckHealth(player, Setting[3].Get<int>(), HealthSplash))
                return;

            //Check if already eating.
            if (player.status.eating || DateTime.Now.Subtract(lastEat).TotalSeconds < 4) return;
            if (Setting[0].Get<bool>() && CheckEffect(player, Effects.Strength, Strength))
                return;
            if (Setting[1].Get<bool>() && CheckEffect(player, Effects.Speed, Speed))
                return;
            if (Setting[2].Get<bool>() && CheckEffect(player, Effects.Fire_Resistance, FireResistance))
                return;
        }

        private bool CheckEffect(IPlayer player, Effects effect, int[] meta) {

            var currentEffect = player.status.entity.effects.Get(effect);
            player.status.containers.inventory.FindId(0);
            if (currentEffect != null && !currentEffect.Expired) return false; // Still have potion effect.
            if (player.status.containers.inventory.hotbar.FindId(PotionId, meta) == -1) {
                var slot = player.status.containers.inventory.inner.FindId(PotionId, meta);
                if (slot != -1)
                    player.status.containers.inventory.hotbar.BringToHotbar(8, slot, null); //Slot 8 for healing food.
            }

            var healingSlot = player.status.containers.inventory.hotbar.FindId(PotionId, meta);
            if (healingSlot != -1) {
                player.functions.SetHotbarSlot((short) (healingSlot - 36));
                Thread.Sleep(50);
                player.functions.EatAsync();
                lastEat = DateTime.Now;
                return true;
            }

            return false; // Potion not found.
        }

        private bool CheckHealth(IPlayer player, int health, int[] meta) {

            if (player.status.entity.health > health) return false;

            if (player.status.containers.inventory.hotbar.FindId(PotionId, meta) == -1) {
                var slot = player.status.containers.inventory.inner.FindId(PotionId, meta);
                if (slot != -1)
                    player.status.containers.inventory.hotbar.BringToHotbar(8, slot, null); //Slot 8 for healing food.
            }

            var healingSlot = player.status.containers.inventory.hotbar.FindId(PotionId, meta);
            if (healingSlot != -1) {

                //Look at the ground.
                var rotation = player.status.entity.rotation;
                rotation.pitch = 90;
                player.functions.Look(new IRotation[] { rotation }, true);

                //Register that we are waiting
                //to throw a potion.
                AwaitingThrow = meta;
                lastEat = DateTime.Now;

                return true;
            }

            return false; // Potion not found.
        }
    }
}