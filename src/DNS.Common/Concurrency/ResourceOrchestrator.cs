using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DNS.Common.Concurrency
{
    /// <summary>
    /// Orchestrates a resource exclusive session via a priority queueing mechanism
    /// </summary>
    public abstract class ResourceOrchestrator<TResourceConsumers> : IDisposable
    {
        private bool _disposed;

        private readonly PriorityQueue<TResourceConsumers, int> _priorityQueue;
        private readonly ExclusiveSession _exclusiveSession;

        protected abstract List<TResourceConsumers> PrioritisedConsumers { get; }

        protected ResourceOrchestrator()
        {
            _priorityQueue = new PriorityQueue<TResourceConsumers, int>(PrioritisedConsumers);
            _exclusiveSession = new ExclusiveSession();

            var awaitOrchastratorRunning = new AutoResetEvent(false);

            Task.Run(() =>
            {
                awaitOrchastratorRunning.Set();
                
                while (!_disposed)
                {
                    _exclusiveSession.AwaitSessionEnd();

                    if (_priorityQueue.IsEmpty)
                    {
                        continue;
                    }

                    _exclusiveSession.BeginSession(_priorityQueue.Dequeue());
                }
            });

            awaitOrchastratorRunning.WaitOne();
        }

        ~ResourceOrchestrator() => Dispose(false);

        public void ClaimResource(TResourceConsumers consumer)
        {
            if (!_exclusiveSession.HasSession)
            {
                _exclusiveSession.BeginSession();
                return;
            }

            var caller = Thread.CurrentThread.ManagedThreadId;

            _priorityQueue.Enqueue(consumer, caller);
            _exclusiveSession.AwaitSessionStarted(caller);
        }

        public void ReleaseResource() => _exclusiveSession.EndSession();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _exclusiveSession?.Dispose();
            }

            _disposed = true;
        }
    }
}