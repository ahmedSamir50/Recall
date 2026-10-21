## tests/Recall.UnitTests/ArabicDigitShapingTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class ArabicDigitShapingTests
    public void ShapeDigits_KeepsLatinDigits()
```

No branch.


## tests/Recall.UnitTests/DeviceRecordServiceTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class DeviceRecordServiceTests
    public void ChooseId_AcceptsNonEmptyCandidate()
    public void ChooseId_RejectsKnownBadAndroidId()
    public void ChooseId_RejectsWhitespaceCandidate()
    public void EnsureLoaded_DoesNotSaveWhenCandidateNotReady()
    public void EnsureLoaded_KeepsFirstStoredId_WhenLaterCandidateDiffers()
    public void TrySetDisplayName_LeavesDeviceIdAndOriginalOsNameUnchanged()
    public void TrySetDisplayName_Empty_ReturnsEmpty()
    public void TrySetDisplayName_EqualName_ReturnsUnchanged()
    public void EnsureLoaded_StoresEmptyNativeName()
    public void GetInitials_CoversGraphemeCases(string name, string expectedInitials)
        public DeviceRecordState? Load() => State;
        public void Save(DeviceRecordState state) => State = state;
        public string NewDeviceId()
```

No branch.


## tests/Recall.UnitTests/LocaleServiceTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class LocaleServiceTests
    public void LangCode_Arabic_IsAr()
    public void LangCode_English_IsEn()
    public void ShapeDigits_English_LeavesLatinDigits()
    public void ApplyCulture_Arabic_SetsCurrentCulture()
```

No branch.


## tests/Recall.UnitTests/LocalFileOpenPolicyTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class LocalFileOpenPolicyTests
    public void Classify_Whitespace_IsEmpty() =>
    public void Classify_ContentScheme_IsContentUri() =>
    public void Classify_ContentScheme_IgnoresCase() =>
    public void Classify_DiskPath_IsFilesystem() =>
    public void Decide_Thrown_IsFailed() =>
    public void Decide_Empty_IsFailed() =>
    public void Decide_ContentAccepted_IsOpened() =>
    public void Decide_ContentRejected_IsFailed() =>
    public void Decide_MissingFile_IsMissing() =>
    public void Decide_LauncherOpened_IsOpened() =>
    public void Decide_LauncherRejected_IsFailed() =>
    public void Exists_Empty_IsFalse() =>
    public void Exists_ContentStream_IsTrue() =>
    public void Exists_ContentStream_IsFalse() =>
    public void Exists_Filesystem_MatchesDisk()
```

No branch.


## tests/Recall.UnitTests/ModelCatalogTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class ModelCatalogTests
    public void All_ContainsTextVisionAndAudio_WithoutLanguageOrSpeechRows()
```

No branch.


## tests/Recall.UnitTests/Phase2CloseTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class Phase2CloseTests
    public void MarkVerifying_SetsPhaseAndClearsFault()
    public void PlanChunks_UnderBudget_StaysOnePiece()
    public void PlanChunks_BlankContent_Throws()
    public void PlanChunks_NullCounter_Throws()
    public void MauiProject_ContainsNoOnnxWeights()
    public void HomeRazor_OmitsDemoCards()
    public void AppCss_DefinesFillHeight()
    public void AppCss_BottomSafeUsesPublishedInset()
    public void Solution_DoesNotReferenceMudBlazor()
    public void ThemeChoiceClass_SystemSelected_IsPrimary() =>
    public void ThemeChoiceClass_SystemSelected_DarkIsGhost() =>
    public void ThemeChoiceClass_DarkSelected_IsPrimary() =>
    public void ThemeChoiceClass_DarkSelected_LightIsGhost() =>
    public void ThemeChoiceClass_LightSelected_IsPrimary() =>
    public void ThemeChoiceClass_LightSelected_SystemIsGhost() =>
            if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
        throw new InvalidOperationException("Recall.slnx was not found above the test output directory.");
```


## tests/Recall.UnitTests/ResourceKeyParityTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class ResourceKeyParityTests
    public void StringsResx_ArabicAndEnglish_HaveTheSameKeys()
            if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
        throw new InvalidOperationException("Recall.slnx was not found.");
