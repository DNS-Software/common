using System.Diagnostics;
using DNS.Common.Concurrency;
using FluentAssertions;

namespace DNS.Common.Tests.Concurrency;

public sealed class ResourceOrchestratorTests
{
    [Fact]
    public async Task ClaimResource_ShouldBeBlocking_WhenResourceIsAlreadyClaimed()
    {
        // Arrange
        var resource = new Resource();
        resource.ClaimResource(ResourceConsumers.Any);
        
        var claimOperation = Task.Run(() =>
        {
            resource.ClaimResource(ResourceConsumers.OperationA);
            resource.ReleaseResource();
        });
        
        // Act + Assert
        claimOperation.IsCompletedSuccessfully.Should().BeFalse();
        resource.ReleaseResource();
        await claimOperation;
        claimOperation.IsCompletedSuccessfully.Should().BeTrue();
    }
    
    [Fact]
    public async Task ClaimResource_ShouldBeQueuedByPriority_WhenResourceIsAlreadyClaimed()
    {
        // Arrange
        var resource = new Resource();
        var readyToClaim = new CountdownEvent(3);
        var proceedToClaim = new ManualResetEvent(false);
        
        resource.ClaimResource(ResourceConsumers.Any);
        
        Task CreateClaimOperation(ResourceConsumers consumer) => Task.Run(() =>
        {
            readyToClaim.Signal();
            proceedToClaim.WaitOne();
            resource.ClaimResource(consumer);
            Thread.Sleep(100); // do some work
            resource.ReleaseResource();
        });
        
        var claimForOperationA = CreateClaimOperation(ResourceConsumers.OperationA);
        var claimForOperationB = CreateClaimOperation(ResourceConsumers.OperationB);
        var claimForOperationAny = CreateClaimOperation(ResourceConsumers.Any);
        
        // Act
        readyToClaim.Wait();
        proceedToClaim.Set();

        Thread.Sleep(1000); // await sort on slow build servers

        resource.ReleaseResource();
        
        var firstClaimOperation = await Task.WhenAny(claimForOperationA, claimForOperationB, claimForOperationAny);
        
        // Assert
        firstClaimOperation.Should().Be(claimForOperationA);
    }
    
    private class Resource : ResourceOrchestrator<ResourceConsumers>
    {
        protected override List<ResourceConsumers> PrioritisedConsumers => new()
        {
            ResourceConsumers.OperationA,
            ResourceConsumers.OperationB
        };
    }
    
    public enum ResourceConsumers
    {
        OperationA,
        OperationB,
        Any,
    }
}