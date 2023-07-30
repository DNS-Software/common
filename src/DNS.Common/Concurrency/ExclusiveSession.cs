using System;
using System.Threading;

namespace DNS.Common.Concurrency
{
    public sealed class ExclusiveSession : IDisposable
    {
        private const int NoSession = -1;
        
        private readonly Semaphore _beginSessionGuard = new Semaphore(1, 1);
        private readonly Atomic<int> _sessionId = new Atomic<int>(NoSession);        
        
        public void BeginSession()
        {
            // Make sure only one session is started at a time
            _beginSessionGuard.WaitOne();
            
            CheckClaimOrAwaitSessionEnd();
            _sessionId.Value = Thread.CurrentThread.ManagedThreadId;
            
            _beginSessionGuard.Release();
        }
        
        public void EndSession()
        {
            if (_sessionId.Value == NoSession)
            {
                return;
            }
            
            if (_sessionId.Value != Thread.CurrentThread.ManagedThreadId)
            {
                throw new InvalidOperationException("Current thread is not owner of the session, session is ended without ever starting");
            }
            
            _sessionId.Value = NoSession;
        }
        
        public void CheckClaimOrAwaitSessionEnd()
        {
            if (_sessionId.Value == NoSession)
            {
                return;
            }
            
            if (_sessionId.Value == Thread.CurrentThread.ManagedThreadId)
            {
                return;
            }
            
            _sessionId.WaitForValue(NoSession);
        }

        public void Dispose()
        {
            _beginSessionGuard?.Dispose();
        }
    }
}