```


## tests/Recall.UnitTests/ReviewFixTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class ReviewFixTests
    public void DownloadTransfer_PlacesBody(int status, long existing, DownloadBodyPlacement placement)
    public void DownloadTransfer_PartialContentLengthIsRemainder()
    public void DownloadTransfer_ShortReadIsNotComplete_UnknownLengthEofIsComplete()
    public void EmptyTitle_DoesNotEmbedTheWordNone()
    public void LongNote_EachChunkKeepsTitlePrefix_AndDoesNotSplitSurrogate()
    public void MeanPool_AveragesTwoIdenticalRows()
    public void IsInsideGrant_DoesNotMatchSiblingPrefix()
    public void HybridFusion_Requires60()
    public void AgeCutoff_AndEmptyGrantKinds_UseFaultCodes()
        public Recall.Core.Vocabulary.AgeCutoffYears Get() => Recall.Core.Vocabulary.AgeCutoffYears.Five;
        public void Set(Recall.Core.Vocabulary.AgeCutoffYears years)
        public IReadOnlyList<ScopeFolderGrant> List() => stored;
        public void Save(IReadOnlyList<ScopeFolderGrant> grants)
            if (char.IsHighSurrogate(text[i]) && (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1])))
            if (char.IsLowSurrogate(text[i]) && (i == 0 || !char.IsHighSurrogate(text[i - 1])))
```


## tests/Recall.UnitTests/ScopeGrantServiceTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class ScopeGrantServiceTests
    public void CanContinue_NoFolders_ReturnsFalse()
    public void CanContinue_OneFolder_ReturnsTrue()
    public void TryAddGrant_ChildOfGrantedParent_ReturnsOverlap()
    public void RemoveGrant_LastFolder_DisablesContinue()
    public async Task FolderPicker_Cancelled_ReturnsCancelledOutcome()
    public async Task FolderPicker_Selected_ReturnsPath()
    public async Task FolderPicker_Denied_ReturnsDeniedOutcome()
```

No branch.


## tests/Recall.UnitTests/ShellUiStoreTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class ShellUiStoreTests
    public void SetThemePreference_WhenCalled_UpdatesSnapshot()
    public void TryAddGrant_WhenFolderAdded_ReflectsInSnapshot()
    public void Dispose_WhenCalled_DoesNotThrow()
    public void SetRoute_WhenPathChanges_UpdatesSnapshotRoute()
```

No branch.


## tests/Recall.UnitTests/SolutionShapeTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class SolutionShapeTests
    public void RequiredPorts_WhenCoreAssemblyIsLoaded_ExposesTheSevenPorts()
    public void ForbiddenCapturePorts_WhenSourceIsRead_DoNotExist()
    public void EachPort_HasAFakeInUnitTests()
    public void SyncFeature_WhenDefault_IsOff()
    public void Modality_Members_AreTextImageVideoAudio()
    public void StoredWidth_Members_Are768_512_256()
    public void AgeCutoff_Members_Are2_5_7()
    public void OpLogOperation_Members_AreInsertDeletePinUnpin()
    public void BannedSymbols_ListsHttpClientSocketTcpListenerUdpClient()
    public void MauiTfms_IncludeTheFourShells_AndNotWinArm64()
    public void CoreCsproj_DoesNotReferenceMauiOnnxOrZVec()
    public void Solution_DoesNotReferenceSpeechOrLanguageModel()
    public void Shell_HasNoHardcodedUserVisibleSentence()
    public void Manifest_HasNoCapturePermission()
    public void RecallSlnx_WhenRead_ContainsRequiredProjects()
    public void RepoRoot_UsesSlnxOnly_NotLegacySln()
    public void RecallUi_WhenScanned_DoesNotInjectThemeLocaleOrScopeServices()
    public void RazorMarkup_WhenScanned_HasNoCodeBlocks()
    public void AddRecallCore_CanBeCalledWithoutMaui()
    public void HttpClient_IsAbsentFromCoreAndUiSource()
            if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
        throw new InvalidOperationException("Recall.slnx was not found above the test output directory.");
```


## tests/Recall.UnitTests/TextFindSessionTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class TextFindSessionTests
    public void ShouldQuery_Blank_IsFalse() =>
    public void ShouldQuery_Sentence_IsTrue() =>
    public void FaultKey_Null_IsNull() =>
    public void FaultKey_Cancelled_IsSearchFault() =>
    public void FaultKey_Other_IsSearchFault() =>
    public void Build_Empty_HasNoCards()
    public void Build_SixHits_FirstFiveAreImmediate()
    public void Build_MissingFile_StaysUnavailable()
    public void Build_LocalTime_UsesInvariantDigits()
    public void MarkUnavailable_OnlyMissing()
    public void OpenFaultKey_OnlyFailed()
```

No branch.


## tests/Recall.UnitTests/TextGuardTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class TextGuardTests
    public void DefaultExtensions_DoNotIncludeLog()
    public void Signature_RejectsExecutableAndScript_AcceptsNote()
    public void OpLog_WritesOneFileForTheUtcDay()
```

No branch.


## tests/Recall.UnitTests/TextModelDownloadUiPolicyTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class TextModelDownloadUiPolicyTests
    public void ShouldAutoStartDownload_OnlyWhenNotStarted(TextModelDownloadPhase phase, bool expected) =>
    public void ShouldOfferRetry_ForRecoverableFailures(TextModelDownloadPhase phase, bool expected) =>
    public void ShouldPublishProgress_OnlyWhenTheShownPercentChanges(int last, int next, bool expected) =>
    public void ComputePercent_UsesFloorAndRejectsEmptyOrNegativeTotals()
    public void ComputeOverallPercent_DoesNotResetWhenTheNextFileStarts()
```

