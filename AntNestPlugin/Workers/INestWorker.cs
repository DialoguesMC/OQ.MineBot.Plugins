namespace AntNestPlugin.Workers
{
    public interface INestWorker
    {
        /// <summary>
        /// Current objective for this worker.
        /// </summary>
        WorkerObjective Objective { get; }
        /// <summary>
        /// Current state of this worker.
        /// </summary>
        WorkerState State { get; }

        /// <summary>
        /// Called each tick and attempts
        /// to do some work.
        /// </summary>
        void Work();
    }

    public enum WorkerObjective
    {
        Extend,
        Search
    }

    public enum WorkerState
    {
        None,
        MovingToNest,
        MovingToLocation,
        Mining
    }
}