using System;
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
using OQ.MineBot.PluginBase.Classes.Entity.Filter;
using OQ.MineBot.PluginBase.Classes.Entity.Lists;
using OQ.MineBot.PluginBase.Classes.Items;
using OQ.MineBot.PluginBase.Classes.Window.Containers.Subcontainers;
using OQ.MineBot.PluginBase.Movement.Maps;
using OQ.MineBot.Protocols.Classes.Base;
using TunnelPlugin.Pattern;

namespace TunnelPlugin
{
    public class PluginCore : IStartPlugin
    {
        public MapOptions PATH = new MapOptions() { Look = false, Quality = SearchQuality.MEDIUM, Mine = true, Strict = true, NoCost = true };
        public MapOptions PATH_ORE = new MapOptions() { Look = false, Quality = SearchQuality.MEDIUM, Mine = true };
        public MapOptions PATH_HOME = new MapOptions() { Look = false, Quality = SearchQuality.HIGHEST, Mine = true };

        public SharedData SHARED = new SharedData();

        public static IPattern PATTERN_SELECTED = new GapTwo();
        public static IPattern[] PATTERNS =
        {
            new GapOne(),
            new GapTwo(),
            new GapThree(),
        };


        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return "Tunneler";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription()
        {
            return "\"Digs tunnles and mine ores\" - Temm, 2017";
        }

        /// <summary>
        /// Author of the plugin.
        /// </summary>
        /// <returns></returns>
        public PluginAuthor GetAuthor()
        {
            return new PluginAuthor("OQ & ZerGo0");
        }

        /// <summary>
        /// Version of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetVersion()
        {
            return "1.02.00";
        }

        /// <summary>
        /// All settings should be stored here.
        /// (NULL if there shouldn't be any settings)
        /// </summary>
        public IPluginSetting[] Setting { get; set; } =
        {
            new NumberSetting("Height", "Height level that the bots should mine at", 12, 1, 256),
            new ComboSetting("Pattern", "", new []{PATTERNS[0].GetName(), PATTERNS[1].GetName(), PATTERNS[2].GetName()}, 1),
            new StringSetting("Macro on inventory full", "Starts the macro when the bots inventory is full.", ""),
            new BoolSetting("Diamond ore", "", true),
            new BoolSetting("Emerald ore", "", true),
            new BoolSetting("Iron ore", "", true),
            new BoolSetting("Gold ore", "", true),
            new BoolSetting("Redstone ore", "", false),
            new BoolSetting("Lapis Lazuli ore", "", false),
            new BoolSetting("Coal ore", "", false),
            new StringSetting("Starting Macro", "This macro will start before the plugin starts tunneling.", ""),
            new BoolSetting("Self-Defense", "Attack mobs if the bot gets attacked.", false),
            new StringSetting("Safety Macro", "If the bot gets below 4 hearts (8 health) it will start this macro.", ""),
            new BoolSetting("Respawn if dead", "Will respawn and start the starting macro.", false),
            new StringSetting("No Pickaxe Macro", "If the bot has no pickaxe in his INV then it will run this macro", ""),
            new ComboSetting("Pickaxe Type", "Which Pickaxe does the bot use? (Used for 'No Pickaxe Macro')", new string[] { "Wood", "Stone", "Iron", "Gold", "Diamond"}, 0),
            //new StringSetting("Error Macro", "If bot is stuck", ""), // for debugging purposes
        };

        /// <summary>
        /// How many ticks have passed since
        /// we attempted to attack the target.
        /// </summary>
        private int hitTicks;

        /// <summary>
        /// Called once the plugin is loaded.
        /// (Params are the version of the program)
        /// (This is not reliable as if "Load plugins" 
        /// isn't enabled this will not be called)
        /// </summary>
        /// <param name="version"></param>
        /// <param name="subversion"></param>
        /// <param name="buildversion"></param>
        public void OnLoad(int version, int subversion, int buildversion) { }

