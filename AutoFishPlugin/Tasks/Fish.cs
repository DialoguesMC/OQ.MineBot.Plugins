using System;
using System.Linq;
using OQ.MineBot.PluginBase.Base.Plugin.Tasks;
using OQ.MineBot.PluginBase.Classes.Objects;
using OQ.MineBot.PluginBase.Classes.Objects.List;

namespace AutoFishPlugin.Tasks
{
    public class Fish : ITask, ITickListener, IStartListener
    {
        private static readonly double[] MOTION_Y_TRESHOLD = {
            -0.02,
            -0.035,
            -0.05,
        };
        private static readonly int[] REACTION_SPEEDS = {
            0,
            -5,
            -10,
        };
        private const int CAST_TIME     = 6; // How many seconds should we wait before we can reel back in. (seconds) 
        private const int MAX_WAIT_TIME = 60; // How long can we wait before reeling in (and retrying). (seconds) 
        private const int ROD_ID        = 346;
        private const int WATER_ID      = 9;

        private bool     fishing;
        private DateTime castTime; // When did we start the fishing process.
        private DateTime maxWaitTime; // Maximum time the bot can wait until reeling in.

        private FishingFloatObject lureObject;
        private bool lureSpawned
        {
            get { return lureObject != null; }
            set { if (value == false) lureObject = null; }
        }

        private bool castTick = false;
        private bool lookTick = true;
        private int  tick     = 0;
        private bool reelTick = false;

        private readonly bool keepRotation;
        private readonly int  sensitivity;
        private readonly int  reactionSpeed;

        public Fish(bool keepRotation, int sensitivity, int reactionSpeed) {
            this.keepRotation = keepRotation;
            this.sensitivity = sensitivity;
            this.reactionSpeed = reactionSpeed;
        }

        public override bool Exec() {
            return !status.entity.isDead && !status.eating;
        }

        public void OnStart() {
            player.events.onObjectSpawned += ObjectSpawned;
            player.events.onEntityVelocity += EntityVelocity;
        }

        public void OnTick() {
            
             // Check if we should reset the 
            // fishing state.
            if (!FishingState()) {
                ResetState();
                return;
            }

            // Check ticks.
            if (tick < 0) {
                tick++;
                return;
            }
            tick = -5;

            if (this.lookTick && keepRotation) {
                LookAtWater();
                this.lookTick = false;
                return;
            }

            // Check if we have the rod equiped.
            if (!IsRodEquiped()) {
                ResetState();
                EquipRod();
                return;
            }

            // Check if we should cast this tick.
            if (this.castTick) {
                if (this.reelTick) {
                    player.functions.UseSelectedItem(); // Right click the rod.
                    this.reelTick = false;
                    this.tick = -10;
                    ResetState();
                    Recast();
                }
                else {
                    player.functions.UseSelectedItem(); // Right click the rod.
                    this.castTime = DateTime.Now;
                    this.maxWaitTime = DateTime.Now.AddSeconds(MAX_WAIT_TIME);
                    this.fishing = true;
                    this.castTick = false;
                    this.tick = -20;
                }
                return;
            }

            // Check if we are not fishing, but
            // our lure state is spawned.
            if (!this.fishing && this.lureSpawned) {
                Recast();
                return;
            }

            // Check if we are still fishing.
            if (this.fishing && this.lureSpawned && !ForceReel())
                return; // Wait for fish.

            // Check if we are still on the waiting process.
            if (DateTime.Now.Subtract(castTime).TotalSeconds < CAST_TIME)
                return;

            // Check if the lure didn't spawn in.
            // (CAST_TIME has already passed)
            if (this.fishing && !this.lureSpawned) {
                Recast();
                return;
            }
            
            // Check if we need to force reel in.
            if (this.fishing && ForceReel()) {
                Recast();
                return;
            }

            this.castTick = true; // Cast.
        }

        private void EntityVelocity(int entityId, short x, short y, short z) {
            if (token.stopped) return;

            // Check if we should care about this
            // velocity change.
            if (!this.fishing || !this.lureSpawned || this.lureObject.Id != entityId)
                return;

            // Check if we are not on the throw timer.
            if (DateTime.Now.Subtract(castTime).TotalSeconds < CAST_TIME)
                return;

            double yd = (double)y / 8000;
            if (x != 0 || z != 0 || yd > MOTION_Y_TRESHOLD[sensitivity]) return;

            // Reel in, we got a fish probably.
            this.reelTick = true;
            Recast();
        }

        private void ObjectSpawned(IWorldObject worldObject, double X, double Y, double Z, byte pitch, byte yaw) {
            if (token.stopped) return;

            // We only care for fishing hook spawns.
            if (worldObject.GetType() != ObjectTypes.FishingHook) return;

            var hook = (FishingFloatObject)worldObject;
            if (hook.Owner != player.status.entity.entityId) return; // We care only for our hook.
            this.lureObject = hook; // Assign the lure object.
        }

        private bool IsRodEquiped() {
            return inventory.hotbar.GetSlot((byte)player.status.entity.selectedSlot).id == ROD_ID;
        }
        private bool FishingState() {
            return !status.eating && !status.entity.isDead;
        }

        private bool ForceReel() {
            return DateTime.Now.Subtract(maxWaitTime).TotalMilliseconds > 0;
        }
        private void EquipRod() {
            inventory.Select(ROD_ID);
        }

        private void ResetState() {
            this.fishing = false;
            this.lureSpawned = false;
        }
        private void Recast() {
            this.castTick = true;
            tick = REACTION_SPEEDS[reactionSpeed];
        }

        private void LookAtWater()
        {

            var blocks = world.GetBlockLocations(status.entity.location.X, status.entity.location.Y, status.entity.location.Z, 16, 6, WATER_ID);
            blocks = blocks.OrderBy(x => x.Distance(player.status.entity.location.ToLocation())).ToArray();

            for (int i = 0; i < blocks.Length; i++)
                if (world.GetBlockId(blocks[i].x, (int)blocks[i].y + 1, blocks[i].z) != WATER_ID && blocks[i].Distance(player.status.entity.location.ToLocation(1)) > 1.75) {
                    actions.LookAtBlock(blocks[i].Offset(0, 0.8f, 0));
                    break;
                }
        }

    }
}