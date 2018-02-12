using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Base;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Classes.Entity;
using OQ.MineBot.PluginBase.Classes.Entity.Lists;
using OQ.MineBot.PluginBase.Classes.Entity.Player;
using OQ.MineBot.PluginBase.Classes.Items;
using OQ.MineBot.PluginBase.Classes.Window.Containers.Subcontainers;
using OQ.MineBot.PluginBase.Pathfinding;
using OQ.MineBot.Protocols.Classes.Base;

namespace ShieldPlugin
{
    public class PluginCore : IStartPlugin
    {
        /// <summary>
        /// Search with very low detail, for the
        /// fastest results.
        /// </summary>
        public MapOptions LowDetailOption = new MapOptions() { Look = true, Quality = SearchQuality.LOWEST };

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        /// <returns></returns>
        public string GetName() {
            return "Shield aura";
        }

        /// <summary>
        /// Description of what the plugin does.
        /// </summary>
        /// <returns></returns>
        public string GetDescription() {
            return "Follows the player and attacks anybody that gets close!";
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
            return "1.04.00";
        }

        /// <summary>
        /// All settings should be stored here.
        /// (NULL if there shouldn't be any settings)
        /// </summary>
        public IPluginSetting[] Setting { get; set; } = 
        {
            new StringSetting("Owner name/uuid", "Player that the bots will follow.", ""),
            new NumberSetting("Clicks per second", "How fast should the bot attack?", 5, 1, 60, 1),
            new NumberSetting("Miss rate", "How often does the bot miss?", 15, 0, 100, 1),
            new StringSetting("Friendly name(s)/uuid(s)", "Uuids of the user that own't be hit. Split by spaces'", ""),
            new BoolSetting("Auto equip best armor?", "Should the bot auto equip the best armor it has?", true),
            new BoolSetting("Equip best weapon?", "Should the best item be auto equiped?", true),
            new ComboSetting("Mode", null, new string[] { "Passive", "Aggressive" }, 0),
        };

        /// <summary>
        /// Player entity of the owner.
        /// </summary>
        private ILiving ownerEntity { get; set; }

        /// <summary>
        /// How many movement ticks have passed.
        /// (Used to save cpu)
        /// </summary>
        private int ticks;
        /// <summary>
        /// How many ticks have passed since
        /// we attempted to attack the target.
        /// </summary>
        private int hitTicks;
        /// <summary>
        /// Are we following a path.
        /// </summary>
        private bool moving;

        /// <summary>
        /// UUID info of the owner.
        /// </summary>
        public UUID ownerUuid;
        
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
        public ResolvableNameCollection Names;

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
            if (!player.settings.loadWorld) {
                Console.WriteLine("[ShieldAura] 'Load world' must be enabled.");
                return new PluginResponse(false, "'Load world' must be enabled.");
            }
            //Check if bot settings are valid.
            if (!player.settings.loadEntities || !player.settings.loadPlayers) {
                Console.WriteLine("[ShieldAura] 'Load players' must be enabled.");
                return new PluginResponse(false, "'Load players' must be enabled.");
            }

            //Check if the variables are valid.
            if (string.IsNullOrWhiteSpace(Setting[0].Get<string>())) {
                Console.WriteLine("[ShieldAura] Invalid owner uuid.");
                return new PluginResponse(false, "Invalid owner uuid.");
            }
            
            //Add friendlies.
            this.Names = new ResolvableNameCollection(Setting[3].Get<string>());

            //Hook start events.
            player.physicsEngine.onPhysicsPreTick += PhysicsEngine_onPhysicsPreTick;
            player.events.onInventoryChanged += Events_onHealthUpdate;
            return new PluginResponse(true);
        }