No branch.


## tests/Recall.UnitTests/TextPipelineTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class TextPipelineTests
    public void WrapSearchQuery_SecondWrap_Throws()
    public void WrapTitledDocument_FormatsOnce()
    public void ContentId_ChangesWhenChunkOrdinalChanges()
    public void ContentId_DoesNotChangeWhenDeviceFieldsDiffer()
    public void IndexPolicy_Text512_Accepts_128Throws()
    public void Matryoshka_AfterCut_LengthIsOne()
    public void DownloadRequestBuilder_OmitsForbiddenPayload()
```

No branch.


## tests/Recall.UnitTests/ThemeServiceTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class ThemeServiceTests
    public void GetEffectiveTheme_SystemAndOsDark_ReturnsDark()
    public void GetEffectiveTheme_SystemAndOsLight_ReturnsLight()
    public void GetEffectiveTheme_ForcedDark_ReturnsDark()
    public void GetEffectiveTheme_ForcedLight_ReturnsLight()
    public void ParseStored_UnknownValue_FallsBackToSystem()
    public void SetPreference_RoundTripsThroughFakeStore()
```

No branch.


## tests/Recall.UnitTests/UiCultureTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class UiCultureTests
    public void Lang_Arabic_IsAr()
    public void Lang_Other_IsEn()
    public void Dir_Arabic_IsRtl()
    public void Dir_English_IsLtr()
```

No branch.


## tests/Recall.UnitTests/Fakes/DeviceFakes.cs

```csharp
namespace Recall.UnitTests.Fakes;
public sealed class FakeDeviceIdCandidateProvider : IDeviceIdCandidateProvider
    public DeviceIdCandidate GetCandidate() => Candidate;
