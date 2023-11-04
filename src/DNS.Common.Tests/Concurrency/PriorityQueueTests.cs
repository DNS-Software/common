using FluentAssertions;
using DCC = DNS.Common.Concurrency;

namespace DNS.Common.Tests.Concurrency;

public sealed class PriorityQueueTests
{
    [Fact]
    public void Enqueue_ShouldBeFifo()
    {
        //Arrange 
        var queue = new DCC.PriorityQueue<int, int>();
        
        // Act
        queue.Enqueue(1, 1);
        queue.Enqueue(0, 2);
        queue.Enqueue(1, 3);
        
        // Assert
        queue.Dequeue().Should().Be(1);
        queue.Dequeue().Should().Be(2);
        queue.Dequeue().Should().Be(3);
    }
    
    [Fact]
    public void Enqueue_ShouldBeFifoAndOrderdOnPriority_WhenUsingPriotisedKeys()
    {
        //Arrange 
        var queue = new DCC.PriorityQueue<PriotriyKey, int>(new [] { PriotriyKey.A , PriotriyKey.B, PriotriyKey.C});

        // Act
        queue.Enqueue(PriotriyKey.B, 1);
        queue.Enqueue(PriotriyKey.C, 2);
        queue.Enqueue(PriotriyKey.A, 3);
        queue.Enqueue((PriotriyKey) 10, 4);
        queue.Enqueue(PriotriyKey.A, 5);
        queue.Enqueue(PriotriyKey.C, 6);
        queue.Enqueue(PriotriyKey.B, 7);
        queue.Enqueue(PriotriyKey.A, 8);
        
        // Assert
        queue.Dequeue().Should().Be(3);
        queue.Dequeue().Should().Be(5);
        queue.Dequeue().Should().Be(8);
        queue.Dequeue().Should().Be(1);
        queue.Dequeue().Should().Be(7);
        queue.Dequeue().Should().Be(2);
        queue.Dequeue().Should().Be(6);
        queue.Dequeue().Should().Be(4);
    }
    
    [Fact]
    public void Dequeue_ShouldThrowInvalidOperationException_WhenQueueIsEmpty()
    {
        // Arrange
        var queue = new DCC.PriorityQueue<int, int>();
        
        // Act
        Action act = () => queue.Dequeue();
        
        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
    
    [Fact]
    public void ValueEnqueuedEvent_ShouldBeNonBlocking_WhenInvoked()
    {
        // Arrange
        var queue = new DCC.PriorityQueue<int, int>();
        var valueQueueInvoked = false;
        var awaitMainThread = new AutoResetEvent(false);
        var awaitValueEnQueuedInvoked = new AutoResetEvent(false);
        
        queue.ValueEnqueued += () =>
        {
            awaitMainThread.WaitOne();
            valueQueueInvoked = true;
            awaitValueEnQueuedInvoked.Set();
        };
        
        // Act
        queue.Enqueue(1, 1);
        
        // Assert
        valueQueueInvoked.Should().BeFalse();
        awaitMainThread.Set();
        awaitValueEnQueuedInvoked.WaitOne();
        valueQueueInvoked.Should().BeTrue();
    }
    
    private enum PriotriyKey
    {
        A,
        B,
        C
    }
}