        private bool initialCalled = false;
        private bool inprogress = false;
        private void Events_onHealthUpdate(IPlayer player, bool changed, bool deleted, ushort id, int cdiff) {

            if (deleted || ownerEntity == null) return;
            initialCalled = true;

            //Go on a new thread as this not very efficient,
            //and freezes the threads.
            ThreadPool.QueueUserWorkItem(state =>
            {
                if (!inprogress) {
                    inprogress = true;

                    //Check if we should auto-equip.
                    if (Setting[4].Get<bool>()) {

                        player.functions.OpenInventory();
                        if (player.functions.EquipBest(EquipmentSlots.Head,
                            ItemsGlobal.itemHolder.helmets))
                            Thread.Sleep(100);
                        if (player.functions.EquipBest(EquipmentSlots.Chest,
                            ItemsGlobal.itemHolder.chestplates))
                            Thread.Sleep(100);
                        if (player.functions.EquipBest(EquipmentSlots.Pants,
                            ItemsGlobal.itemHolder.leggings))
                            Thread.Sleep(100);
                        player.functions.EquipBest(EquipmentSlots.Boots,
                            ItemsGlobal.itemHolder.boots);
                        player.functions.CloseInventory();
                    }

                    //Check if we should auto select weapon.
                    if (Setting[5].Get<bool>()) {

                        //Make sure we aren't eating.
                        while(player.status.eating)
                            Thread.Sleep(25);

                        //Find best weapon.
                        player.functions.EquipWeapon();
                    }

                    inprogress = false;
                }
            });
        }

        private void PhysicsEngine_onPhysicsPreTick(IPlayer player) {

            //Check if we should stop the plugin.
            if (stopToken.stopped) {
                player.physicsEngine.onPhysicsPreTick -= PhysicsEngine_onPhysicsPreTick;
                player.events.onInventoryChanged -= Events_onHealthUpdate;
                return;
            }

            //Don't do anything if dead.
            if (player.status.entity.isDead) return;

            //Increment update ticks.
            ticks++;

            //Check if we need to find the owner.
            if (ownerEntity == null || ownerEntity.unloaded) {
                //Attempt to find a new owner.
                this.ownerEntity = GetOwner(player);
                if (!initialCalled)
                    Events_onHealthUpdate(player, true, false, 0, -1);
            }

            //Add unadded friendlies.
            if(Names.HasUnresolved())
                Names.Resolve(player.entities);

            //Attempt to attack targets.
            var entity = Attack(player);

            //Check if we have an owner entity.
            if (this.ownerEntity == null || ownerEntity.unloaded || ticks < 6 || moving) return; //No player found.

            //Reset the ticks.
            ticks = 0;

            //Update the movement state.
            //this.moving = true;
            IEntity moveTarget = ownerEntity;

            //Check if we should go on the aggresive stance.
            if (entity != null && Setting[6].Get<int>() == 1 && entity.location.Distance(this.ownerEntity.location) <5)
                moveTarget = entity;
            else if (this.ownerEntity.location.Distance(player.status.entity.location) < 1) return;

            //this.moving = true;

            //Create the map and hook all the
            //callbacks.
            if (player.physicsEngine.path == null || player.physicsEngine.path.Complete || !player.physicsEngine.path.Valid) {
                var map = player.functions.AsyncMoveToEntity(moveTarget, stopToken, LowDetailOption);
                //Hook the callbacks.
                map.Completed += areaMap => OnPathReached();
                map.Cancelled += (areaMap, cuboid) => OnPathFailed();
                map.WaypointReached += areaMap =>
                {
                    //Get the target that we should follow.
                    var closestPlayer = player.entities.FindCloestTarget(player.status.entity.location.ToLocation(0), Targeter.DefaultFilter);
                    var currentTarget = ownerEntity;
                    if (closestPlayer != null && Setting[6].Get<int>() == 1 && (ownerEntity==null || closestPlayer.location.Distance(this.ownerEntity.location) < 6))
                        currentTarget = closestPlayer;

                    if(currentTarget != null && currentTarget.location.Distance(player.status.entity.location) < 1 && player.physicsEngine.path?.Complete == false)
                        areaMap.CalculateFromNext(player.world, currentTarget);
                    else // Calculate path to the newest owners location.
                        areaMap.CalculateFromNext(player.world, ownerEntity);
                };

                //Start the pathing process.
                map.Start();

                //Check if the path is instantly completed.
                if (map.Searched && map.Complete && map.Valid)
                    OnPathReached();
            }
        }
        private static Random rnd = new Random();

        private void OnPathReached()
        {
            //Reset the ticks.
            this.ticks = 0;
            //Unblock the ticking loop.
            this.moving = false;
        }

