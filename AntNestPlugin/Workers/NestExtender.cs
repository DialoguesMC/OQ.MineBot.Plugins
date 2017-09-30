using AntNestPlugin.Nest;
using OQ.MineBot.GUI.Protocol.Movement.Maps;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Classes.Base;
using OQ.MineBot.PluginBase.Movement.Maps;

namespace AntNestPlugin.Workers
{
    public class NestExtender : INestWorker
    {
        public MapOptions PathToNest = new MapOptions() { Look = false, Quality = SearchQuality.HIGH, Mine = true };
        public MapOptions PathToWork = new MapOptions() { Look = false, Quality = SearchQuality.LOW, Mine = true };

        /// <summary>
        /// Current objective for this worker.
        /// </summary>
        public WorkerObjective Objective { get; } = WorkerObjective.Extend;

        /// <summary>
        /// Current state of this worker.
        /// </summary>
        public WorkerState State { get; private set; } = WorkerState.None;

        private readonly IPlayer m_player;
        private readonly NestManager m_nest;

        public NestExtender(IPlayer player, NestManager nest) {
            this.m_player = player;
            this.m_nest = nest;
        }

        /// <summary>
        /// Called each tick and attempts
        /// to do some work.
        /// </summary>
        public void Work() {
            
            // Switch by state.
            switch (State) {
                case WorkerState.None:
                    // Just started, or covering after a death.
                    MoveToNest();
                    break;
            }
        }

        #region Move to nest

        /// <summary>
        /// Attempts to move to the nest.
        /// </summary>
        private void MoveToNest() {

            // Check if we have a nest location.
            if (m_nest.Home == null) return;
            // Update current state.
            this.State = WorkerState.MovingToNest;

            // Attempt to create a path to the nest.
            var map = m_player.functions.AsyncMoveToLocation(m_nest.Home, new CancelToken(), PathToNest);
            map.Completed += NestReached;
            map.Cancelled += (areaMap, cuboid) => this.State = WorkerState.None;
            // Begin moving to location.
            map.Start();
        }

        private void NestReached(IAreaMap map) {
            this.State = WorkerState.Mining;
        }

        #endregion
    }
}