        /// <summary>
        /// Called once the plugin is enabled.
        /// Meaning the start methods will be
        /// called when needed.
        /// </summary>
        public void OnEnabled()
        {
            PATTERN_SELECTED = PATTERNS[Setting[1].Get<int>()];
            stopToken.Reset();
        }

        /// <summary>
        /// Called once the plugin is disabled.
        /// Meaning the start methods will not be
        /// called.
        /// </summary>
        public void OnDisabled() { SHARED.Reset(); }

        /// <summary>
        /// The plugin should be stopped.
        /// </summary>
        public void Stop() { stopToken.Stop(); }

        /// <summary>
        /// Return an instance of this plugin.
        /// </summary>
        /// <returns></returns>
        public IPlugin Copy() { return (IStartPlugin)MemberwiseClone(); }

        private OreBulk ores;

        private ILocation directionOffset;
        private bool reachedHome;
        private bool moving;
        private int mult;
        private CancelToken stopToken = new CancelToken();

        // ZerGo0 Code
        private bool StartMacroDone;
        private bool StartMacroHome;
        private bool GotAttacked;
        private bool SafetyMacro;
        private bool SafetyMacroDone;
        private bool FullInvMacroDone;
        private bool PickAxeDone;
        private bool DebugMode;

        private Task macroTask;


        private IAsyncMap currentMap;

        private int step = 0;
        private int fails = 0;
        private int PickAxeType;
        private ILocation CurrentLocation;
        private ILocation LastLocation;
        private int ErrorCount;
        private ILocation newLocation;
        private int ErrorErrorCount;

        /// <summary>
        /// Called once a "player" logs
        /// in to the server.
        /// (This does not start a new thread, so
        /// if you want to do any long temn functions
        /// please start your own thread!)
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PluginResponse OnStart(IPlayer player)
        {

            //Check if bot settings are valid.
            if (!player.settings.loadWorld)
            {
                Console.WriteLine("[Tunneler] 'Load world' must be enabled.");
                return new PluginResponse(false, "'Load world' must be enabled.");
            }
            if (player.settings.staticWorlds)
            { // Warning
                Console.WriteLine("[Tunneler] 'Shared worlds' should be disabled.");
            }

            //Self Defense
            if (Setting[11].Get<bool>() && !player.settings.loadWorld)
            {
                Console.WriteLine("[Tunneler] 'Load world' must be enabled.");
                return new PluginResponse(false, "'Load world' must be enabled.");
            }
            //Check if bot settings are valid.
            if (Setting[11].Get<bool>() && !player.settings.loadMobs)
            {
                Console.WriteLine("[Tunneler] 'Load Mobs' must be enabled.");
                return new PluginResponse(false, "'Load Mobs' must be enabled.");
            }

            // Register new stop token.
            stopToken = new CancelToken();


            //Prevent the bot from getting wrong start position
            if (Setting[10].value == null || string.IsNullOrWhiteSpace(Setting[10].Get<string>()))
            {
                if (DebugMode)
                {
                    Console.WriteLine("Starting Macro disabled, getting home position now!");
                }
                // Retrieve information from shared data.
                PickDirection();
                PickHome(player);
            }

            // ZerGo0 Code
            StartMacroDone = false;
            FullInvMacroDone = false;
            PickAxeDone = false;
            SafetyMacroDone = false;
            StartMacroHome = false;
            SafetyMacro = false;

            //Debug Mode
            DebugMode = false;

            //Get Pickaxe ID
            if (Setting[15].Get<int>() == 0)
            {
                PickAxeType = 270;
            }
            else if (Setting[15].Get<int>() == 1)
            {
                PickAxeType = 274;
            }
            else if (Setting[15].Get<int>() == 2)
            {
                PickAxeType = 257;
            }
            else if (Setting[15].Get<int>() == 3)
            {
                PickAxeType = 285;
            }
            else if (Setting[15].Get<int>() == 4)
            {
                PickAxeType = 278;
            }

            if (DebugMode)
            {
                Console.WriteLine("Pickaxe Type: " + PickAxeType);
            }

            // Register events.
            player.physicsEngine.onPhysicsPreTick += Tick;
            player.events.onHealthUpdate += OnHealthUpdate;

            // Auto EQ at start
            if (Setting[11].Get<bool>())
            {

                player.functions.OpenInventory();
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
            }

            return new PluginResponse(true);
        }