```

No branch.


## tests/Recall.UnitTests/Fakes/PortFakes.cs

```csharp
namespace Recall.UnitTests.Fakes;
public sealed class FakeFileScopeWatcherService : IFileScopeWatcherService
public sealed class FakeScopeManagerService : IScopeManagerService
public sealed class FakeVideoFrameSamplerService : IVideoFrameSamplerService
public sealed class FakeSecureStorageService : ISecureStorageService
public sealed class FakeIdleDetectorService : IIdleDetectorService
public sealed class FakeOnboardingCompletionStore : IOnboardingCompletionStore
public sealed class FakeDebugAppReset : IDebugAppReset
public sealed class FakeModelDownloadService : IModelDownloadService
public sealed class FakeIndexPolicy : IIndexPolicy
public sealed class FakeTextTokenCounter : ITextTokenCounter
public sealed class FakeTextEmbeddingEncoder : ITextEmbeddingEncoder
public sealed class FakeGrantedTextIngestService : IGrantedTextIngestService
public type FakeEncoderFileHash
FakeEncoderFileHash
public sealed class FakeEncoderFileHash : IEncoderFileHash
public type IngestTestKit
IngestTestKit
public static class IngestTestKit
public type FakeLocalContentProbe
FakeLocalContentProbe
public sealed class FakeLocalContentProbe : ILocalContentProbe
public sealed class FakeTextSearchService : ITextSearchService
public sealed class FakeVisionEmbeddingEncoder : IVisionEmbeddingEncoder
public sealed class FakeMediaVectorIndex : IMediaVectorIndex
public sealed class FakePictureSearchService : IPictureSearchService
if (VisionSnapshot.Phase == TextModelDownloadPhase.NotStarted)
if (OnEmbed is not null)
if (Inserts.Any(i => string.Equals(i.ContentId, insert.ContentId, StringComparison.Ordinal)))
public sealed class FakeDeviceIdCandidateProviderPort : IDeviceIdCandidateProvider
public sealed class FakeRandomDeviceIdSourcePort : IRandomDeviceIdSource
if (!AllowWrite)
if (string.IsNullOrWhiteSpace(contentId) || string.IsNullOrWhiteSpace(path))
if (string.IsNullOrWhiteSpace(path))
if (string.Equals(alias.Path, path, StringComparison.OrdinalIgnoreCase))
if (string.Equals(chunk.Path, path, StringComparison.OrdinalIgnoreCase) && !chunk.Retired && !newIds.Contains(id))
if (string.Equals(alias.Path, path, StringComparison.OrdinalIgnoreCase) && !alias.Retired && !newIds.Contains(alias.ContentId))
if (chunkRows.TryGetValue(chunk.ContentId, out var existing)
else if (!chunkRows.ContainsKey(chunk.ContentId))
if (chunkRows.ContainsKey(chunk.ContentId))
if (removed)
public sealed class FakeTextVectorIndex : ITextVectorIndex
public sealed class FakeSyncAdapter : ISyncAdapter
public sealed class FakeThemePreferenceStorePort : IThemePreferenceStore
public sealed class FakeUiLocaleStorePort : IUiLocaleStore
public sealed class FakeSystemThemeProviderPort : ISystemThemeProvider
public sealed class FakeScopeGrantStorePort : IScopeGrantStore
public sealed class FakeFolderPickerServicePort : IFolderPickerService
public sealed class FakeDeviceRecordService : IDeviceRecordService
public sealed class FakeDeviceRecordStore : IDeviceRecordStore
public sealed class FakeDeviceOsNameProvider : IDeviceOsNameProvider
public sealed class FakeAgeCutoffPreferenceStore : IAgeCutoffPreferenceStore
public sealed class FakeRecallShellKind : IRecallShellKind
public sealed class FakeGrantedFolderAccessService : IGrantedFolderAccessService
public sealed class FakeUserSkipStore : IUserSkipStore
    public FakeScopeManagerService()
    public IReadOnlyList<Recall.Core.Scope.ScopeFolderGrant> ListGrants() => inner.ListGrants();
    public Recall.Core.Scope.ScopeGrantAddResult TryAddGrant(Recall.Core.Scope.ScopeFolderGrant grant) =>
    public void RemoveGrant(string path) => inner.RemoveGrant(path);
    public void SetGrantFileKinds(string path, GrantFileKinds kinds) => inner.SetGrantFileKinds(path, kinds);
    public bool CanContinue() => inner.CanContinue();
    public bool IsComplete() => Complete;
    public void MarkComplete() => Complete = true;
    public void ResetToOnboarding() => Complete = false;
    public Task ResetAllLocalDataAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public TextModelDownloadSnapshot GetTextDownloadSnapshot() => Snapshot;
    public Task DownloadTextModelAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default)
    public void CancelTextDownload()
    public Task RetryTextDownloadAsync(IProgress<ModelDownloadProgress>? progress, CancellationToken cancellationToken = default) =>
    public void EnsureStorableWidth(Modality modality, int width)
    public int CountTokens(string text) => Math.Max(1, text.Length / 4);
    public ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
    public Task IngestGrantedTextAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IReadOnlyList<TextSearchHit>> FindAsync(string query, IReadOnlyList<ScopeFolderGrant> grants, CancellationToken cancellationToken = default) =>
    public DeviceIdCandidate GetCandidate() => new("fake", true);
    public string NewDeviceId() => "random";
    public bool TryInsert(TextIndexInsert insert)
    public IReadOnlyList<TextSearchHit> Search(TextSearchRequest request, CancellationToken cancellationToken = default) => [];
    public Recall.Core.Preferences.ThemePreference Get() => Recall.Core.Preferences.ThemePreference.System;
    public void Set(Recall.Core.Preferences.ThemePreference preference)
    public Recall.Core.Preferences.UiLocale Get() => Recall.Core.Preferences.UiLocale.En;
    public void Set(Recall.Core.Preferences.UiLocale locale)
    public bool IsDark() => true;
    public IReadOnlyList<Recall.Core.Scope.ScopeFolderGrant> List() => [];
    public void Save(IReadOnlyList<Recall.Core.Scope.ScopeFolderGrant> grants)
    public Task<FolderPickerResult> PickFolderAsync(CancellationToken cancellationToken = default) =>
    public string GetDeviceId() => "fake-device-id";
    public string GetOriginalOsName() => "Recall device";
    public string GetDisplayName() => "Recall device";
    public DisplayNameSetResult TrySetDisplayName(string displayName) => DisplayNameSetResult.Saved;
    public string GetInitials() => "RD";
    public Recall.Core.Device.DeviceRecordState? Load() => null;
    public void Save(Recall.Core.Device.DeviceRecordState state)
    public string GetOsDeviceName() => OsName;
    public Recall.Core.Vocabulary.AgeCutoffYears Get() => Value;
    public void Set(Recall.Core.Vocabulary.AgeCutoffYears years) => Value = years;
    public void RestorePersistedAccess() => RestoreCount++;
        if (Snapshot.Phase == TextModelDownloadPhase.NotStarted)
        if (Inserts.Any(i => i.ContentId == insert.ContentId))
