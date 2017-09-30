using System.Collections.Concurrent;
using System.Collections.Generic;
using OQ.MineBot.PluginBase.Classes;

namespace AntNestPlugin.Nest
{
    /*
        Locations that the workers should
        mine at.
    */
    public class NestLocations
    {
        /// <summary>
        /// ILocation - location.
        /// Bool - taken.
        /// 
        /// Keep all the locations where
        /// the workers can mine.
        /// </summary>
        public ConcurrentDictionary<ILocation, bool> Locations = new ConcurrentDictionary<ILocation, bool>();

        /// <summary>
        /// Attempts to get the nearest location
        /// to the worker.
        /// </summary>
        public ILocation GetNearest(ILocation location) {

            var locations = Locations.ToArray();
            var distance = double.MaxValue;
            ILocation currentLocation = null;

            for(int i = 0; i < locations.Length; i++)
                if (!locations[i].Value) {
                    var currentDistance = locations[i].Key.Distance(location);
                    if (currentDistance < distance) {
                        distance = currentDistance;
                        currentLocation = locations[i].Key;
                    }
                }
            if (currentLocation == null) return null; // All locations taken.

            // Update the loation's state.
            Locations.TryUpdate(currentLocation, true, false);
            return currentLocation;
        }

        /// <summary>
        /// Callback called once a location
        /// is finished by a worker.
        /// </summary>
        public void LocationCompleted(ILocation location) {
            if (Locations.ContainsKey(location)) {
                bool val;
                Locations.TryRemove(location, out val);
            }
        }
    }
}