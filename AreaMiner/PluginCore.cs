using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Blocks;
using OQ.MineBot.PluginBase.Classes.Entity;
using OQ.MineBot.PluginBase.Classes.Entity.Player;
using OQ.MineBot.PluginBase.Classes.Items;
using OQ.MineBot.PluginBase.Classes.Materials;
using OQ.MineBot.PluginBase.Classes.Physics;
using OQ.MineBot.PluginBase.Pathfinding;
using OQ.MineBot.Protocols.Classes.Base;

namespace AreaMiner
{
    public class PluginCore : IStartPlugin
    {
        /// <summary>
        /// How accurate pathing should be.
        /// </summary>
        public MapOptions PathOptions = new MapOptions() { Look = false, Quality = SearchQuality.MEDIUM, Mine = true };

        private IPlayer player;

        private IRadius totalRadius;

        private ILocation target;
        private bool moving;
        private bool equiped;
        private bool mining;

        private bool skipTick;
        private int  ticks;

        private Task macroTask;

        private int[] ignoreIds;

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Area miner";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription() {
            return "Mines the area that is selected by the user.";
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
            return "1.02.00";
        }

        /// <summary>
        /// All settings should be stored here.
        /// (NULL if there shouldn't be any settings)
        /// </summary>
        public IPluginSetting[] Setting { get; set; } =
        {
            new StringSetting("Start x y z", "", "0 0 0"),
            new StringSetting("End x y z", "", "0 0 0"),
            new StringSetting("Macro on inventory full", "Starts the macro when the bots inventory is full.", ""),
            new ComboSetting("Speed mode", null, new string[] { "Accurate", "Fast" }, 0),
            new StringSetting("Ignore ids", "What blocks should be ignored.", ""),
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

            var startSplit = Setting[0].Get<string>().Split(' ');
            var endSplit = Setting[1].Get<string>().Split(' ');
            if (startSplit.Length != 3 || endSplit.Length != 3) {
                Console.WriteLine("[AreaMiner] Invalid coordinates (must be x y z).");
                return;
            }
            Shares = new ShareManager(new IRadius(new Location(int.Parse(startSplit[0]), int.Parse(startSplit[1]), int.Parse(startSplit[2])),
                                                  new Location(int.Parse(endSplit[0]), int.Parse(endSplit[1]), int.Parse(endSplit[2]))));
            stopToken.Reset();
        }

        /// <summary>
        /// Called once the plugin is disabled.
        /// Meaning the start methods will not be
        /// called.
        /// </summary>
        public void OnDisabled() {
            beingMined.Clear();
            broken.Clear();
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

            // Avoid starting the bot without having it enabled
            // in the plugins tab.
            if (Shares == null) {
                Console.WriteLine("[AreaMiner] Can't start the plugin from the 'Accounts' tab.");
                return new PluginResponse(false, "Can't start the plugin from the 'Accounts' tab.");
            }

            //Check if bot settings are valid.
            if (!player.settings.loadWorld) {
                Console.WriteLine("[AreaMiner] 'Load world' must be enabled.");
                return new PluginResponse(false, "'Load world' must be enabled.");
            }
            //Check if settings are valid.
            if (string.IsNullOrWhiteSpace(Setting[0].Get<string>()) ||
                string.IsNullOrWhiteSpace(Setting[1].Get<string>())) {
                Console.WriteLine("[AreaMiner] No coordinates have been entered.");
                return new PluginResponse(false, "No coordinates have been entered.");
            }
            if (!Setting[0].Get<string>().Contains(' ') || !Setting[1].Get<string>().Contains(' ')) {
                Console.WriteLine("[AreaMiner] Invalid coordinates (does not contain ' ').");
                return new PluginResponse(false, "Invalid coordinates (does not contain ' ').");
            }
            var startSplit = Setting[0].Get<string>().Split(' ');
            var endSplit = Setting[1].Get<string>().Split(' ');
            if (startSplit.Length != 3 || endSplit.Length != 3) {
                Console.WriteLine("[AreaMiner] Invalid coordinates (must be x y z).");
                return new PluginResponse(false, "Invalid coordinates (must be x y z).");
            }

            //Set the player.
            this.player = player;

            //If there are no zone assinged,
            //generate one.
            Shares.Add(player);

            // Split the ids.
            var ids = Setting[4].Get<string>().Split(' ');
            List<int> ignoreIdList = new List<int>();
            for (int i = 0; i < ids.Length; i++) {
                int id;
                if (!int.TryParse(ids[i], out id))
                    continue;
                ignoreIdList.Add(id);
            }
            this.ignoreIds = ignoreIdList.ToArray();

            //Hook start events.
            player.physicsEngine.onPhysicsPreTick += PhysicsEngine_onPhysicsPreTick;
            player.events.onDisconnected += Events_onDisconnected;
            return new PluginResponse(true);
        }

