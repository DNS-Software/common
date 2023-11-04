using System.Diagnostics;
using DNS.Common.Concurrency;
using FluentAssertions;

namespace DNS.Common.Tests.Concurrency;

public sealed class ExclusiveSessionTests : IDisposable
{
    private readonly ExclusiveSession _exclusiveSession;
    
    public ExclusiveSessionTests()
    {
        _exclusiveSession = new ExclusiveSession();
    }
    
    public void Dispose()
    {
        _exclusiveSession.Dispose();
    }
    
    [Fact]
    public void BeginExclusiveSession_ShouldNotThrow_WhenInvoked()
    {
        // Arrange + Act
        Action act = () => _exclusiveSession.BeginSession();
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void BeginExclusiveSession_ShouldAwaitCurrentSession_WhenInvoked()
    {
        // Arrange
        const int maxTimeToAwait = 100;
        var timer = new Stopwatch();
        
        _exclusiveSession.BeginSession();
        
        // Act
        var beginSecondSessionTask = Task.Run(() => 
        {
            timer.Start();
            _exclusiveSession.BeginSession();
        });
        
        beginSecondSessionTask.Wait(maxTimeToAwait);
        timer.Stop();

        // Assert
        beginSecondSessionTask.IsCompleted.Should().BeFalse();
        timer.Elapsed.TotalMilliseconds.Should().BeGreaterOrEqualTo(maxTimeToAwait);
    }
    
    [Fact]
    public void EndExclusiveSession_ShouldNotThrow_WhenASessionIsActive()
    {
        // Arrange
        _exclusiveSession.BeginSession();
        
        // Act
        Action act = () => _exclusiveSession.EndSession();
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void EndExclusiveSession_ShouldNotThrow_WhenNoSessionIsActive()
    {
        // Act
        Action act = () => _exclusiveSession.EndSession();
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void EndExclusiveSession_ShouldThrow_WhenCurrentThreadIsNotTheSessionOwner()
    {
        // Arrange
        Task.Run(() => _exclusiveSession.BeginSession()).Wait();
        
        // Act
        Action act = () => _exclusiveSession.EndSession();

        // Assert
        act.Should().Throw<InvalidOperationException>("Current thread is not owner of the session, session is ended without ever starting");
    }
    
    [Fact]
    public void ExclusiveSessions_ShouldNotRunInParallel_WhenInvoked()
    {
        var timer = Stopwatch.StartNew();
        var firstSession = Task.Run(() =>
        {
            _exclusiveSession.BeginSession();
            Thread.Sleep(10);
            _exclusiveSession.EndSession();
        });
        var secondSession = Task.Run(() =>
        {
            _exclusiveSession.BeginSession();
            Thread.Sleep(10);
            _exclusiveSession.EndSession();
        });
        
        Task.WaitAll(firstSession, secondSession);
        timer.Stop();
        
        timer.Elapsed.TotalMilliseconds.Should().BeGreaterOrEqualTo(20);
    }
}