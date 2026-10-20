using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Recall.Core.DependencyInjection;
using Recall.Core.Ingest;
using Recall.Core.Ports;
using Recall.Core.Scope;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class Phase3GrantTests
{
    [Fact]
    public void TryAddGrant_NullGrant_Throws()
    {
        var service = new ScopeGrantService(new FakeScopeGrantStore());
        Action act = () => service.TryAddGrant(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryAddGrant_BlankPath_ThrowsAndDoesNotSave()
    {
        var store = new FakeScopeGrantStore();
        var service = new ScopeGrantService(store);
        Action act = () => service.TryAddGrant(new ScopeFolderGrant("   ", "blank", GrantFileKinds.All));
        act.Should().Throw<ArgumentException>();
        store.List().Should().BeEmpty();
    }

    [Fact]
    public void TryAddGrant_ChildOfGrantedParent_ReturnsOverlap()
    {
        var store = new FakeScopeGrantStore();
        var service = new ScopeGrantService(store);
        service.TryAddGrant(new ScopeFolderGrant(@"D:\", "drive", GrantFileKinds.All));
        var result = service.TryAddGrant(new ScopeFolderGrant(@"D:\Photos", "photos", GrantFileKinds.All));
        result.Status.Should().Be(ScopeGrantAddStatus.OverlapChildOfExistingParent);
        store.List().Should().HaveCount(1);
    }

    [Fact]
    public void TryAddGrant_ParentTextOnly_KeepsText()
    {
        var store = new FakeScopeGrantStore();
        var service = new ScopeGrantService(store);
        service.TryAddGrant(new ScopeFolderGrant(@"D:\Photos", "photos", GrantFileKinds.Image));
        service.TryAddGrant(new ScopeFolderGrant(@"D:\", "drive", GrantFileKinds.Text));
        var grants = store.List();
        grants.Should().HaveCount(1);
        grants[0].EnabledKinds.Should().Be(GrantFileKinds.Text);
    }

    [Fact]
    public void TryAddGrant_ParentReplacesChild_KeepsDriveRoot()
    {
        var store = new FakeScopeGrantStore();
        var service = new ScopeGrantService(store);
        service.TryAddGrant(new ScopeFolderGrant(@"D:\Photos", "photos", GrantFileKinds.All));
        var result = service.TryAddGrant(new ScopeFolderGrant(@"D:\", "drive", GrantFileKinds.All));
        result.Status.Should().Be(ScopeGrantAddStatus.OverlapParentReplacesChildren);
        var grants = store.List();
        grants.Should().HaveCount(1);
        ScopeGrantPolicy.Normalize(grants[0].Path).Should().Be(ScopeGrantPolicy.Normalize(@"D:\"));
    }

    [Fact]
    public void TryAddGrant_ParentReplacesTwoChildren_KeepsDriveRoot()
    {
        var store = new FakeScopeGrantStore();
        var service = new ScopeGrantService(store);
        service.TryAddGrant(new ScopeFolderGrant(@"D:\Photos", "photos", GrantFileKinds.All));
        service.TryAddGrant(new ScopeFolderGrant(@"D:\PhotosExtra", "extra", GrantFileKinds.All));
        var result = service.TryAddGrant(new ScopeFolderGrant(@"D:\", "drive", GrantFileKinds.All));
        result.Status.Should().Be(ScopeGrantAddStatus.OverlapParentReplacesChildren);
        store.List().Should().HaveCount(1);
    }

    [Fact]
    public void TryAddGrant_PhotosExtra_IsNotAChild()
    {
        var store = new FakeScopeGrantStore();
        var service = new ScopeGrantService(store);
        service.TryAddGrant(new ScopeFolderGrant(@"D:\Photos", "photos", GrantFileKinds.All));
        var result = service.TryAddGrant(new ScopeFolderGrant(@"D:\PhotosExtra", "extra", GrantFileKinds.All));
        result.Status.Should().Be(ScopeGrantAddStatus.Added);
        store.List().Should().HaveCount(2);
    }

    [Fact]
    public void TryAddGrant_Duplicate_DoesNotSave()
    {
        var store = new FakeScopeGrantStore();
        var service = new ScopeGrantService(store);
        service.TryAddGrant(new ScopeFolderGrant(@"D:\Photos", "photos", GrantFileKinds.All));
        var result = service.TryAddGrant(new ScopeFolderGrant(@"D:\Photos", "photos", GrantFileKinds.All));
        result.Status.Should().Be(ScopeGrantAddStatus.Duplicate);
        store.List().Should().HaveCount(1);
    }

    [Fact]
    public void IsContent_Blank_IsFalse()
    {
        ContentUri.IsContent(null).Should().BeFalse();
        ContentUri.IsContent("   ").Should().BeFalse();
    }

    [Fact]
    public void IsContent_ContentScheme_IsTrue()
    {
        ContentUri.IsContent("content://com.example/tree/123").Should().BeTrue();
    }

    [Fact]
    public void IsContent_DrivePath_IsFalse()
    {
        ContentUri.IsContent(@"D:\Photos").Should().BeFalse();
    }

    [Fact]
    public void Choose_WindowsAndroidMacIosUnknown_MapToWatcherKind()
    {
        ScopeWatcherKindPolicy.Choose(RecallShellKind.Windows).Should().Be(ScopeWatcherKind.WindowsFileSystem);
        ScopeWatcherKindPolicy.Choose(RecallShellKind.Android).Should().Be(ScopeWatcherKind.WindowsFileSystem);
        ScopeWatcherKindPolicy.Choose(RecallShellKind.MacCatalyst).Should().Be(ScopeWatcherKind.WindowsFileSystem);
        ScopeWatcherKindPolicy.Choose(RecallShellKind.Ios).Should().Be(ScopeWatcherKind.None);
        ScopeWatcherKindPolicy.Choose(RecallShellKind.Unknown).Should().Be(ScopeWatcherKind.None);
        ScopeWatcherKindPolicy.Choose((RecallShellKind)99).Should().Be(ScopeWatcherKind.None);
    }

    [Fact]
    public void OverlapKey_BothOverlapStatuses_ReturnWarning()
    {
        Recall.UI.Components.FolderGrantMessages.OverlapKey(ScopeGrantAddStatus.OverlapChildOfExistingParent).Should().Be("Folders_OverlapWarning");
        Recall.UI.Components.FolderGrantMessages.OverlapKey(ScopeGrantAddStatus.OverlapParentReplacesChildren).Should().Be("Folders_OverlapWarning");
    }

    [Fact]
    public void OverlapKey_AddedAndDuplicate_ReturnNull()
    {
        Recall.UI.Components.FolderGrantMessages.OverlapKey(ScopeGrantAddStatus.Added).Should().BeNull();
        Recall.UI.Components.FolderGrantMessages.OverlapKey(ScopeGrantAddStatus.Duplicate).Should().BeNull();
    }

    [Fact]
    public void OverlapWarning_BothLanguages_SayParentIsKept()
    {
        var root = FindRepoRoot();
        var en = ReadResx(Path.Combine(root, "src", "Recall.UI", "Resources", "Strings.resx"));
        var ar = ReadResx(Path.Combine(root, "src", "Recall.UI", "Resources", "Strings.ar.resx"));
        en["Folders_OverlapWarning"].Should().Be("Recall keeps the parent folder. The folder inside it is not indexed again.");
        ar["Folders_OverlapWarning"].Should().Be("يبقي ريكول المجلد الأب. لا يُفهرس المجلد الذي بداخله مرة أخرى.");
        en["Home_IndexNow"].Should().Be("Index now");
        ar["Home_IndexNow"].Should().Be("فهرس الآن");
        en["Home_IndexNowLead"].Should().Be("This folder is allowed and not indexed yet.");
        ar["Home_IndexNowLead"].Should().Be("هذا المجلد مسموح ولم يُفهرس بعد.");
    }

    [Fact]
    public void ShouldShow_NullOrEmpty_IsFalse()
    {
        Recall.UI.Pages.IndexNowPanel.ShouldShow(null).Should().BeFalse();
        Recall.UI.Pages.IndexNowPanel.ShouldShow([]).Should().BeFalse();
    }

    [Fact]
    public void ShouldShow_OneOffer_IsTrue()
    {
        var offers = new List<ScopeFolderGrant> { new(@"D:\Photos", "Photos", GrantFileKinds.All) };
        Recall.UI.Pages.IndexNowPanel.ShouldShow(offers).Should().BeTrue();
    }

    [Fact]
    public void HomeRazor_IndexNow_UsesBothLanguagesAndLtrPath()
    {
        var razor = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Recall.UI", "Pages", "Find", "Home.razor"));
        razor.Should().Contain("Home_IndexNow");
        razor.Should().Contain("Home_IndexNowLead");
        razor.Should().Contain("bdi class=\"ltr\"");
    }

    [Fact]
    public void ListOffers_Empty_IsEmpty()
    {
        IndexNowOfferPolicy.ListOffers([], new HashSet<string>()).Should().BeEmpty();
    }

    [Fact]
    public void ListOffers_UnfinishedRoot_IsOffered()
    {
        var grants = new List<ScopeFolderGrant> { new(@"D:\Photos", "Photos", GrantFileKinds.All) };
        var offers = IndexNowOfferPolicy.ListOffers(grants, new HashSet<string>());
        offers.Should().HaveCount(1);
    }

    [Fact]
    public void ListOffers_FinishedRoot_IsHidden()
    {
        var grants = new List<ScopeFolderGrant> { new(@"D:\Photos", "Photos", GrantFileKinds.All) };
        var finished = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ScopeGrantPolicy.Normalize(@"D:\Photos") };
        IndexNowOfferPolicy.ListOffers(grants, finished).Should().BeEmpty();
    }

    [Fact]
    public void ListOffers_ChildOfAllowedParent_IsHidden()
    {
        var grants = new List<ScopeFolderGrant>
        {
            new(@"D:\", "drive", GrantFileKinds.All),
            new(@"D:\Photos", "photos", GrantFileKinds.All),
        };
        var offers = IndexNowOfferPolicy.ListOffers(grants, new HashSet<string>());
        offers.Should().HaveCount(1);
        offers[0].Path.Should().Be(ScopeGrantPolicy.Normalize(@"D:\"));
    }

    [Fact]
    public void ListOffers_ChildWithoutParentInList_IsOffered()
    {
        var grants = new List<ScopeFolderGrant> { new(@"D:\Photos", "photos", GrantFileKinds.All) };
        var offers = IndexNowOfferPolicy.ListOffers(grants, new HashSet<string>());
        offers.Should().HaveCount(1);
    }

    [Fact]
    public async Task IndexAsync_FinishesGrant_AndOfferDisappears()
    {
        var store = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(store);
        scope.TryAddGrant(new ScopeFolderGrant(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), "temp", GrantFileKinds.All));
        var grantPath = store.List()[0].Path;
        var cursors = new FakeIngestCursorStore();
        var reader = new EmptyReader();
        var index = new FakeTextVectorIndex();
        var (engine, opLog, _) = IngestTestKit.CreateEngineParts(new FakeTextEmbeddingEncoder(), index, null);
        var ingest = IngestTestKit.CreateGrantedService(scope, reader, engine, cursors, index, opLog);
        var service = new IndexNowService(scope, cursors, ingest);
        service.ListOffers().Should().HaveCount(1);
        await service.IndexAsync(grantPath, TestContext.Current.CancellationToken);
        service.ListOffers().Should().BeEmpty();
    }

    [Fact]
    public async Task IndexAsync_FinishedGrant_CallsIngest()
    {
        var store = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(store);
        scope.TryAddGrant(new ScopeFolderGrant(@"D:\Known", "known", GrantFileKinds.All));
        var cursors = new FakeIngestCursorStore();
        cursors.MarkGrantFinished(@"D:\Known");
        var counting = new FakeGrantedTextIngestService();
        var service = new IndexNowService(scope, cursors, counting);
        await service.IndexAsync(@"D:\Known", TestContext.Current.CancellationToken);
        counting.IngestGrantCalls.Should().Be(1);
    }

    [Fact]
    public void AddRecallCore_CanBeCalledWithoutMaui()
    {
        var services = new ServiceCollection();
        services.AddRecallCore();
        services.Should().Contain(s => s.ServiceType == typeof(IIndexNowService));
    }

    private sealed class EmptyReader : IGrantedTextReader
    {
        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private static Dictionary<string, string> ReadResx(string path)
    {
        var document = XDocument.Load(path);
        return document.Root!
            .Elements("data")
            .ToDictionary(
                element => element.Attribute("name")!.Value,
                element => element.Element("value")!.Value,
                StringComparer.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Recall.slnx was not found above the test output directory.");
    }
}