        public void OnHealthUpdate(IPlayer player, float health, int food, float foodSaturation)
        {
            //Don't attack anything if we are running a macro
            if (!IsMacroRunning())
            {
                //Breaks Chest opening for some reason if it is used with InvUpdate
                if (Setting[11].Get<bool>())
                {
                    if (DebugMode)
                    {
                        Console.WriteLine("Auto Armor.");
                    }
                    player.functions.OpenInventory();
                    if (player.functions.EquipBest(EquipmentSlots.Head,
                        ItemsGlobal.itemHolder.helmets))
                        Thread.Sleep(250);
                    if (player.functions.EquipBest(EquipmentSlots.Chest,
                        ItemsGlobal.itemHolder.chestplates))
                        Thread.Sleep(250);
                    if (player.functions.EquipBest(EquipmentSlots.Pants,
                        ItemsGlobal.itemHolder.leggings))
                        Thread.Sleep(250);
                    if (player.functions.EquipBest(EquipmentSlots.Boots,
                        ItemsGlobal.itemHolder.boots))
                        Thread.Sleep(250);
                    player.functions.CloseInventory();
                }

                //Perform respawn and start the start macro again, could change it to perform a diffrent macro if users want to 
                //Might be broken on custom servers?
                if (Setting[13].Get<bool>() && player.status.entity.isDead == true)
                {
                    if (DebugMode)
                    {
                        Console.WriteLine("Perform Respawn");
                    }
                    if (currentMap != null)
                    {
                        currentMap.Dispose();
                    }
                    Thread.Sleep(2500);
                    player.functions.PerformaRespawn();
                    StartMacroDone = false;
                    StartMacroHome = false;
                    return;
                }

                //Safety Macro
                if (Setting[12].value != null && !string.IsNullOrWhiteSpace(Setting[12].Get<string>()) && player.status.entity.health < 8 && !player.status.entity.isDead && SafetyMacro == false && !IsMacroRunning())
                {
                    if (DebugMode)
                    {
                        Console.WriteLine("Safety Macro.");
                    }
                    if (currentMap != null)
                    {
                        currentMap.Dispose();
                    }
                    macroTask = player.functions.StartMacro(Setting[12].Get<string>());
                    StartMacroHome = false;
                    SafetyMacro = true;
                    SafetyMacroDone = true;
                    return;
                }

                //Check if we get attacked.
                //Get mob location?
                var moblocation = player.status.entity.location.ToLocation(0);
                //Find target
                var currentTarget = player.entities.FindClosestMob(moblocation.x, moblocation.y, moblocation.z);

                //Register if we got hit by a mob or  not
                if (Setting[11].Get<bool>() && currentTarget.location.Distance(player.status.entity.location) <= 3 && player.status.entity.health < 20)
                {
                    if (DebugMode)
                    {
                        Console.WriteLine("We got hit by a mob");
                    }
                    var pos = SHARED.Home.Offset(directionOffset.Multiply(mult));
                    if (currentMap != null)
                    {
                        currentMap.Dispose();
                    }
                    currentMap = player.functions.AsyncMoveToLocation(pos, stopToken, fails < 8 ? PATH : PATH_HOME);
                    GotAttacked = true;
                }
            }
        }

