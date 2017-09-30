using System.Collections.Concurrent;
using AntNestPlugin.Workers;
using OQ.MineBot.PluginBase;

namespace AntNestPlugin.Nest
{
    public class RoleAssigner
    {
        /// <summary>
        /// All registered workers are 
        /// stored here.
        /// </summary>
        public ConcurrentDictionary<IPlayer, INestWorker> Workers = new ConcurrentDictionary<IPlayer, INestWorker>();
        /// <summary>
        /// Do we have a worker, who's job is
        /// to extend the nest?
        /// </summary>
        private bool m_containsExtender { get; set; }

        public INestWorker Assign(IPlayer player, NestManager nest) {

            // Check if this player is already
            // assigned.
            if (Workers.ContainsKey(player))
                return Workers[player];

            // Store the role here, after 
            // registering it in the disctionary
            // return it.
            INestWorker role;

            // If we don't have any extenders, then
            // assign this role to the joiner.
            if (m_containsExtender == false) {
                this.m_containsExtender = true;
                role  = new NestExtender(player, nest);
            }
            else role = new NestSearcher();

            // Attempt to register the role.
            Workers.TryAdd(player, role);
            return role;
        }

        public void Leave(IPlayer player) {

            // Check if this player wasn't
            // registered.
            if (Workers.ContainsKey(player)) return;

            // Check if the players role was to extend.
            if (Workers[player].Objective == WorkerObjective.Extend) {
                m_containsExtender = false;
            }
            // Remove from the tracked list.
            INestWorker worker;
            Workers.TryRemove(player, out worker);
        }
    }
}