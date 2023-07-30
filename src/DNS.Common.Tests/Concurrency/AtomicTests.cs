using DNS.Common.Concurrency;
using FluentAssertions;

namespace DNS.Common.Tests.Concurrency;

public sealed class AtomicTests
{
    [Fact]
    public void Atomic_ShouldHoldTypeDefault_WhenInstantiated()
    {
        InstantiateWithDefault<int>();
        InstantiateWithDefault<double>();
        InstantiateWithDefault<object>();
        InstantiateWithDefault<DateTime>();
        InstantiateWithDefault<Exception>();

        static void InstantiateWithDefault<T>()
        {
            var atomic = new Atomic<T>();
            atomic.Value.Should().Be(default(T));
        }
    }
    
    [Fact]
    public void Atomic_ShouldHoldInitialValue_WhenInstantiated()
    {
        InstantiateWithInitialValue(1);
        InstantiateWithInitialValue(0.1);
        InstantiateWithInitialValue(DateTime.Now);
        InstantiateWithInitialValue<Exception>(new ArgumentException());

        static void InstantiateWithInitialValue<T>(T initialValue)
        {
            var atomic = new Atomic<T>(initialValue);
            atomic.Value.Should().Be(initialValue);
        }
    }
    
    [Fact]
    public void Atomic_ShouldThrowArgumentException_WhenInstantiatedWithAtomicValue()
    {
        InstantiateWithInvalidType<Atomic<int>>();
        InstantiateWithInvalidTypeAndInitialValue(new Atomic<int>());
        
        void InstantiateWithInvalidType<T>()
        {
            Action act = () => new Atomic<T>();
            act.Should().Throw<ArgumentException>("Genric type T cannot be of type Atomic<T>");
        }
        
        void InstantiateWithInvalidTypeAndInitialValue<T>(T initialValue)
        {
            Action act = () => new Atomic<T>(initialValue);
            act.Should().Throw<ArgumentException>("Genric type T cannot be of type Atomic<T>");
        }
    }

    [Fact]
    public void Atomic_ShouldWakeUpThread_WhenValueChanges()
    {
        // Arrange
        const int maxWaitTime = 100;

        var atomic = new Atomic<int>();
        var waitForValueTask = Task.Run(() => atomic.WaitForValue(1));

        // Act
        atomic.Value = 1;
        var waitResult = waitForValueTask.Wait(maxWaitTime);

        // Assert
        waitResult.Should().BeTrue();
        waitForValueTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void AtomicWaitForValue_ShouldBeBlocked_WhenDesiredValueNeverAchieved()
    {
        // Arrange
        const int maxWaitTime = 100;

        var atomic = new Atomic<int>();
        var waitForValueTask = Task.Run(() => atomic.WaitForValue(1));

        // Act
        var waitResult = waitForValueTask.Wait(maxWaitTime);

        // Assert
        waitResult.Should().BeFalse();
        waitForValueTask.IsCompletedSuccessfully.Should().BeFalse();
        waitForValueTask.Status.Should().Be(TaskStatus.Running);
    }
}