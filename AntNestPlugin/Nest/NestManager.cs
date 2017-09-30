using AntNestPlugin.Workers;
using OQ.MineBot.PluginBase;
using OQ.MineBot.PluginBase.Classes;
using OQ.MineBot.PluginBase.Classes.World;
using OQ.MineBot.Protocols.Classes.Base;

namespace AntNestPlugin.Nest
{
    public class NestManager
    {
        /// <summary>
        /// Y level of the nest.
        /// </summary>
        public int Level { get; private set; }

        /// <summary>
        /// What are the workers looking
        /// for?
        /// </summary>
        public ResourceTypes[] Objectives { get; } = new ResourceTypes[]
        {
            ResourceTypes.Coal,
            ResourceTypes.Diamond,
            ResourceTypes.Emerald,
            ResourceTypes.Gold,
            ResourceTypes.Iron,
            ResourceTypes.Redstone,
            ResourceTypes.Lapis,
        };

        /// <summary>
        /// Base position of the nest.
        /// </summary>
        public ILocation Home { get; private set; }

        /// <summary>
        /// All available locations are stored here.
        /// </summary>
        public NestLocations Locations = new NestLocations();
        /// <summary>
        /// Assigns roles to the players.
        /// </summary>
        private RoleAssigner Roles = new RoleAssigner();

        public INestWorker JoinNest(IPlayer player) {
            
            // Attempt to assign a role for this
            // player.
            var role = Roles.Assign(player, this);

            // Attempt to set the home, based on
            // this players location. If he is the
            // first one to join then the home will
            // be spawned at it's location.
            PickHome(player.status.entity.location.ToLocation(0), player.world);
            return role;
        }
        public void LeaveNest(IPlayer player) {
            Roles.Leave(player);
        }

        /// <summary>
        /// Attempts to pick a home if it's still
        /// not set.
        /// </summary>
        /// <param name="myLocation"></param>
        /// <param name="world"></param>
        private void PickHome(ILocation myLocation, IWorld world) {
            // Check if home already exists.
            if (Home != null) return;
            
            // Assign level.
            this.Level = 12;
            
            // Check if this location is avaialble.
            this.Home = world.FindHorizontal(myLocation, new ushort[] {1});
        }
    }

    public enum ResourceTypes
    {
        Gold = 14,
        Iron = 15,
        Coal = 16,
        Lapis = 21,
        Diamond = 56,
        Redstone = 73,
        Emerald = 129
    }
}