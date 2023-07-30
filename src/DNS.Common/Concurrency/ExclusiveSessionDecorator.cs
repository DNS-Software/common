using System;

namespace DNS.Common.Concurrency
{
    public abstract class ExclusiveSessionDecorator<T> : IDisposable
    {
        private bool _disposed = false;
        
        protected T Service { get; }
        protected ExclusiveSession ExclusiveSession { get; }

        protected ExclusiveSessionDecorator(T service)
        {
            Service = service;
            ExclusiveSession = new ExclusiveSession();
        }
        
        ~ExclusiveSessionDecorator() => Dispose(false);

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
                ExclusiveSession.Dispose();
                
                if (typeof(T).IsAssignableFrom(typeof(IDisposable)))
                {
                    ((IDisposable) Service).Dispose();
                }
            }
            
            _disposed = true;
        }
    }
}

