using System;
using System.Threading;

namespace DNS.Common.Concurrency
{
    public sealed class ExclusiveSession : IDisposable
    {
        private const int NoSession = -1;
        
        private readonly SemaphoreSlim _beginSessionGuard = new SemaphoreSlim(1, 1);
        private readonly Atomic<int> _sessionId = new Atomic<int>(NoSession);

        public bool HasSession => _sessionId.Value != NoSession;
        
        public void BeginSession(int? sessionOwner = null)
        {
            // Make sure only one session is started at a time
            _beginSessionGuard.Wait();
            
            CheckClaimOrAwaitSessionEnd();
            _sessionId.Value = sessionOwner ?? Thread.CurrentThread.ManagedThreadId;
            
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
                throw new InvalidOperationException("Current thread is not owner of the session!");
            }
            
            _sessionId.Value = NoSession;
        }
        
        public void AwaitSessionStarted(int owner) => _sessionId.WaitForValue(owner);
        public void AwaitSessionEnd() => _sessionId.WaitForValue(NoSession);

        private void CheckClaimOrAwaitSessionEnd()
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
            _sessionId?.Dispose();
        }
    }
}

