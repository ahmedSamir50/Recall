using FluentAssertions;
using Recall.Core.Device;
using Recall.Core.Ports;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class DeviceRecordServiceTests
{
    [Fact]
    public void ChooseId_AcceptsNonEmptyCandidate()
    {
        var store = new InMemoryDeviceRecordStore();
        var os = new FakeDeviceOsNameProvider { OsName = "Pixel 9" };
        var ids = new FakeDeviceIdCandidateProvider { Candidate = new DeviceIdCandidate("abc123", true) };
        var random = new TrackingRandomDeviceIdSource();
        var service = new DeviceRecordService(store, os, ids, random);

        service.GetDeviceId().Should().Be("abc123");
        random.Calls.Should().Be(0);
    }

    [Fact]
    public void ChooseId_RejectsKnownBadAndroidId()
    {
        var store = new InMemoryDeviceRecordStore();
        var os = new FakeDeviceOsNameProvider();
        var ids = new FakeDeviceIdCandidateProvider
        {
            Candidate = new DeviceIdCandidate(DevicePlatformIds.KnownBadAndroidId, true),
        };
        var random = new TrackingRandomDeviceIdSource { Next = "random-fallback" };
        var service = new DeviceRecordService(store, os, ids, random);

        service.GetDeviceId().Should().Be("random-fallback");
        random.Calls.Should().Be(1);
    }

    [Fact]
    public void ChooseId_RejectsWhitespaceCandidate()
    {
        var store = new InMemoryDeviceRecordStore();
        var os = new FakeDeviceOsNameProvider();
        var ids = new FakeDeviceIdCandidateProvider { Candidate = new DeviceIdCandidate("   ", true) };
        var random = new TrackingRandomDeviceIdSource { Next = "random-fallback" };
        var service = new DeviceRecordService(store, os, ids, random);

        service.GetDeviceId().Should().Be("random-fallback");
    }

    [Fact]
    public void EnsureLoaded_DoesNotSaveWhenCandidateNotReady()
    {
        var store = new InMemoryDeviceRecordStore();
        var os = new FakeDeviceOsNameProvider();
        var ids = new FakeDeviceIdCandidateProvider { Candidate = new DeviceIdCandidate(null, false) };
        var random = new TrackingRandomDeviceIdSource();
        var service = new DeviceRecordService(store, os, ids, random);

        var act = () => service.GetDeviceId();
        act.Should().Throw<DeviceIdNotReadyException>();
        store.State.Should().BeNull();
        random.Calls.Should().Be(0);
    }

    [Fact]
    public void EnsureLoaded_KeepsFirstStoredId_WhenLaterCandidateDiffers()
    {
        var store = new InMemoryDeviceRecordStore();
        var os = new FakeDeviceOsNameProvider();
        var ids = new FakeDeviceIdCandidateProvider { Candidate = new DeviceIdCandidate("first-id", true) };
        var random = new TrackingRandomDeviceIdSource();
        var first = new DeviceRecordService(store, os, ids, random);
        first.GetDeviceId().Should().Be("first-id");

        ids.Candidate = new DeviceIdCandidate("second-id", true);
        var second = new DeviceRecordService(store, os, ids, random);
        second.GetDeviceId().Should().Be("first-id");
    }

    [Fact]
    public void TrySetDisplayName_LeavesDeviceIdAndOriginalOsNameUnchanged()
    {
        var store = new InMemoryDeviceRecordStore();
        var os = new FakeDeviceOsNameProvider { OsName = "MacBook Pro" };
        var ids = new FakeDeviceIdCandidateProvider { Candidate = new DeviceIdCandidate("device-1", true) };
        var service = new DeviceRecordService(store, os, ids, new TrackingRandomDeviceIdSource());
        var id = service.GetDeviceId();
        var original = service.GetOriginalOsName();

        service.TrySetDisplayName("Travel laptop").Should().Be(DisplayNameSetResult.Saved);

        service.GetDeviceId().Should().Be(id);
        service.GetOriginalOsName().Should().Be(original);
        service.GetDisplayName().Should().Be("Travel laptop");
        service.GetInitials().Should().Be("TL");
    }

    [Fact]
    public void TrySetDisplayName_Empty_ReturnsEmpty()
    {
        var store = new InMemoryDeviceRecordStore();
        var service = Create(serviceStore: store);
        service.TrySetDisplayName("   ").Should().Be(DisplayNameSetResult.Empty);
    }

    [Fact]
    public void TrySetDisplayName_EqualName_ReturnsUnchanged()
    {
        var store = new InMemoryDeviceRecordStore();
        var os = new FakeDeviceOsNameProvider { OsName = "Phone" };
        var service = Create(store, os);
        service.TrySetDisplayName("Phone").Should().Be(DisplayNameSetResult.Unchanged);
    }

    [Fact]
    public void EnsureLoaded_StoresEmptyNativeName()
    {
        var store = new InMemoryDeviceRecordStore();
        var os = new FakeDeviceOsNameProvider { OsName = "   " };
        var service = Create(store, os);
        service.GetOriginalOsName().Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "?")]
    [InlineData("Ada", "A")]
    [InlineData("Ada Lovelace", "AL")]
    [InlineData("محمد علي", "مع")]
    public void GetInitials_CoversGraphemeCases(string name, string expectedInitials)
    {
        var store = new InMemoryDeviceRecordStore
        {
            State = new DeviceRecordState("id", string.Empty, name),
        };
        var service = Create(store);
        service.GetInitials().Should().Be(expectedInitials);
    }

    private static DeviceRecordService Create(
        InMemoryDeviceRecordStore? serviceStore = null,
        FakeDeviceOsNameProvider? os = null)
    {
        serviceStore ??= new InMemoryDeviceRecordStore();
        os ??= new FakeDeviceOsNameProvider { OsName = "Test" };
        var ids = new FakeDeviceIdCandidateProvider { Candidate = new DeviceIdCandidate("device-1", true) };
        return new DeviceRecordService(serviceStore, os, ids, new TrackingRandomDeviceIdSource());
    }

    private sealed class InMemoryDeviceRecordStore : IDeviceRecordStore
    {
        public DeviceRecordState? State { get; set; }

        public DeviceRecordState? Load() => State;

        public void Save(DeviceRecordState state) => State = state;
    }

    private sealed class TrackingRandomDeviceIdSource : IRandomDeviceIdSource
    {
        public int Calls { get; private set; }

        public string Next { get; set; } = Guid.NewGuid().ToString("N");

        public string NewDeviceId()
        {
            Calls++;
            return Next;
        }
    }
}