        private void PhysicsEngine_onPhysicsPreTick(IPlayer player) {
            
            //Check if we should stop the plugin.
            if (stopToken.stopped) {
                player.physicsEngine.onPhysicsPreTick -= PhysicsEngine_onPhysicsPreTick;
                if (target != null) {
                    object val;
                    beingMined.TryRemove(target, out val);
                }
                return;
            }

            //Check if we are not busy at the moment.
            if (player.status.entity.isDead || player.status.eating
                || moving || mining || IsMacroRunning())
                return;


            //Check if we are full.
            if (Setting[2].value != null && !string.IsNullOrWhiteSpace(Setting[2].Get<string>()) &&
                player.status.containers.inventory.hotbar.FindFreeSlot() == -1 &&
                player.status.containers.inventory.inner.FindFreeSlot() == -1)
            {
                //Inventory is full, do full events.
                macroTask = player.functions.StartMacro(Setting[2].Get<string>());
                return;
            }

            //Handle tick skipping:
            //Check if this tick should be skipped.
            if (skipTick) {
                skipTick = false;
                return;
            }
            ticks++;
            if (ticks < (Setting[3].Get<int>() == 0?5:1)) return;
            ticks = 0;

            //Check if we should mine something.
            if (this.target != null) {

                //We have reached the position we wanted,
                //but the target is still valid, meaning
                //we should still mine it.
                if (!equiped) {
                    //This tick we are equiping a tool,
                    //so on the next tick start mining.
                    EquipBest();
                    this.equiped = true;

                    //We should use the same tick to set up
                    //the look as well, as it will be processed
                    //at the same time on the server.
                    player.functions.LookAtBlock(this.target);

                    this.skipTick = true;
                    return;
                }

                //Update the mining state.
                //Attempt to mine the target.
                MineTarget();
                return;
            }

            //Get the next target.
            this.target = this.FindNext();
            if (this.target == null)   return;

            //Attempt to move to the target.
            //Update moving state.
            this.moving = true;

            ThreadPool.QueueUserWorkItem(state => 
            {
                //Create the map and hook all the
                //callbacks.
                var success = player.functions.WaitMoveToRange(target, stopToken, PathOptions);
                if (!success)
                    this.target = null;

                this.moving = false;
            });
        }

        private void Events_onDisconnected(IPlayer player, string reason) {

            //Remove the mine restriction.
            if (target != null) {
                object obj;
                beingMined.TryRemove(target, out obj);
            }
        }

        /// <summary>
        /// Attempts to mine a block at
        /// the target location.
        /// </summary>
        private void MineTarget() {

            //Update the mining state.
            this.mining = true;

            //Attempt to mine the target.
            var result = player.functions.BlockDig(this.target, MiningResult);

            //Check for insta cancelled.
            if (result.cancelled || !result.valid) {

                //Insta completed:
                //Reset states.
                this.mining = false;
                this.equiped = false;
                this.target = null;
            }
        }
        /// <summary>
        /// Called once block digging process
        /// is done.
        /// </summary>
        /// <param name="digAction"></param>
        private void MiningResult(IDigAction digAction) {
            
            //Check if we completed mining the block.
            if (digAction.cancelled || digAction.completed || !digAction.valid) {

                //Reset states.
                this.mining = false;
                this.equiped = false;
                this.target = null;
            }
        }


        /// <summary>
        /// Equips best tool that we have in
        /// the inventory for the target block.
        /// </summary>
        private void EquipBest() {

            //Check if the target is valid.
            if (target == null) return;

            //Check if we have a tool for the specific block.
            var blockType = BlocksGlobal.blockHolder.GetBlock(player.world.GetBlockId(target.x, (int)target.y, target.z));
            if (blockType != null && blockType.material != null && blockType.material.requiredTool != MaterialTools.ANY) {
                //Get items that would be acceptable.
                var acceptable = ItemsGlobal.itemHolder.GetItems(blockType.material.requiredTool);
                if (acceptable != null) {

                    //Extract ids.
                    var ids = acceptable.Select(x => (int)x.id).ToArray();

                    //We should eat, check if we have tools.
                    if (player.status.containers.inventory.hotbar.FindId(ids) == -1) { //We should find it in inventory.
                                                                                        //Find the slot.
                        var slot = player.status.containers.inventory.inner.FindId(ids);
                        if (slot != -1) {
                            player.status.containers.inventory.hotbar.BringToHotbar(6, slot, null); //Bring to held slot.
                        }
                    }

                    //Check if we have any tools in the hotbar.
                    var toolSlot = player.status.containers.inventory.hotbar.FindId(ids);
                    //Check if we have the tool in hotbar.
                    if (toolSlot != -1) {
                        //Select the item in hotbar.
                        player.functions.SetHotbarSlot((short)(toolSlot - 36));
                    }
                }
            }
        }