        private void Tick(IPlayer player)
        {
            //Check if we should stop the plugin.
            if (stopToken.stopped)
            {
                player.physicsEngine.onPhysicsPreTick -= Tick;
                player.events.onHealthUpdate -= OnHealthUpdate;
                return;
            }

            CurrentLocation = player.status.entity.location.ToLocation();

            //preparation for status messages to send current location
            if (CurrentLocation.Compare(LastLocation) == false)
            {
                if (DebugMode)
                {
                    Console.WriteLine(CurrentLocation);
                }
                ErrorCount = 0;
                LastLocation = player.status.entity.location.ToLocation();
                return;
            }

            //Errofix for torchbug
            if (DebugMode && !IsMacroRunning() && CurrentLocation.Compare(LastLocation) == true)
            {
                ErrorCount++;
                if (ErrorCount > 750)
                {
                    if (DebugMode)
                    {
                        Console.WriteLine("Error at: " + CurrentLocation);
                    }
                    ErrorErrorCount++;
                    if (ErrorErrorCount > 2)
                    {
                        if (DebugMode)
                        {
                            Console.WriteLine("Error 3 times at: " + CurrentLocation);
                        }
                        macroTask = player.functions.StartMacro(Setting[10].Get<string>());
                        StartMacroHome = false;
                        ErrorErrorCount = 0;
                        return;
                    }
                    else
                    {
                        macroTask = player.functions.StartMacro(Setting[16].Get<string>());
                        ErrorCount = 0;
                        return;
                    }
                }
            }

            //Attack mob
            if (Setting[11].Get<bool>())
            {
                //Check if we get attacked.
                //Get mob location?
                var moblocation = player.status.entity.location.ToLocation(0);
                //Find target
                var currentTarget = player.entities.FindClosestMob(moblocation.x, moblocation.y, moblocation.z);

                if (currentTarget != null)
                {
                    if (currentTarget.location.Distance(player.status.entity.location) <= 3 && GotAttacked == true)
                    {
                        if (DebugMode)
                        {
                            Console.WriteLine("Currently attacking a mob.");
                        }
                        var entity = Attack(player);
                        return;
                    }
                    else
                    {
                        //Bot got damage from something else
                        GotAttacked = false;
                    }
                }
                else
                {
                    GotAttacked = false;
                }
            }

            //Reset Start postion since it breaks the bot if path is not avaible
            if (StartMacroDone == true && !IsMacroRunning() && StartMacroHome == false ||
                SafetyMacroDone == true && !IsMacroRunning() && StartMacroHome == false ||
                FullInvMacroDone == true && !IsMacroRunning() && StartMacroHome == false ||
                PickAxeDone == true && !IsMacroRunning() && StartMacroHome == false)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Set home for tunneler.");
                }
                mult = 0;
                var currentloc = player.status.entity.location.ToLocation();
                newLocation = new Location(currentloc.x, Setting[0].Get<int>() - 1, currentloc.z);
                SHARED.Home = newLocation;
                PickDirection();
                PickHome(player);
                StartMacroHome = true;
                return;
            }

