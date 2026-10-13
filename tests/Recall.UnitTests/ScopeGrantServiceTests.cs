using FluentAssertions;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class ScopeGrantServiceTests
{
    [Fact]
    public void CanContinue_NoFolders_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(new FakeScopeGrantStore());

        // Act
        var canContinue = service.CanContinue();

        // Assert
        canContinue.Should().BeFalse();
    }

    [Fact]
    public void CanContinue_OneFolder_ReturnsTrue()
    {
        // Arrange
        var store = new FakeScopeGrantStore();
        store.Save([new ScopeFolderGrant(@"D:\Photos", "Photos")]);
        var service = CreateService(store);

        // Act
        var canContinue = service.CanContinue();

        // Assert
        canContinue.Should().BeTrue();
    }

    [Fact]
    public void TryAddGrant_ChildOfGrantedParent_ReturnsOverlap()
    {
        // Arrange
        var store = new FakeScopeGrantStore();
        store.Save([new ScopeFolderGrant(@"D:\", "Drive D")]);
        var service = CreateService(store);

        // Act
        var result = service.TryAddGrant(new ScopeFolderGrant(@"D:\Photos", "Photos"));

        // Assert
        result.Status.Should().Be(ScopeGrantAddStatus.OverlapChildOfExistingParent);
        service.ListGrants().Should().HaveCount(1);
    }

    [Fact]
    public void RemoveGrant_LastFolder_DisablesContinue()
    {
        // Arrange
        var store = new FakeScopeGrantStore();
        store.Save([new ScopeFolderGrant(@"D:\Photos", "Photos")]);
        var service = CreateService(store);

        // Act
        service.RemoveGrant(@"D:\Photos");

        // Assert
        service.CanContinue().Should().BeFalse();
        service.ListGrants().Should().BeEmpty();
    }

    [Fact]
    public async Task FolderPicker_Cancelled_ReturnsCancelledOutcome()
    {
        // Arrange
        var picker = new FakeFolderPickerService
        {
            NextResult = new FolderPickerResult(FolderPickerOutcome.Cancelled, null),
        };

        // Act
        var result = await picker.PickFolderAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(FolderPickerOutcome.Cancelled);
        result.Path.Should().BeNull();
    }

    [Fact]
    public async Task FolderPicker_Selected_ReturnsPath()
    {
        // Arrange
        var picker = new FakeFolderPickerService
        {
            NextResult = new FolderPickerResult(FolderPickerOutcome.Selected, @"C:\Docs"),
        };

        // Act
        var result = await picker.PickFolderAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(FolderPickerOutcome.Selected);
        result.Path.Should().Be(@"C:\Docs");
    }

    [Fact]
    public async Task FolderPicker_Denied_ReturnsDeniedOutcome()
    {
        // Arrange
        var picker = new FakeFolderPickerService
        {
            NextResult = new FolderPickerResult(FolderPickerOutcome.Denied, null),
        };

        // Act
        var result = await picker.PickFolderAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Outcome.Should().Be(FolderPickerOutcome.Denied);
    }

    private static ScopeGrantService CreateService(FakeScopeGrantStore store) => new(store);
}
