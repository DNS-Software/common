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

        private readonly AutoResetEvent _checkPriorityQueue;
        private readonly PriorityQueue<TResourceConsumers, int> _priorityQueue;
        private readonly ExclusiveSession _exclusiveSession;

        protected abstract List<TResourceConsumers> PrioritisedConsumers { get; }

        protected ResourceOrchestrator()
        {
            _checkPriorityQueue = new AutoResetEvent(false);
            _priorityQueue = new PriorityQueue<TResourceConsumers, int>(PrioritisedConsumers);
            _exclusiveSession = new ExclusiveSession();

            _exclusiveSession.SessionEnded += CheckPriorityQueue;
            _priorityQueue.ValueEnqueued += CheckPriorityQueue;

            var awaitOrchastratorRunning = new AutoResetEvent(false);

            Task.Run(() =>
            {
                awaitOrchastratorRunning.Set();
                
                while (!_disposed)
                {
                    _checkPriorityQueue.WaitOne();

                    if (_priorityQueue.IsEmpty)
                    {
                        continue;
                    }
                    
                    if (_exclusiveSession.HasSession)
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
                _priorityQueue.ValueEnqueued -= CheckPriorityQueue;
                _exclusiveSession.SessionEnded -= CheckPriorityQueue;
                
                _checkPriorityQueue?.Dispose();
                _exclusiveSession?.Dispose();
            }

            _disposed = true;
        }

        private void CheckPriorityQueue() => _checkPriorityQueue.Set();
    }
}