        private void OnPathFailed()
        {
            //Reset the ticks.
            this.ticks = -(int)10;
            //Unblock the ticking loop.
            this.moving = false;
        }


        private ILiving GetOwner(IPlayer player) {

            //Attempt to get the uuid if it's null.
            if (ownerUuid == null) {
                
                //Check if we have the name or the uuid.
                if (Setting[0].Get<string>().Length == 32 || Setting[0].Get<string>().Length == 36) {

                    //The user inputed a uuid.
                    this.ownerUuid = new UUID(Setting[0].Get<string>(), null);
                }
                else {
                    
                    //Attempt to find a name.
                    this.ownerUuid = player.entities.FindUuidByName(Setting[0].Get<string>());
                    if(ownerUuid != null)
                        Targeter.IgnoreList.Add(ownerUuid.Uuid);
                }
            }

            //Do not progres if the uuid is null.
            if (ownerUuid == null) return null;

            //Loop through all players.
            foreach (var entity in player.entities.playerList) {

                //Check if entity is valid.
                if(entity.Value == null) continue;

                //Find the player with the owners user id.
                if (((IPlayerEntity)entity.Value).uuid == ownerUuid.Uuid.Replace("-", ""))
                    return entity.Value;
            }

            //Owner entity was not found.
            return null;
        }

        private IEntity Attack(IPlayer player) {

            //Attack surrounding players.
            var closestPlayer = player.entities.FindCloestTarget(player.status.entity.location.ToLocation(0), Targeter.DefaultFilter);

            //Attack player if found.
            if (closestPlayer != null) {

                //Look at the target.
                player.functions.LookAt(closestPlayer.location, true);

                //Check if we should attack.
                hitTicks++;

                // 1 hit tick is about 50 ms.
                int ms = hitTicks*50;

                if (ms >= (1000/Setting[1].Get<int>())) {
                    //Hitting, reset tick counter.
                    hitTicks = 0;

                    //Check if we should miss.
                    if (rnd.Next(1, 101) < Setting[2].Get<int>())
                        //Miss.
                        player.functions.PerformSwing();
                    else
                        //Hit.
                        player.functions.EntityAttack(closestPlayer.entityId);
                }
            }
            return closestPlayer;
        }
    }

    public class ResolvableNameCollection
    {
        public List<ResolvableName> Names = new List<ResolvableName>();
        private int m_unresolved = 0;

        public ResolvableNameCollection(string friendlyNames) {
            
            if (!string.IsNullOrWhiteSpace(friendlyNames)) {

                string[] friendlyArray;
                    
                //Check if multiple.
                if (friendlyNames.Contains(' ')) friendlyArray = friendlyNames.Split(' ');
                else friendlyArray = new []{friendlyNames};

                for(int i = 0; i < friendlyArray.Length; i++)
                    if (friendlyArray[i].Length == 32 || friendlyArray[i].Length == 36)
                        Add(new ResolvableName(friendlyArray[i].Replace("-", "")));
                    else
                        Add(new ResolvableName(friendlyArray[i], false));
            }
        }

        public void Add(ResolvableName Name) {
            if (Name == null || !Name.Resolved) {
                m_unresolved += 1;
                return; // Safety check.
            }

            this.Names.Add(Name);
            Targeter.IgnoreList.Add(Name.Uuid);
        }

        public bool HasUnresolved() {
            return m_unresolved > 0;
        }

        public void Resolve(IEntityList entities) {
            for(int i = 0; i < Names.Count; i++)
                if (!Names[i].Resolved) {
                    if (Names[i].Resolve(entities))
                        this.m_unresolved -= 1;
                }
        }
    }

    public class ResolvableName
    {
        public string Name;
        public string Uuid;
        public bool Resolved;

        public ResolvableName(string Name, bool Resolved) {
            this.Name = Name;
            this.Resolved = Resolved;
        }
        public ResolvableName(string Uuid) {
            this.Uuid = Uuid;
            this.Resolved = true;
        }

        public bool Resolve(IEntityList entities) {
            this.Uuid = entities.FindUuidByName(Name)?.Uuid;
            this.Resolved = this.Uuid != null;
            if (this.Resolved)
                Targeter.IgnoreList.Add(this.Uuid);
            return this.Resolved;
        }
    }
}
