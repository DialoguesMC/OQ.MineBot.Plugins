namespace AntNestPlugin.Workers
{
    public class NestSearcher : INestWorker
    {
        /// <summary>
        /// Current objective for this worker.
        /// </summary>
        public WorkerObjective Objective { get; } = WorkerObjective.Search;

        /// <summary>
        /// Current state of this worker.
        /// </summary>
        public WorkerState State { get; } = WorkerState.None;

        /// <summary>
        /// Called each tick and attempts
        /// to do some work.
        /// </summary>
        public void Work() {
            throw new System.NotImplementedException();
        }
    }
}