        /// <summary>
        /// Find the next block we should mine.
        /// </summary>
        /// <returns></returns>
        private ILocation FindNext() {

            //Get the area from the radius.
            IRadius playerRadius = Shares.Get(player);
            if (playerRadius == null) return null;

            //Search from top to bottom.
            //(As that is easier to manager)
            ILocation closest = null;
            double distance = int.MaxValue;
            for (int y = (int)playerRadius.start.y + playerRadius.height; y >= (int)playerRadius.start.y - playerRadius.height; y--)
                if(closest == null)
                    for (int x = playerRadius.start.x; x < playerRadius.start.x + playerRadius.xSize; x++)
                        for (int z = playerRadius.start.z; z < playerRadius.start.z + playerRadius.zSize; z++) {

                            var tempLocation = new Location(x, y, z);
                            //Check if the block is valid for mining.
                            if (beingMined.ContainsKey(tempLocation) || player.world.GetBlockId(x, y, z) == 0)
                                continue;
                            if (broken.ContainsKey(tempLocation) &&
                                broken[tempLocation].Subtract(DateTime.Now).TotalSeconds < -15)
                                continue;
                            if(ignoreIds?.Contains(player.world.GetBlockId(x, y, z)) == true) continue;

                            if (closest == null) {
                                distance = tempLocation.Distance(player.status.entity.location.ToLocation(0));
                                closest = new Location(x, y, z);
                            }
                            else if (tempLocation.Distance(player.status.entity.location.ToLocation(0)) < distance) {
                                distance = tempLocation.Distance(player.status.entity.location.ToLocation(0));
                                closest = tempLocation;
                            }
                        }

            return closest;
        }

        private bool IsMacroRunning() {
            //Check if there is an instance of the task.
            if (macroTask == null) return false;
            //Check completion state.
            return !macroTask.IsCompleted && !macroTask.IsCanceled && !macroTask.IsFaulted;
        }

        // These classes allow the bots
        // to share work.
        // (E.g.: mining different blocks.)
        #region Shared work.

        /// <summary>
        /// Blocks that are taken already.
        /// </summary>
        public static ConcurrentDictionary<ILocation, object> beingMined = new ConcurrentDictionary<ILocation, object>();

        /// <summary>
        /// Blocks that are having a hard time being mined.
        /// </summary>
        public static ConcurrentDictionary<ILocation, DateTime> broken = new ConcurrentDictionary<ILocation, DateTime>(); 

        /// <summary>
        /// 
        /// </summary>
        public static ShareManager Shares;

        #endregion
    }
}


public class ShareManager
{
    private ConcurrentDictionary<IPlayer, IRadius> _Zones = new ConcurrentDictionary<IPlayer, IRadius>();
    private readonly IRadius _Total;

    private bool _Processing;
    private bool _Reprocess;

    public ShareManager(IRadius total) {

        // Apply the total area that needs
        // to be mined.
        this._Total = total;
    }

    public void Add(IPlayer player) {

        // Add a new player to the zone array.
        _Zones.TryAdd(player, new IRadius(_Total.start, new Location(_Total.start.x + _Total.xSize, _Total.start.y + _Total.height, _Total.start.z + _Total.zSize)));

        // Update all shares, as we
        // got another person in.
        Update();
    }

    public IRadius Get(IPlayer player) {
        return _Zones[player];
    }

    private void Update() {

        // Avoid processing 2 calls at the same time.
        if (_Processing) {
            this._Reprocess = true;
            return;
        }
        this._Processing = true; // We are not processing the call.
        bool first = true; // If this is our first time in this call
                           // process the call, with _reprocess ignored.

        // Check if while we were processing another
        // call came in.
        while (first || this._Reprocess) {
            first = false;
            this._Reprocess = false; // We are reprocessing so update the state.

            // Get a zones instance.
            var zones = _Zones.ToArray();
            Calculate(zones);
        }
        this._Processing = false; // Done processing the call.
    }

    private void Calculate(KeyValuePair<IPlayer, IRadius>[] zones) {

        // Count the workers.
        var count = zones.Length;

        // Create new zones for choosing here.
        IRadius[] zoneList = new IRadius[zones.Length];

        int x, z;
        int l;
        if (_Total.xSize > _Total.zSize) {
            x = _Total.xSize/count;
            l = _Total.xSize;
            z = _Total.zSize;
            for (int i = 0; i < zones.Length; i++)
                zoneList[i] = new IRadius(new Location(_Total.start.x + x*i, _Total.start.y, _Total.start.z),
                    new Location(_Total.start.x + (x*(i + 1)) + (i == zones.Length - 1 ? l - _Total.xSize / count : 0), _Total.start.y - _Total.height, _Total.start.z + z));
        }
        else {
            x = _Total.xSize;
            z = _Total.zSize/count;
            l = _Total.zSize;
            for (int i = 0; i < zones.Length; i++)
                zoneList[i] = new IRadius(new Location(_Total.start.x, _Total.start.y, _Total.start.z + z*i),
                    new Location(_Total.start.x + x, _Total.start.y + _Total.height, _Total.start.z + z*(i + 1) + (i==zones.Length-1? l- _Total.zSize / count:0)));
        }

        for (int i = 0; i < zoneList.Length; i++)
            zones[i].Value.UpdateHorizontal(zoneList[i].start,
                new Location(zoneList[i].start.x + zoneList[i].xSize + 1, zoneList[i].start.y + zoneList[i].height,
                    zoneList[i].start.z + 1 +zoneList[i].zSize));
    }
}