```



```csharp
FakeScopeChangeSink
FakeIngestCursorStore
if (chunks is not null)
if (this.chunks.ContainsKey(chunk.ContentId))
if (!files.TryGetValue(previousPath, out var row))
if (string.Equals(chunk.Path, previousPath, StringComparison.OrdinalIgnoreCase) && !chunk.Retired)
if (files.TryGetValue(path, out var row))
if (string.Equals(chunk.Path, path, StringComparison.OrdinalIgnoreCase))
if (grants.TryGetValue(grantPath, out var state))
``` 

```csharp
if (this.chunks.ContainsKey(chunk.ContentId) && !liveIds.Contains(chunk.ContentId))
if (!newIds.Add(chunk.ContentId))
``` 

```csharp
if (!seen.Add(chunk.ContentId))
if (this.chunks.TryGetValue(chunk.ContentId, out var existing)
if (chunks is null)
if (string.Equals(row.Path, path, StringComparison.OrdinalIgnoreCase) && !row.Retired && !wanted.Contains(id))
``` 

```csharp
if (string.Equals(this.chunks[id].Path, path, StringComparison.OrdinalIgnoreCase))
``` 
## tests/Recall.UnitTests/Fakes/PreferenceFakes.cs

```csharp
namespace Recall.UnitTests.Fakes;
public sealed class FakeThemePreferenceStore : IThemePreferenceStore
public sealed class FakeUiLocaleStore : IUiLocaleStore
public sealed class FakeSystemThemeProvider : ISystemThemeProvider
public sealed class FakeScopeGrantStore : IScopeGrantStore
public sealed class FakeFolderPickerService : IFolderPickerService
    public ThemePreference Get() => Value;
    public void Set(ThemePreference preference) => Value = preference;
    public UiLocale Get() => Value;
    public void Set(UiLocale locale) => Value = locale;
    public bool IsDark() => OsIsDark;
    public IReadOnlyList<Recall.Core.Scope.ScopeFolderGrant> List() => grants;
    public void Save(IReadOnlyList<Recall.Core.Scope.ScopeFolderGrant> grants) =>
    public Task<FolderPickerResult> PickFolderAsync(CancellationToken cancellationToken = default) =>
```

No branch.


## tests/Recall.IntegrationTests/LlamaEmbedIntegrationTests.cs

```csharp
namespace Recall.IntegrationTests;
public sealed class LlamaEmbedIntegrationTests
    public async Task Embed_LocalGguf_Is768AndUnit()
        catch (InvalidOperationException ex)
```

No branch.


## tests/Recall.IntegrationTests/TextSearchIntegrationTests.cs

```csharp
namespace Recall.IntegrationTests;
public sealed class TextSearchIntegrationTests : IDisposable
    public void QuantizeTypeUndefined_ProducesFloatIndex()
    public void Search_DropsPathOutsideGrant()
    public void RrfRankConstant_Is60()
    public void IndexPolicy_Rejects128WideStore()
    public async Task HybridQuery_ReturnsGrantedHit_FromPinnedNote()
    public void Dispose()
        if (Directory.Exists(root))
```


## tests/Recall.IntegrationTests/Infrastructure/InMemoryScopeGrantStore.cs

```csharp
namespace Recall.IntegrationTests.Infrastructure;
    public IReadOnlyList<ScopeFolderGrant> List() => grants;
    public void Save(IReadOnlyList<ScopeFolderGrant> grants) => this.grants = grants.ToList();
```

No public type.

No branch.


## tests/Recall.IntegrationTests/Infrastructure/IntegrationTestServiceCollection.cs

```csharp
namespace Recall.IntegrationTests.Infrastructure;
public static class IntegrationTestServiceCollection
    public static ServiceProvider Build(string modelDirectory, string vectorRoot)
```

No branch.


## tests/Recall.IntegrationTests/Infrastructure/IntegrationTextModelPaths.cs

```csharp
namespace Recall.IntegrationTests.Infrastructure;
public static class IntegrationTextModelPaths
    public static string ResolveModelDirectory()
    public static bool IsReady(string directory) =>
        if (!string.IsNullOrWhiteSpace(env) && IsReady(env))
            if (IsReady(dir))
        throw new InvalidOperationException(
            if (File.Exists(Path.Combine(dir.FullName, "Recall.slnx")))
        throw new InvalidOperationException("Recall.slnx not found.");
```


## tests/Recall.BenchTests/TextBenchmarkHostTests.cs

```csharp
namespace Recall.BenchTests;
public sealed class TextBenchmarkHostTests
    public void Label_NotAndroid_IsDesktop()
    public void Label_AndroidMissingHost_IsEmulator()
    public void Label_AndroidBlankHost_IsEmulator()
    public void Label_AndroidPhoneHost_IsPhone()
    public void Label_AndroidPhoneHostMixedCase_IsPhone()
    public void DesktopIdenticalVectors_AreNotThePhoneGate()
    public void PhoneHybridAndLoad_MeetTheExit()
    public void LabelThisProcess_MatchesOperatingSystem()
```

No branch.


## tests/Recall.UnitTests/CodeAndSkipTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class CodeAndSkipTests
    if (Directory.Exists(root))
    catch (IOException)
    if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
```



## tests/Recall.UnitTests/Phase3CursorTests.cs

```csharp
namespace Recall.UnitTests;
public type Phase3CursorTests
Phase3CursorTests
public sealed class Phase3CursorTests
public void Decide_NullReadingMissingAndChangedHash_Read()
public void Decide_FinishedSameHash_Skip()
public void Decide_BlankHash_Throws()
public void MarkReading_Committed_SurvivesNewStore()
public void MarkFinished_DuplicateChunkId_RollsBackToReading()
public void MarkFinished_ThenDecide_SkipsSameHash()
public void MarkFinished_SameContentId_ReplacesLiveChunk()
public void RoundTrip_QuoteInPath_UsesParameter()
public void RenamePath_MovesFileAndLiveChunks()
public void MarkMissing_KeepsChunkRowAndSetsMissing()
public void RetirePath_SetsRetired()
public void IsGrantFinished_OnlyWhenFinished()
public void CheckpointSql_UsesParametersAndFileName()
public int Calls { get; private set; }
public List<GrantedTextDocument> Documents { get; } = [];
public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
public async Task IngestGrant_UnknownPath_DoesNotRead()
public async Task IngestGrant_TextAndCodeOff_DoesNotRead()
public async Task IngestDocument_FinishedSameHash_DoesNotEmbed()
public async Task IngestDocument_Reading_Embeds()
public async Task IngestDocument_ThrowAfterMarkReading_StaysReading()
public async Task Resume_ReadingRow_EmbedsAgain_FinishedRow_DoesNotEmbed()
public ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
public int Calls { get; private set; }
public bool FailNext { get; set; } = true;
public ValueTask<ReadOnlyMemory<float>> EmbedPrefixedTextAsync(string prefixedText, CancellationToken cancellationToken = default)
public async Task Apply_Deleted_AppendsDeleteAndDoesNotEmbed()
public async Task Delete_OnePath_SharedId_DoesNotAppendDelete()
public async Task Delete_LastPath_AppendsDeleteOnce()
public async Task Apply_DeletedWhenAlreadyMissing_DoesNotAppendAgain()
public async Task Apply_DeletedWithNoChunks_MarksMissingOnly()
public async Task Apply_RenamedSameHash_DoesNotEmbed()
public async Task Apply_RenamedChangedHash_RetiresAndEmbedsOnce()
public async Task Apply_RenamedUnknownPrevious_IngestsNewPath()
public async Task Apply_RenamedMissingDocument_MarksPreviousMissing()
public async Task Apply_CreatedNullDocument_DoesNotWrite()
public async Task Apply_BlankPath_DoesNotWrite()
public async Task Apply_Cancelled_Throws()
public async Task TryRead_MissingBlockedIgnoredOrUnsafe_ReturnsNull()
public async Task TryRead_AdmittedText_ReturnsDocument()
public void Apply_RetiredChunk_DropsHit()
public void Apply_MissingChunk_SetsSourceMissing()
public void Apply_ChunkPath_ReplacesStoredPath()
public void Apply_PathOutsideGrant_DropsHit()
public void Apply_NoChunk_KeepsHit()
public void Build_SourceMissing_IsUnavailableEvenWhenFileExists()
public void Build_MissingFile_IsUnavailable()
public void Build_PresentFile_IsAvailable()
public void AppendDelete_Blank_Throws()
public void AppendDelete_Memory_IsDeleteAndUnsent()
public void AppendDelete_NoDirectory_WritesNoFile()
public void AppendDelete_Directory_WritesDeleteLine()
    catch (IOException ex)
    if (FailNext)
    if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
``` 



## tests/Recall.UnitTests/Phase3GrantTests.cs

```csharp
namespace Recall.UnitTests;
public type Phase3GrantTests
Phase3GrantTests
public sealed class Phase3GrantTests
public void TryAddGrant_NullGrant_Throws()
public void TryAddGrant_BlankPath_ThrowsAndDoesNotSave()
public void TryAddGrant_ChildOfGrantedParent_ReturnsOverlap()
public void TryAddGrant_ParentTextOnly_KeepsText()
public void TryAddGrant_ParentReplacesChild_KeepsDriveRoot()
public void TryAddGrant_ParentReplacesTwoChildren_KeepsDriveRoot()
public void TryAddGrant_PhotosExtra_IsNotAChild()
public void TryAddGrant_Duplicate_DoesNotSave()
public void IsContent_Blank_IsFalse()
public void IsContent_ContentScheme_IsTrue()
public void IsContent_DrivePath_IsFalse()
public void Choose_WindowsAndroidMacIosUnknown_MapToWatcherKind()
public void OverlapKey_BothOverlapStatuses_ReturnWarning()
public void OverlapKey_AddedAndDuplicate_ReturnNull()
public void OverlapWarning_BothLanguages_SayParentIsKept()
public void ShouldShow_NullOrEmpty_IsFalse()
public void ShouldShow_OneOffer_IsTrue()
public void HomeRazor_IndexNow_UsesBothLanguagesAndLtrPath()
public void ListOffers_Empty_IsEmpty()
public void ListOffers_UnfinishedRoot_IsOffered()
public void ListOffers_FinishedRoot_IsHidden()
public void ListOffers_ChildOfAllowedParent_IsHidden()
public void ListOffers_ChildWithoutParentInList_IsOffered()
public async Task IndexAsync_FinishesGrant_AndOfferDisappears()
public async Task IndexAsync_FinishedGrant_CallsIngest()
public void AddRecallCore_CanBeCalledWithoutMaui()
public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
``` 



## tests/Recall.UnitTests/Phase3WatchTests.cs

```csharp
namespace Recall.UnitTests;
public type Phase3WatchTests
Phase3WatchTests
public sealed class Phase3WatchTests
public void TryAdd_BlankPath_IsIgnored()
public void TryAdd_SamePath_ReplacesWithoutGrowing()
public void TryAdd_SixtyFifthPath_OverflowsAndRequestsRescan()
public void Drain_ClearsItems()
public void AcknowledgeRescan_ClearsFlag()
public void TryMap_BlankOrError_ReturnsFalse()
public void TryMap_RenamedWithPrevious_ReturnsRenamed()
public void TryMap_RenamedWithoutPrevious_ReturnsCreated()
public void TryMap_DeletedChangedCreated_ReturnThoseKinds()
public void Select_NullOrEmpty_ReturnsEmpty()
public void Select_Blank_IsDropped()
public void Select_Duplicate_KeepsOne()
public void Select_ChildOfParent_DropsChild()
public void Select_ThousandChildren_StaysOneRoot()
public void Select_TwoSiblingRoots_ReturnsBoth()
public void Choose_WindowsMacAndroid_UseFilesystemWatcher()
public void CanWatchDirectories_IosPhone_IsFalse()
public void CanWatchDirectories_MacCatalyst_IsTrue()
public void CanWatchDirectories_Windows_IsTrue()
public void Diff_AddedWhileClosed_EmitsCreated()
public void Diff_SameHash_EmitsNothing()
public void Diff_DifferentHash_EmitsModified()
public void Diff_MissingStored_EmitsDeleted()
public void Diff_AlreadyMissing_EmitsNothing()
public void Diff_UniqueHashPair_EmitsRenamed()
public void Diff_RepeatedHash_DoesNotPair()
public void Diff_BlankPath_IsIgnored()
public void DisabledScopeWatcher_Start_DoesNotWatch()
public void Stop_LeavesWatchingFalse()
public async Task WindowsFileScopeWatcher_TempFolder_ReportsCreatedModifiedRenamedDeleted()
public void Stop_DisposesAndClearsWatching()
public void OnError_RequestsRescan()
public void OnCreated_Ios_StartsLoopWithoutWatcher()
public void OnCreated_Unknown_DoesNotStart()
public void OnCreated_Windows_StartsWatcher()
public void RequestRescan_BeforeLoop_SetsFlag()
public void OnResumed_Ios_RequestsRescan()
public void OnResumed_Windows_RequestsRescan()
public void OnResumed_Mac_RequestsRescan()
public void WindowsFileScopeWatcher_NestedGrants_OpensOneWatcher()
public void WindowsFileScopeWatcher_ContentUri_OpensZeroAndRescans()
public async Task PumpOnce_WaitsUntilReport()
public void TryAdd_ConcurrentWithDrain_DoesNotThrow()
public async Task Idle_Coordinator_BusyKeepsChange_IdleDrains()
public async Task PumpOnce_ExceptionOnFirstChange_AppliesSecond()
public async Task PumpOnce_Cancel_Stops()
public void Stop_StopsWatcher()
public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
public List<string> Succeeded { get; } = [];
public Task IngestGrantedTextAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
public Task IngestGrantAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;
public Task IngestDocumentAsync(ScopeFolderGrant grant, GrantedTextDocument document, CancellationToken cancellationToken = default)
    if (kinds.Contains(ScopeChangeKind.Created)
    catch (IOException ex)
    if (document.FileName.Contains("first", StringComparison.OrdinalIgnoreCase))
```


## tests/Recall.IntegrationTests/Infrastructure/TestEncoderFileHash.cs

```csharp
namespace Recall.IntegrationTests.Infrastructure;
No public type.
No branch.
``` 

## tests/Recall.UnitTests/LlamaEmbedTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class LlamaEmbedTests
    public void Embed_Blank_Throws()
    public Task Embed_Cancelled_Throws()
    public Task Embed_NativeCode_Throws()
    public Task Embed_Success_IsUnit768()
    public void Count_Blank_Throws()
    public void Count_ReturnsValue()
    public void RequireCount_Code_Throws()
    public void RequireCount_Zero_ReturnsCount()
    public void Ensure_MissingFile_Throws()
    public void Ensure_OpenFails_Throws()
    public void Ensure_SecondCall_OpensOnce()
    public void Count_Cancelled_Throws()
    public Task LazyEncoder_Forwards()
    public Task Dispose_ClosesOnce()
    public void Defaults_AreGguF()
    public void AppSettings_TextModel_IsGguF()
            if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
```

No branch.


## tests/Recall.UnitTests/PhaseAddendumTests.cs

```csharp
namespace Recall.UnitTests;
public type PhaseAddendumTests
PhaseAddendumTests
public void Fold_Yeh_StaysYeh()
public void Fold_Null_Throws()
public void Tokens_LowercasesNonHan()
public void Fold_AlefAndTatweel_Match()
public void Fold_HanCharacter_IsOneToken()
public void Card_ShowsRawNote()
public void NumericSpan_DigitsOnly_Bm25Only()
public void NumericSpan_CodeFile_StaysDense()
public void DatasetFolder_32Hex_Skips()
public void DatasetFolder_GrantRoot_NotSkipped()
public void DatasetFolder_Below300_NotSkipped()
public void DatasetFolder_Uuid_IsMachineMade()
public void DatasetFolder_TwelveDigits_IsMachineMade()
public void DatasetFolder_Cafe1234_IsNotMachineMade()
public void DatasetFolder_NumberedScans_NotSkipped()
public void Idle_RunsIngest_BusyDefers()
public void TwentyChunks_OneFile_ReturnsOneCard()
public void TwentyChunks_OneFile_KeepsMaxRrfScore()
public void Search_GrantFilter_DoesNotPromiseTwenty()
public void ContentUri_EnumeratedDocument_IsInsideGrant()
public void ContentUri_NotEnumerated_IsOutside()
public void ContentUri_PhotosExtra_IsOutside()
public void GrantPathFor_NullDocument_Throws()
public void DeleteOnePath_SharedId_OtherPathStaysHit()
public async Task Shrink_LastPath_DeletesVector()
public async Task Shrink_OtherPathLive_KeepsVector()
public async Task SecondHash_DeletesOldRow_ReembedsSameId()
public async Task Repair_KillAfterDelete_ResumesFromReading()
public async Task Repair_OtherPathLive_DoesNotDelete()
public async Task SecondPath_SameBytes_DoesNotAppendInsert()
public void Open_CopiesChunkPairIntoAlias()
public async Task Find_PendingOver64_ReconcilesSixtyFour()
public async Task Find_NoPending_DoesNotReconcile()
public async Task Find_TwentyChunks_ReturnsOneCard()
public async Task ChangedBytes_DeletesOldZvecRow()
public async Task ChangedBytes_SharedId_KeepsVector()
public void ShortRow_Edit_StaysShortUnlessPinned()
public async Task Find_WriteBusy_SkipsReconcile()
public async Task TextModelReadyIngest_ReadGrantsAsync_CallsIngest()
public void CloudFile_NotLocal_NotIndexed()
public void VectorSchemaStale_Fault_IsTwenty()
if (parts.Index.Inserts.Count > 1)
catch (IOException ex)
catch (IOException ex)
``` 

## tests/Recall.UnitTests/Phase4VisionTests.cs

```csharp
namespace Recall.UnitTests;
public sealed class Phase4VisionTests : IDisposable
public void Vision_UnloadedAfterJob()
public void Vision_Throw_StillUnloads()
public void Create_QuantizeType_StaysUndefined()
public void PictureRow_NotInTextCollection()
public void Frames_OneVideo_OneFileInTop20()
public void Frames_OneVideo_KeepsMaxRrfScore()
public async Task PictureQuery_GrantedImage_HitsPictureCollection()
public async Task PictureFind_OutOfGrant_DoesNotTakeASlot()
public async Task PictureFind_NoGrants_ReturnsEmpty()
public void OcrReaders_MissingHash_DoesNotLoad()
public void EndsSlice_At8192Tokens_IsTrue()
public void EndsSlice_At10Pages_IsTrue()
public void NeedsOcr_ThreeReplacementChars_IsTrue()
catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
if (File.Exists(Path.Combine(directory.FullName, "Recall.slnx")))
```

Covers the Phase 4 surface with fakes and the native source contract: mmproj open codes 1, 6, 8, embed-when-closed 4, width-not-768 5, vision unload on success and throw, the 768 media schema with `QuantizeType.Undefined`, picture rows outside `items_text`, frame collapse to one card per file with the max score, grant filtering, OCR hashes and line choice, the provisional PDF caps, broken-Arabic detection, and both-locale picture waiting copy.