            //Starting macro (for starting position for example)
            if (Setting[10].value != null && !string.IsNullOrWhiteSpace(Setting[10].Get<string>()) && StartMacroDone == false)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Starting Macro");
                }
                macroTask = player.functions.StartMacro(Setting[10].Get<string>());
                StartMacroDone = true;
                StartMacroHome = false;
                return;
            }

            //Check if we have a pickaxe
            if (Setting[14].value != null && !string.IsNullOrWhiteSpace(Setting[14].Get<string>()) && !IsMacroRunning() && player.status.containers.inventory.FindIds(PickAxeType).Length == 0)
            {
                if (DebugMode)
                {
                    Console.WriteLine("No Pickaxe Macro");
                }
                if (currentMap != null)
                {
                    currentMap.Dispose();
                }
                macroTask = player.functions.StartMacro(Setting[14].Get<string>());
                PickAxeDone = true;
                StartMacroHome = false;
                return;
            }

            //Check if we are not busy at the moment.
            if (player.status.entity.isDead || player.status.eating
                || moving || IsMacroRunning() || currentMap?.IsValidMoving() == true || GotAttacked == true)
                return;

            //Check if we are full.
            if (Setting[2].value != null && !string.IsNullOrWhiteSpace(Setting[2].Get<string>()) &&
                player.status.containers.inventory.hotbar.FindFreeSlot() == -1 &&
                player.status.containers.inventory.inner.FindFreeSlot() == -1)
            {
                if (DebugMode)
                {
                    Console.WriteLine("Inv full.");
                }
                //Inventory is full, do full events.
                macroTask = player.functions.StartMacro(Setting[2].Get<string>());
                FullInvMacroDone = true;
                StartMacroHome = false;
                return;
            }

            // Check if we need to move home.
            if (MoveHome(player))
                return;

            // If we can't branch, then we should dig further.
            if (MineOres(player))
                return;
            if (!DigBranch(player))
            {
                DigTunnel(player);
            }

            //Reset Values if we successfully ended a Tick
            SafetyMacro = false;
            GotAttacked = false;
            SafetyMacroDone = false;
            FullInvMacroDone = false;
            PickAxeDone = false;
        }

        private IEntity Attack(IPlayer player)
        {

            //Get mob location?
            var current = player.status.entity.location.ToLocation(0);
            //Find target
            var closestMob = player.entities.FindClosestMob(current.x, current.y, current.z);

            //Attack mob if found.
            if (closestMob != null)
            {
                //Equip weapon
                var slot = player.status.containers.inventory.hotbar.GetSlot((byte)player.status.entity.selectedSlot).id;
                if (slot != 268 &&
                    slot != 272 &&
                    slot != 267 &&
                    slot != 283 &&
                    slot != 276)
                {
                    player.functions.EquipWeapon();
                }

                //Look at the target.
                player.functions.LookAt(closestMob.location, true);

                //Check if we should attack.
                hitTicks++;

                // 1 hit tick is about 50 ms.
                //Might need to add new combat system delay, but too lazy
                int ms = hitTicks * 50;

                //11 CPS, should be okay I think
                if (ms >= (1000 / 8))
                {

                    //Hitting, reset tick counter.
                    hitTicks = 0;

                    player.functions.EntityAttack(closestMob.entityId);

                }
            }
            return closestMob;
        }

        private void DigTunnel(IPlayer player)
        {

            // Calculate the next position.
            var pos = SHARED.Home.Offset(directionOffset.Multiply(mult));
            if (!IsTunnelable(player, pos))
            {
                mult += PATTERN_SELECTED.gap;
                return;
            }
            moving = true;

            currentMap = player.functions.AsyncMoveToLocation(pos, stopToken, fails < 8 ? PATH : PATH_HOME);
            currentMap.Completed += areaMap =>
            {
                fails = 0;
                mult += PATTERN_SELECTED.gap;
                moving = false;
            };
            currentMap.Cancelled += (areaMap, cuboid) =>
            {
                fails++;
                moving = false;
            };

            if (!currentMap.Start() || (currentMap.Searched && currentMap.Complete && currentMap.Valid))
                moving = false;
        }

        private bool DigBranch(IPlayer player)
        {
            if (mult == 0) return false;

            // Calculate current position
            // where we should branch from.
            var pos = SHARED.Home.Offset(directionOffset.Multiply(mult - PATTERN_SELECTED.gap));
            if (!IsTunnelable(player, pos))
            {
                return false;
            }

            if (step == 1 || step == 3)
            { // Reset.
                moving = true;
                currentMap = player.functions.AsyncMoveToLocation(pos, stopToken, PATH_HOME);
                currentMap.Completed += areaMap =>
                {
                    if (step == 3)
                    {
                        step = 0;
                    }
                    else step++;
                    moving = false;
                };
                currentMap.Cancelled += (areaMap, cuboid) =>
                {
                    moving = false;
                };
                if (!currentMap.Start() || (currentMap.Searched && currentMap.Complete && currentMap.Valid))
                    moving = false;

                return true;
            }

            // Swap x and z to get offset
            // to walls instead of tunenl.
            ILocation wallOffset = new Location(directionOffset.z, directionOffset.y, directionOffset.x);
            if (step == 2)
            { // If we are on step 1 then we should mine the other side. 
                wallOffset = wallOffset.Multiply(-1);
            }

            // Calculate the position we should check for a valid branch.
            var start = pos.Offset(wallOffset);
            if (!BlocksGlobal.blockHolder.IsSolid(player.world.GetBlockId(start.Offset(1))) ||
                !BlocksGlobal.blockHolder.IsSolid(player.world.GetBlockId(start.Offset(2))))
                return false;

            // Calculate the position we should mine to.
            var temp = wallOffset.Multiply(PATTERN_SELECTED.lenght);
            var end = pos.Offset(temp);
            if (!IsTunnelable(player, end))
            {
                //step++;
                step = 0;
                return false;
            }
            moving = true;

            currentMap = player.functions.AsyncMoveToLocation(end, stopToken, PATH);
            currentMap.Completed += areaMap =>
            {
                step++;

                // Collect each location that we just mined.
                ILocation[] locations = new ILocation[PATTERN_SELECTED.lenght * 2];
                for (int i = 0; i < PATTERN_SELECTED.lenght; i++)
                {
                    locations[i * 2] = pos.Offset(wallOffset.Multiply(i + 1).Offset(1));
                    locations[i * 2 + 1] = pos.Offset(wallOffset.Multiply(i + 1).Offset(2));
                }

                // Scan the branch that we just mined.
                var ores = new OreBulk();
                ScanNeighbourChunk(player, locations, ores);
                if (!ores.IsEmpty()) this.ores = ores;
                moving = false;
            };
            currentMap.Cancelled += (areaMap, cuboid) =>
            {
                step++;
                moving = false;
            };
            if (!currentMap.Start() || (currentMap.Searched && currentMap.Complete && currentMap.Valid))
                moving = false;
            return true;
        }

        private bool MineOres(IPlayer player)
        {

            // Check if we have an ore set, if we don't
            // then we still haven't dug out a valid branch.
            if (ores == null || GotAttacked == true) return false;

            // Check if the ore list is empty, if it's
            // not empty take out locations and use them
            // until we run out.
            var loc = ores.Take();
            if (loc == null)
            {
                ores = null;
                return false;
            }
            // Check if the ore is still the same block
            // (if not we have already mined it)
            if (player.world.GetBlockId(loc.location) != loc.id)
            {
                return true; // Ignore this ore.
            }

            // By moving to the ores location we 
            // guarantee to mine it and pick it up.
            moving = true;

            // Offset Y by -1 so that the bot would stand on the location
            // instead of stand above it.
            var map = player.functions.AsyncMoveToLocation(loc.location.Offset(-1), stopToken, PATH_ORE);
            map.Completed += areaMap => {
                ScanNeighbourOres(player, loc.location, ores); // Scan the block that we are standing on.
                ScanNeighbourOres(player, loc.location.Offset(2), ores); // Scan the block that our head is in.
                moving = false;
            };
            map.Cancelled += (areaMap, cuboid) => {
                moving = false; // Remove the current ore and
                                // ignore it (by not readding it)
            };
            //Check if the path is instantly completed.
            if (!map.Start() || (map.Searched && map.Complete && map.Valid))
                moving = false;
            return true;
        }

        /// Attempts to move to the shared home location
        /// if the bot has not reached it before.
        /// <returns>True if we are moving to the location.</returns>
        private bool MoveHome(IPlayer player)
        {
            if (GotAttacked) return false;
            if (reachedHome) return false;
            if (moving) return true;

            currentMap = player.functions.AsyncMoveToLocation(SHARED.Home, stopToken, PATH_HOME);
            currentMap.Completed += areaMap => {
                reachedHome = true;
                moving = false;
            };
            currentMap.Cancelled += (areaMap, cuboid) => {
                reachedHome = false;
                moving = false;
            };

            //Start moving.
            this.moving = true;
            currentMap.Start();
            return true;
        }

        private void PickDirection()
        {
            directionOffset = SHARED.DirectionToOffset(SHARED.PickDirection());
        }

        private void PickHome(IPlayer player)
        {
            var loc = player.status.entity.location.ToLocation();
            SHARED.SetHome(new Location(loc.x, Setting[0].Get<int>() - 1, loc.z));
        }

        private bool IsTunnelable(IPlayer player, ILocation pos)
        {

            return !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(1))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(2))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(3))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(1, 1, 0))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(1, 2, 0))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(0, 1, 1))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(0, 2, 1))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(-1, 1, 0))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(-1, 2, 0))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(0, 1, -1))) &&
                   !BlocksGlobal.blockHolder.IsDanger(player.world.GetBlockId(pos.Offset(0, 2, -1)))
                   ;
        }
        private void ScanNeighbourOres(IPlayer player, ILocation location, OreBulk ores)
        {

            // Scan neighbours in an 3x3 area.
            for (int x = -1; x < 2; x++)
                for (int y = -1; y < 2; y++)
                    for (int z = -1; z < 2; z++)
                    {
                        var loc = location.Offset(x, y, z);
                        var id = player.world.GetBlockId(loc);
                        if (IsOre(player, id, loc)) ores.Add(new OreLocation(loc, id));
                    }
        }

        private void ScanNeighbourChunk(IPlayer player, ILocation[] location, OreBulk ores)
        {
            for (int i = 0; i < location.Length; i++)
                ScanNeighbourOres(player, location[i], ores);
        }

        // Also checks if the block is minable.
        private bool IsOre(IPlayer player, ushort id, ILocation location)
        {
            if (!BlocksGlobal.blockHolder.IsSafeToMine(player.world, location, true)) return false;

            if ((Setting[3].Get<bool>() && id == 56) ||
                (Setting[4].Get<bool>() && id == 129) ||
                (Setting[5].Get<bool>() && id == 15) ||
                (Setting[6].Get<bool>() && id == 14) ||
                (Setting[7].Get<bool>() && (id == 73 || id == 74)) || // Active and inactive redstone.
                (Setting[8].Get<bool>() && id == 21) ||
                (Setting[9].Get<bool>() && id == 16))
            {
                return true;
            }
            return false;
        }

        private bool IsMacroRunning()
        {
            //Check if there is an instance of the task.
            if (macroTask == null) return false;
            //Check completion state.
            return !macroTask.IsCompleted && !macroTask.IsCanceled && !macroTask.IsFaulted;
        }
    }

    public class SharedData
    {
        // Position of where all bots
        // will move before starting to tunnel.
        public ILocation Home { get; set; }
        // Keeps track of what direction
        // the next bot should mine.
        private int m_direction;

        // Index - direction
        private ILocation[] OFFSET_TABLE = new[]
        {
            new Location(1, 0, 0),
            new Location(0, 0, 1),
            new Location(-1, 0, 0),
            new Location(0, 0, -1),
        };

        // Attempts to set the Home variable.
        public bool SetHome(ILocation location)
        {
            if (Home != null) return false;
            Home = location;
            return true;
        }

        // Returns a direction and increments it.
        public int PickDirection()
        {
            var dir = m_direction++;
            if (m_direction >= 4) m_direction = 0; // 0-3 directions only, if it's more then reset.
            return dir;
        }

        // Converts a direction input
        // directional offset.
        public ILocation DirectionToOffset(int dir)
        {
            return OFFSET_TABLE[dir];
        }

        // Resets all values to default.
        public void Reset()
        {
            m_direction = 0;
            Home = null;
        }
    }

    public class OreBulk
    {
        /// <summary>
        /// Locations of ores that are 
        /// connected to one another.
        /// </summary>
        private readonly List<OreLocation> locations = new List<OreLocation>();

        public bool IsEmpty()
        {
            return locations.Count == 0;
        }
        public void Add(OreLocation location)
        {
            // Check if this location still doesn't exist.
            for (int i = 0; i < locations.Count; i++)
                if (locations[i].location.Compare(location.location))
                    return; // Same location already exists.
            locations.Add(location);
        }
        public OreLocation Take()
        {
            if (locations.Count == 0) return null;

            var loc = locations[0];
            locations.RemoveAt(0);

            return loc;
        }
    }

    public class OreLocation
    {
        public ushort id;
        public ILocation location;

        public OreLocation(ILocation location, ushort id)
        {
            this.location = location;
            this.id = id;
        }
    }
}