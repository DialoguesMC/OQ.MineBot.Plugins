using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity;
using OQ.MineBot.PluginBase.Classes.Entity.Lists;
using OQ.MineBot.PluginBase.Classes.Items;
using OQ.MineBot.PluginBase.Classes.Window.Containers.Subcontainers;
using OQ.MineBot.PluginBase.Pathfinding;

namespace AutoEatPlugin
{
    public class PluginCore : IStartPlugin
    {
        public static int[] HealingFood = {322};
        public static int[] HealingSoup = { 282 };
        public static int[] Food = { 260, 297, 319, 320, 350, 357, 360, 364, 366, 391, 393, 400 };

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Auto eater";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription() {
            return "Eats food when hungry, eats gapples when needed.";
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
            return "1.00.01";
        }

        /// <summary>
        /// All settings should be stored here.
        /// (NULL if there shouldn't be any settings)
        /// </summary>
        public IPluginSetting[] Setting { get; set; } =
        {
            new NumberSetting("Eat when hunger is below X", "When should the bot eat normal food (-1 if it shouldn't eat them).", -1, -1, 19, 14),
            new NumberSetting("Eat gapples when below X hp", "When should the bot eat golden apples (-1 if it shouldn't eat them).", -1, -1, 20, 1),
            new ComboSetting("Mode", null, new string[] { "Efficient", "Accurate" }, 0),
            new BoolSetting("Soup", "Can the bot use soup for healing?", false),
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
            return (IStartPlugin)MemberwiseClone();
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

            if (Setting[2].Get<int>() == 2) {

                //Hook health/food change.
                player.events.onHealthUpdate += OnHealthUpdate;
                //Call the changed event in case the player
                //is at minimum food/health already.
                OnHealthUpdate(player, player.status.entity.health, player.status.entity.food,
                    player.status.entity.foodSaturation);
            }
            else
                player.events.onTick += OnTick;

            return new PluginResponse(true);
        }

        private void OnTick(IPlayer player)
        {
            //Check if we should un-hook, in case the
            //plugin is stopped.
            if (stopToken.stopped) {
                player.events.onTick -= OnTick;
                return;
            }

            //Check if already eating.
            if (player.status.eating || DateTime.Now.Subtract(lastEat).TotalSeconds < 4) return;

            //Check if our health is below the requirement to eat.
            if (Setting[1].Get<int>() != -1 && Setting[1].Get<int>() >= player.status.entity.health) {

                //We have reached the min health treshold, meaning
                //we should attempt to find food and eat it.

                if (player.status.containers.inventory.Select(HealingFood) != -1) {
                    //Wait for the server to notice the call.
                    Thread.Sleep(50);
                    //Start eating.
                    player.functions.EatAsync();
                    lastEat = DateTime.Now;
                    return; //We started eating, don't search for other food.
                }

                //Check if we should search for soups.
                if (Setting[3].Get<bool>()) {
                    if (player.status.containers.inventory.Select(HealingSoup)) {
                        //Wait for the server to notice the call.
                        Thread.Sleep(50);
                        //Don't start the eating process, just right click
                        //the soup item as they are processes instantly
                        //by the server.
                        player.functions.UseSelectedItem();
                        lastEat = DateTime.Now;
                        return; //We started eating, don't search for other food.
                    }
                }
            }

            //Health is fine, check if we need to eat normal food.
            if (Setting[0].Get<int>() != -1 && Setting[0].Get<int>() >= player.status.entity.food) {

                //We have reached the min food treshold, meaning
                //we should attempt to find food and eat it.

                if (player.status.containers.inventory.Select(Food) != -1) {
                    //Wait for the server to notice the call.
                    Thread.Sleep(50);
                    //Start eating.
                    player.functions.EatAsync();
                    lastEat = DateTime.Now;
                    return; //We started eating, don't search for other food.
                }
            }
        }

        private DateTime lastEat = DateTime.MinValue;
        private void OnHealthUpdate(IPlayer player, float health, int food, float foodSaturation) {

            //Check if we should un-hook, in case the
            //plugin is stopped.
            if (stopToken.stopped) {
                player.events.onHealthUpdate -= OnHealthUpdate;
                return;
            }

            //Check if already eating.
            if (player.status.eating || DateTime.Now.Subtract(lastEat).TotalSeconds < 4) return;
            lastEat = DateTime.Now;
            
            //Check if our health is below the requirement to eat.
            if (Setting[1].Get<int>() != -1 && Setting[1].Get<int>() >= health) {

                //We should each, as the requirement is reached.

                //Check if we should get out apples to our hotbar.
                if (player.status.containers.inventory.hotbar.FindId(HealingFood) == -1) { //We should find it in inventory.
                    //Find the slot.
                    var slot = player.status.containers.inventory.inner.FindId(HealingFood);
                    if (slot != -1) {
                        player.status.containers.inventory.hotbar.BringToHotbar(8, slot, null); //Slot 8 for healing food.
                    }
                }

                //Check if we have any food in the hotbar.
                var healingSlot = player.status.containers.inventory.hotbar.FindId(HealingFood);
                //Check if found any healing food.
                if (healingSlot != -1) {
                    //Select the item in hotbar.
                    player.functions.SetHotbarSlot((short)(healingSlot-36));
                    //Wait for the server to notice the call.
                    Thread.Sleep(50);
                    //Start eating.
                    player.functions.EatAsync();
                    return; //We started eating, don't search for other food.
                }   
            }

            //Health is fine, check if we need to eat normal food.
            if (Setting[0].Get<int>() != -1 && Setting[0].Get<int>() >= food) {

                //We should eat, check if we have food.
                if (player.status.containers.inventory.hotbar.FindId(Food) == -1) { //We should find it in inventory.
                    //Find the slot.
                    var slot = player.status.containers.inventory.inner.FindId(Food);
                    if (slot != -1) {
                        player.status.containers.inventory.hotbar.BringToHotbar(7, slot, null); //Slot 7 for normal food.
                    }
                }

                //Check if we have any food in the hotbar.
                var foodSlot = player.status.containers.inventory.hotbar.FindId(Food);
                //Check if found any healing food.
                if (foodSlot != -1) {
                    //Select the item in hotbar.
                    player.functions.SetHotbarSlot((short)(foodSlot-36));
                    //Wait for the server to notice the call.
                    Thread.Sleep(50);
                    //Start eating.
                    player.functions.EatAsync();
                }
            }
        }
    }
}
