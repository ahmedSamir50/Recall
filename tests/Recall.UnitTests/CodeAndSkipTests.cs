using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using Recall.Core.Ingest;
using Recall.Core.Preferences;
using Recall.Core.Scope;
using Recall.Core.Text;
using Recall.UnitTests.Fakes;

namespace Recall.UnitTests;

public sealed class CodeAndSkipTests
{
    private static string NewTempRoot()
    {
        return Path.Combine(Path.GetTempPath(), "recall-code-skip-" + Guid.NewGuid().ToString("N"));
    }

    private static void DeleteTempRoot(string root)
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
        catch (IOException)
        {
            GC.KeepAlive(root);
        }
    }

    private sealed class CountingReader : IGrantedTextReader
    {
        public int Calls { get; private set; }

        public async IAsyncEnumerable<GrantedTextDocument> ReadAsync(ScopeFolderGrant grant, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Calls++;
            cancellationToken.ThrowIfCancellationRequested();
            await Task.CompletedTask.ConfigureAwait(false);
            yield break;
        }
    }

    [Fact]
    public void GrantFileKinds_Code_Is16AndInsideAll()
    {
        // Arrange
        // Act
        var code = (int)GrantFileKinds.Code;
        var all = GrantFileKinds.All;

        // Assert
        code.Should().Be(16);
        all.HasFlag(GrantFileKinds.Code).Should().BeTrue();
        all.HasFlag(GrantFileKinds.Text).Should().BeTrue();
        all.HasFlag(GrantFileKinds.Image).Should().BeTrue();
        all.HasFlag(GrantFileKinds.Video).Should().BeTrue();
        all.HasFlag(GrantFileKinds.Audio).Should().BeTrue();
        ((GrantFileKinds)15).HasFlag(GrantFileKinds.Code).Should().BeFalse();
    }

    [Fact]
    public void BuiltInSkips_Lengths_MatchCanon()
    {
        // Arrange
        // Act
        // Assert
        BuiltInSkips.CodeExtensions.Should().HaveCount(83);
        BuiltInSkips.ExactCodeNames.Should().HaveCount(12);
        BuiltInSkips.SkipFolders.Should().HaveCount(45);
        BuiltInSkips.SecretExtensions.Should().HaveCount(25);
        BuiltInSkips.SecretNames.Should().HaveCount(13);
        BuiltInSkips.DatabaseExtensions.Should().HaveCount(27);
        BuiltInSkips.LockNames.Should().HaveCount(14);
        BuiltInSkips.MinifiedSuffixes.Should().HaveCount(4);
        BuiltInSkips.CodeExtensions.Should().Contain(".cs");
        BuiltInSkips.ExactCodeNames.Should().Contain("Makefile");
        BuiltInSkips.SkipFolders.Should().Contain("node_modules");
        BuiltInSkips.SecretExtensions.Should().Contain("pem");
        BuiltInSkips.SecretNames.Should().Contain(".npmrc");
        BuiltInSkips.DatabaseExtensions.Should().Contain("db");
        BuiltInSkips.LockNames.Should().Contain("package-lock.json");
        BuiltInSkips.MinifiedSuffixes.Should().Contain(".min.js");
    }

    [Fact]
    public void IsBlockedFolder_GitHub_IsFalse()
    {
        // Arrange
        // Act
        var blocked = FileAdmission.IsBlockedFolder(".github", UserSkipAdditions.Empty);

        // Assert
        blocked.Should().BeFalse();
    }

    [Fact]
    public void IsBlockedFolder_Git_IsTrue()
    {
        // Arrange
        // Act
        var blocked = FileAdmission.IsBlockedFolder(".git", UserSkipAdditions.Empty);

        // Assert
        blocked.Should().BeTrue();
    }

    [Fact]
    public void IsBlockedFolder_UserName_IsTrue()
    {
        // Arrange
        var user = new UserSkipAdditions([], ["scratch"], []);

        // Act
        var blocked = FileAdmission.IsBlockedFolder("scratch", user);

        // Assert
        blocked.Should().BeTrue();
    }

    [Fact]
    public void IsBlockedFolder_Blank_IsFalse()
    {
        // Arrange
        // Act
        var blocked = FileAdmission.IsBlockedFolder("   ", UserSkipAdditions.Empty);

        // Assert
        blocked.Should().BeFalse();
    }

    [Fact]
    public void IsSecret_Pem_IsTrue()
    {
        // Arrange
        // Act
        var secret = FileAdmission.IsSecret("cert.pem");

        // Assert
        secret.Should().BeTrue();
    }

    [Fact]
    public void IsSecret_Env_IsTrue()
    {
        // Arrange
        // Act
        var secret = FileAdmission.IsSecret(".env");

        // Assert
        secret.Should().BeTrue();
    }

    [Fact]
    public void IsSecret_EnvExample_IsFalse()
    {
        // Arrange
        // Act
        var secret = FileAdmission.IsSecret(".env.example");

        // Assert
        secret.Should().BeFalse();
    }

    [Fact]
    public void IsSecret_EnvExampleSuffix_IsFalse()
    {
        // Arrange
        // Act
        var secret = FileAdmission.IsSecret("prod.env.example");

        // Assert
        secret.Should().BeFalse();
    }

    [Fact]
    public void IsSecret_EnvExampleBak_IsTrue()
    {
        // Arrange
        // Act
        var secret = FileAdmission.IsSecret(".env.example.bak");

        // Assert
        secret.Should().BeTrue();
    }

    [Fact]
    public void IsSecret_AppSettingsJson_IsFalse()
    {
        // Arrange
        // Act
        var secret = FileAdmission.IsSecret("appsettings.json");

        // Assert
        secret.Should().BeFalse();
    }

    [Fact]
    public void IsSecret_AppSettingsDevelopment_IsTrue()
    {
        // Arrange
        // Act
        var secret = FileAdmission.IsSecret("appsettings.Development.json");

        // Assert
        secret.Should().BeTrue();
    }

    [Fact]
    public void IsSecret_IdRsaPrefix_IsTrue()
    {
        // Arrange
        // Act
        var secret = FileAdmission.IsSecret("id_rsa_backup");

        // Assert
        secret.Should().BeTrue();
    }

    [Fact]
    public void IsSecret_NotIdRsa_IsFalse()
    {
        // Arrange
        // Act
        var secret = FileAdmission.IsSecret("notid_rsa_notes.txt");

        // Assert
        secret.Should().BeFalse();
    }

    [Fact]
    public void IsSecret_BareSecret_IsTrue()
    {
        // Arrange
        // Act
        var secret = FileAdmission.IsSecret("secret");

        // Assert
        secret.Should().BeTrue();
    }

    [Fact]
    public void IsDatabase_Db_IsTrue()
    {
        // Arrange
        // Act
        var db = FileAdmission.IsDatabase("data.db");

        // Assert
        db.Should().BeTrue();
    }

    [Fact]
    public void IsDatabase_Sql_IsFalse()
    {
        // Arrange
        // Act
        var db = FileAdmission.IsDatabase("query.sql");

        // Assert
        db.Should().BeFalse();
    }

    [Fact]
    public void IsLockFile_PackageLock_IsTrue()
    {
        // Arrange
        // Act
        var locked = FileAdmission.IsLockFile("package-lock.json");

        // Assert
        locked.Should().BeTrue();
    }

    [Fact]
    public void IsMinified_MinJs_IsTrue()
    {
        // Arrange
        // Act
        var min = FileAdmission.IsMinified("app.min.js");

        // Assert
        min.Should().BeTrue();
    }

    [Fact]
    public void IsCodeFile_MinJs_IsFalse()
    {
        // Arrange
        // Act
        var code = FileAdmission.IsCodeFile("app.min.js");

        // Assert
        code.Should().BeFalse();
    }

    [Fact]
    public void IsCodeFile_Cs_IsTrue()
    {
        // Arrange
        // Act
        var code = FileAdmission.IsCodeFile("Note.cs");

        // Assert
        code.Should().BeTrue();
    }

    [Fact]
    public void IsCodeFile_MakefileAnyCase_IsTrue()
    {
        // Arrange
        // Act
        var code = FileAdmission.IsCodeFile("MAKEFILE");

        // Assert
        code.Should().BeTrue();
    }

    [Fact]
    public void IsCodeFile_Txt_IsFalse()
    {
        // Arrange
        // Act
        var code = FileAdmission.IsCodeFile("notes.txt");

        // Assert
        code.Should().BeFalse();
    }

    [Fact]
    public void Decide_CsWithCode_Reads()
    {
        // Arrange
        // Act
        var result = FileAdmission.Decide("Note.cs", GrantFileKinds.Code, UserSkipAdditions.Empty, false);

        // Assert
        result.Should().Be(FileAdmissionResult.Read);
    }

    [Fact]
    public void Decide_CsWithoutCode_SkipsKind()
    {
        // Arrange
        // Act
        var result = FileAdmission.Decide("Note.cs", GrantFileKinds.Text, UserSkipAdditions.Empty, false);

        // Assert
        result.Should().Be(FileAdmissionResult.SkipKind);
    }

    [Fact]
    public void Decide_TxtWithText_Reads()
    {
        // Arrange
        // Act
        var result = FileAdmission.Decide("notes.txt", GrantFileKinds.Text, UserSkipAdditions.Empty, false);

        // Assert
        result.Should().Be(FileAdmissionResult.Read);
    }

    [Fact]
    public void Decide_TxtWithoutText_SkipsKind()
    {
        // Arrange
        // Act
        var result = FileAdmission.Decide("notes.txt", GrantFileKinds.Code, UserSkipAdditions.Empty, false);

        // Assert
        result.Should().Be(FileAdmissionResult.SkipKind);
    }

    [Fact]
    public void Decide_Png_SkipsOther()
    {
        // Arrange
        // Act
        var result = FileAdmission.Decide("photo.png", GrantFileKinds.All, UserSkipAdditions.Empty, false);

        // Assert
        result.Should().Be(FileAdmissionResult.SkipOther);
    }

    [Fact]
    public void Decide_Blank_SkipsOther()
    {
        // Arrange
        // Act
        var result = FileAdmission.Decide("   ", GrantFileKinds.All, UserSkipAdditions.Empty, false);

        // Assert
        result.Should().Be(FileAdmissionResult.SkipOther);
    }

    [Fact]
    public void Decide_Env_SkipsSecretEvenWhenNotGitIgnored()
    {
        // Arrange
        // Act
        var result = FileAdmission.Decide(".env", GrantFileKinds.All, UserSkipAdditions.Empty, false);

        // Assert
        result.Should().Be(FileAdmissionResult.SkipSecret);
    }

    [Fact]
    public void Decide_Db_SkipsDatabase()
    {
        // Arrange
        // Act
        var result = FileAdmission.Decide("data.db", GrantFileKinds.All, UserSkipAdditions.Empty, false);

        // Assert
        result.Should().Be(FileAdmissionResult.SkipDatabase);
    }

    [Fact]
    public void Decide_Lock_SkipsLock()
    {
        // Arrange
        // Act
        var result = FileAdmission.Decide("package-lock.json", GrantFileKinds.All, UserSkipAdditions.Empty, false);

        // Assert
        result.Should().Be(FileAdmissionResult.SkipLock);
    }

    [Fact]
    public void Decide_UserFile_Skips()
    {
        // Arrange
        var user = new UserSkipAdditions([], [], ["custom.txt"]);

        // Act
        var result = FileAdmission.Decide("custom.txt", GrantFileKinds.All, user, false);

        // Assert
        result.Should().Be(FileAdmissionResult.SkipUserFile);
    }

    [Fact]
    public void Decide_UserExtension_Skips()
    {
        // Arrange
        var user = new UserSkipAdditions([".log"], [], []);

        // Act
        var result = FileAdmission.Decide("trace.log", GrantFileKinds.All, user, false);

        // Assert
        result.Should().Be(FileAdmissionResult.SkipUserExtension);
    }

    [Fact]
    public void Decide_Minified_SkipsBeforeKind()
    {
        // Arrange
        // Act
        var result = FileAdmission.Decide("app.min.js", GrantFileKinds.All, UserSkipAdditions.Empty, false);

        // Assert
        result.Should().Be(FileAdmissionResult.SkipMinified);
    }

    [Fact]
    public void Decide_GitIgnored_Skips()
    {
        // Arrange
        // Act
        var result = FileAdmission.Decide("notes.txt", GrantFileKinds.Text, UserSkipAdditions.Empty, true);

        // Assert
        result.Should().Be(FileAdmissionResult.SkipGitIgnore);
    }

    [Fact]
    public void Decide_SqlWithCode_Reads()
    {
        // Arrange
        // Act
        var result = FileAdmission.Decide("query.sql", GrantFileKinds.Code, UserSkipAdditions.Empty, false);

        // Assert
        result.Should().Be(FileAdmissionResult.Read);
    }

    [Fact]
    public void TryParse_BlankAndComment_False()
    {
        // Arrange
        // Act
        var blank = GitIgnoreMatcher.TryParse("   ", out _);
        var comment = GitIgnoreMatcher.TryParse("# comment", out _);

        // Assert
        blank.Should().BeFalse();
        comment.Should().BeFalse();
    }

    [Fact]
    public void TryParse_EscapedHash_IsPattern()
    {
        // Arrange
        // Act
        var ok = GitIgnoreMatcher.TryParse("\\#foo", out var pattern);

        // Assert
        ok.Should().BeTrue();
        pattern.Body.Should().Be("#foo");
        pattern.Negated.Should().BeFalse();
    }

    [Fact]
    public void TryParse_Negation_SetsFlag()
    {
        // Arrange
        // Act
        var ok = GitIgnoreMatcher.TryParse("!foo", out var pattern);

        // Assert
        ok.Should().BeTrue();
        pattern.Negated.Should().BeTrue();
        pattern.Body.Should().Be("foo");
    }

    [Fact]
    public void TryParse_EscapedBang_IsLiteral()
    {
        // Arrange
        // Act
        var ok = GitIgnoreMatcher.TryParse("\\!foo", out var pattern);

        // Assert
        ok.Should().BeTrue();
        pattern.Negated.Should().BeFalse();
        pattern.Body.Should().Be("!foo");
    }

    [Fact]
    public void TryParse_TrailingSlash_DirectoryOnly()
    {
        // Arrange
        // Act
        var ok = GitIgnoreMatcher.TryParse("build/", out var pattern);

        // Assert
        ok.Should().BeTrue();
        pattern.DirectoryOnly.Should().BeTrue();
        pattern.Body.Should().Be("build");
    }

    [Fact]
    public void TryParse_AnchoredSlash_StripsLeadingSlash()
    {
        // Arrange
        // Act
        var ok = GitIgnoreMatcher.TryParse("/foo", out var pattern);

        // Assert
        ok.Should().BeTrue();
        pattern.Anchored.Should().BeTrue();
        pattern.Body.Should().Be("foo");
    }

    [Fact]
    public void IsIgnored_StarLog_MatchesNestedName()
    {
        // Arrange
        var files = new List<GitIgnoreFile> { new(string.Empty, ["*.log"]) };

        // Act
        var ignored = GitIgnoreMatcher.IsIgnored("a/b/c.log", files);

        // Assert
        ignored.Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_Negation_Reincludes()
    {
        // Arrange
        var files = new List<GitIgnoreFile> { new(string.Empty, ["*.log", "!keep.log"]) };

        // Act
        var ignored = GitIgnoreMatcher.IsIgnored("keep.log", files);

        // Assert
        ignored.Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_ParentDirectory_BlocksChildException()
    {
        // Arrange
        var files = new List<GitIgnoreFile> { new(string.Empty, ["build/", "!build/keep.txt"]) };

        // Act
        var ignored = GitIgnoreMatcher.IsIgnored("build/keep.txt", files);

        // Assert
        ignored.Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_Anchored_DoesNotMatchNested()
    {
        // Arrange
        var files = new List<GitIgnoreFile> { new(string.Empty, ["/foo"]) };

        // Act
        var ignored = GitIgnoreMatcher.IsIgnored("a/foo", files);

        // Assert
        ignored.Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_QuestionMark_OneCharacter()
    {
        // Arrange
        var files = new List<GitIgnoreFile> { new(string.Empty, ["?.txt"]) };

        // Act
        var one = GitIgnoreMatcher.IsIgnored("a.txt", files);
        var two = GitIgnoreMatcher.IsIgnored("ab.txt", files);

        // Assert
        one.Should().BeTrue();
        two.Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_GlobStar_CrossesDirectories()
    {
        // Arrange
        var files = new List<GitIgnoreFile> { new(string.Empty, ["a/**/c"]) };

        // Act
        var ignored = GitIgnoreMatcher.IsIgnored("a/x/y/c", files);

        // Assert
        ignored.Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_CharacterClass_MatchesSet()
    {
        // Arrange
        var files = new List<GitIgnoreFile> { new(string.Empty, ["[abc].txt"]) };

        // Act
        var ignored = GitIgnoreMatcher.IsIgnored("b.txt", files);

        // Assert
        ignored.Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_CharacterClass_DoesNotMatchSlash()
    {
        // Arrange
        var negated = new List<GitIgnoreFile> { new(string.Empty, ["a[!b]c"]) };
        var slashClass = new List<GitIgnoreFile> { new(string.Empty, ["a[/]b"]) };

        // Act
        var outsideSet = GitIgnoreMatcher.IsIgnored("axc", negated);
        var negatedSlash = GitIgnoreMatcher.IsIgnored("a/c", negated);
        var explicitSlash = GitIgnoreMatcher.IsIgnored("a/b", slashClass);

        // Assert
        outsideSet.Should().BeTrue();
        negatedSlash.Should().BeFalse();
        explicitSlash.Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_UnclosedBracket_IsLiteral()
    {
        // Arrange
        var files = new List<GitIgnoreFile> { new(string.Empty, ["foo[.txt"]) };

        // Act
        var literal = GitIgnoreMatcher.IsIgnored("foo[.txt", files);
        var other = GitIgnoreMatcher.IsIgnored("footxt", files);

        // Assert
        literal.Should().BeTrue();
        other.Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_TrailingBackslash_DoesNotJoinNextLine()
    {
        // Arrange
        var files = new List<GitIgnoreFile> { new(string.Empty, ["foo\\", "bar"]) };

        // Act
        var foo = GitIgnoreMatcher.IsIgnored("foo", files);
        var bar = GitIgnoreMatcher.IsIgnored("bar", files);

        // Assert
        foo.Should().BeFalse();
        bar.Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_NoFiles_False()
    {
        // Arrange
        // Act
        var ignored = GitIgnoreMatcher.IsIgnored("notes.txt", []);

        // Assert
        ignored.Should().BeFalse();
    }

    [Fact]
    public async Task LoadDirectoryFiles_OutsideGrant_DoesNotReadParent()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            var grant = Path.Combine(root, "grant");
            var sub = Path.Combine(grant, "sub");
            Directory.CreateDirectory(sub);
            await File.WriteAllTextAsync(Path.Combine(root, ".gitignore"), "*.txt\n", TestContext.Current.CancellationToken);

            // Act
            var files = GitIgnoreMatcher.LoadDirectoryFiles(grant, sub);
            var ignored = GitIgnoreMatcher.IsIgnored("sub/notes.txt", files);

            // Assert
            ignored.Should().BeFalse();
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task LoadDirectoryFiles_ExcludeFile_IgnoresNamedFile()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            var excludeDir = Path.Combine(root, ".git", "info");
            Directory.CreateDirectory(excludeDir);
            await File.WriteAllTextAsync(Path.Combine(excludeDir, "exclude"), "hidden.txt\n", TestContext.Current.CancellationToken);

            // Act
            var files = GitIgnoreMatcher.LoadDirectoryFiles(root, root);
            var ignored = GitIgnoreMatcher.IsIgnored("hidden.txt", files);

            // Assert
            ignored.Should().BeTrue();
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task LoadDirectoryFiles_NestedNegation_Wins()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            var sub = Path.Combine(root, "sub");
            Directory.CreateDirectory(sub);
            await File.WriteAllTextAsync(Path.Combine(root, ".gitignore"), "*.txt\n", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(sub, ".gitignore"), "!keep.txt\n", TestContext.Current.CancellationToken);

            // Act
            var files = GitIgnoreMatcher.LoadDirectoryFiles(root, sub);
            var keep = GitIgnoreMatcher.IsIgnored("sub/keep.txt", files);
            var other = GitIgnoreMatcher.IsIgnored("sub/other.txt", files);

            // Assert
            keep.Should().BeFalse();
            other.Should().BeTrue();
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void UserSkipJson_Blank_Empty()
    {
        // Arrange
        // Act
        var blank = UserSkipJson.Deserialize("   ");
        var empty = UserSkipJson.Deserialize("");

        // Assert
        blank.Extensions.Should().BeEmpty();
        empty.FileNames.Should().BeEmpty();
    }

    [Fact]
    public void UserSkipJson_NullLists_BecomeEmpty()
    {
        // Arrange
        // Act
        var additions = UserSkipJson.Deserialize("{\"Extensions\":null,\"FolderNames\":null,\"FileNames\":null}");

        // Assert
        additions.Extensions.Should().BeEmpty();
        additions.FolderNames.Should().BeEmpty();
        additions.FileNames.Should().BeEmpty();
    }

    [Fact]
    public void UserSkipJson_InvalidJson_ReturnsEmpty()
    {
        // Arrange
        // Act
        var additions = UserSkipJson.Deserialize("{not-json");

        // Assert
        additions.Extensions.Should().BeEmpty();
        additions.FolderNames.Should().BeEmpty();
        additions.FileNames.Should().BeEmpty();
    }

    [Fact]
    public void TryAdd_Whitespace_Empty()
    {
        // Arrange
        var service = new UserSkipService(new FakeUserSkipStore());

        // Act
        var status = service.TryAdd(UserSkipKind.Extension, "   ");

        // Assert
        status.Should().Be(UserSkipAddStatus.Empty);
    }

    [Fact]
    public void TryAdd_Separator_Empty()
    {
        // Arrange
        var service = new UserSkipService(new FakeUserSkipStore());

        // Act
        var status = service.TryAdd(UserSkipKind.FolderName, "a/b");

        // Assert
        status.Should().Be(UserSkipAddStatus.Empty);
    }

    [Fact]
    public void TryAdd_DotOnly_Empty()
    {
        // Arrange
        var service = new UserSkipService(new FakeUserSkipStore());

        // Act
        var status = service.TryAdd(UserSkipKind.Extension, ".");

        // Assert
        status.Should().Be(UserSkipAddStatus.Empty);
    }

    [Fact]
    public void TryAdd_BuiltInCs_BuiltIn()
    {
        // Arrange
        var service = new UserSkipService(new FakeUserSkipStore());

        // Act
        var status = service.TryAdd(UserSkipKind.Extension, ".cs");

        // Assert
        status.Should().Be(UserSkipAddStatus.BuiltIn);
    }

    [Fact]
    public void TryAdd_BuiltInFolder_BuiltIn()
    {
        // Arrange
        var service = new UserSkipService(new FakeUserSkipStore());

        // Act
        var status = service.TryAdd(UserSkipKind.FolderName, "node_modules");

        // Assert
        status.Should().Be(UserSkipAddStatus.BuiltIn);
    }

    [Fact]
    public void TryAdd_Duplicate_Duplicate()
    {
        // Arrange
        var store = new FakeUserSkipStore();
        var service = new UserSkipService(store);
        service.TryAdd(UserSkipKind.Extension, ".log");

        // Act
        var status = service.TryAdd(UserSkipKind.Extension, ".LOG");

        // Assert
        status.Should().Be(UserSkipAddStatus.Duplicate);
    }

    [Fact]
    public void TryAdd_Log_AddedAndLowercased()
    {
        // Arrange
        var store = new FakeUserSkipStore();
        var service = new UserSkipService(store);

        // Act
        var status = service.TryAdd(UserSkipKind.Extension, "LOG");

        // Assert
        status.Should().Be(UserSkipAddStatus.Added);
        store.Load().Extensions.Should().Contain(".log");
    }

    [Fact]
    public void TryRemove_BuiltIn_False()
    {
        // Arrange
        var service = new UserSkipService(new FakeUserSkipStore());

        // Act
        var removed = service.TryRemove(UserSkipKind.Extension, ".cs");

        // Assert
        removed.Should().BeFalse();
    }

    [Fact]
    public void TryRemove_Missing_False()
    {
        // Arrange
        var service = new UserSkipService(new FakeUserSkipStore());

        // Act
        var removed = service.TryRemove(UserSkipKind.Extension, ".log");

        // Assert
        removed.Should().BeFalse();
    }

    [Fact]
    public void TryRemove_UserRow_True()
    {
        // Arrange
        var store = new FakeUserSkipStore();
        var service = new UserSkipService(store);
        service.TryAdd(UserSkipKind.Extension, ".log");

        // Act
        var removed = service.TryRemove(UserSkipKind.Extension, ".log");

        // Assert
        removed.Should().BeTrue();
    }

    [Fact]
    public void MessageKey_Added_Null()
    {
        // Arrange
        // Act
        var key = UserSkipService.MessageKey(UserSkipAddStatus.Added);

        // Assert
        key.Should().BeNull();
    }

    [Fact]
    public void MessageKey_Empty_Key()
    {
        // Arrange
        // Act
        var key = UserSkipService.MessageKey(UserSkipAddStatus.Empty);

        // Assert
        key.Should().Be("Skips_Empty");
    }

    [Fact]
    public void MessageKey_Duplicate_Key()
    {
        // Arrange
        // Act
        var key = UserSkipService.MessageKey(UserSkipAddStatus.Duplicate);

        // Assert
        key.Should().Be("Skips_Duplicate");
    }

    [Fact]
    public void MessageKey_BuiltIn_Key()
    {
        // Arrange
        // Act
        var key = UserSkipService.MessageKey(UserSkipAddStatus.BuiltIn);

        // Assert
        key.Should().Be("Skips_BuiltIn");
    }

    [Fact]
    public void MessageKey_Unknown_EmptyKey()
    {
        // Arrange
        // Act
        var key = UserSkipService.MessageKey((UserSkipAddStatus)99);

        // Assert
        key.Should().Be("Skips_Empty");
    }

    [Fact]
    public void IsSafeCodeText_Shebang_True()
    {
        // Arrange
        var sample = Encoding.UTF8.GetBytes("#!/bin/sh\necho hi");

        // Act
        var safe = TextFileSignature.IsSafeCodeText(sample);

        // Assert
        safe.Should().BeTrue();
    }

    [Fact]
    public void IsSafeText_Shebang_False()
    {
        // Arrange
        var sample = Encoding.UTF8.GetBytes("#!/bin/sh\necho hi");

        // Act
        var safe = TextFileSignature.IsSafeText(sample);

        // Assert
        safe.Should().BeFalse();
    }

    [Fact]
    public void IsSafeCodeText_Empty_False()
    {
        // Arrange
        // Act
        var safe = TextFileSignature.IsSafeCodeText(ReadOnlySpan<byte>.Empty);

        // Assert
        safe.Should().BeFalse();
    }

    [Fact]
    public void IsSafeCodeText_NullByte_False()
    {
        // Arrange
        var sample = new byte[] { (byte)'a', 0, (byte)'b' };

        // Act
        var safe = TextFileSignature.IsSafeCodeText(sample);

        // Assert
        safe.Should().BeFalse();
    }

    [Fact]
    public void IsSafeCodeText_Mz_False()
    {
        // Arrange
        var sample = new byte[] { (byte)'M', (byte)'Z', 0x90, 0 };

        // Act
        var safe = TextFileSignature.IsSafeCodeText(sample);

        // Assert
        safe.Should().BeFalse();
    }

    [Fact]
    public async Task ReadAsync_MissingDirectory_YieldsNothing()
    {
        // Arrange
        var root = NewTempRoot();
        var reader = IngestTestKit.CreateDirectoryReader();
        var grant = new ScopeFolderGrant(Path.Combine(root, "missing"), "missing", GrantFileKinds.All);

        // Act
        var count = 0;
        await foreach (var _ in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
        {
            count++;
        }

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Cancelled_Throws()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "notes.txt"), "hello", TestContext.Current.CancellationToken);
            var reader = IngestTestKit.CreateDirectoryReader();
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.All);
            using var cancelled = new CancellationTokenSource();
            await cancelled.CancelAsync();

            // Act
            Func<Task> act = async () =>
            {
                await foreach (var _ in reader.ReadAsync(grant, cancelled.Token).ConfigureAwait(false))
                {
                    GC.KeepAlive(grant);
                }
            };

            // Assert
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task ReadAsync_CodeOn_YieldsCs()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "Note.cs"), "class Note {}", TestContext.Current.CancellationToken);
            var reader = IngestTestKit.CreateDirectoryReader();
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.All);

            // Act
            var docs = new List<GrantedTextDocument>();
            await foreach (var doc in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
            {
                docs.Add(doc);
            }

            // Assert
            docs.Should().ContainSingle();
            docs[0].FileName.Should().Be("Note.cs");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task ReadAsync_CodeOff_SkipsCs()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "Note.cs"), "class Note {}", TestContext.Current.CancellationToken);
            var reader = IngestTestKit.CreateDirectoryReader();
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.Text);

            // Act
            var count = 0;
            await foreach (var _ in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
            {
                count++;
            }

            // Assert
            count.Should().Be(0);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task ReadAsync_TextOff_SkipsTxt()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "notes.txt"), "hello", TestContext.Current.CancellationToken);
            var reader = IngestTestKit.CreateDirectoryReader();
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.Code);

            // Act
            var count = 0;
            await foreach (var _ in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
            {
                count++;
            }

            // Assert
            count.Should().Be(0);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task ReadAsync_Env_NotRead()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, ".env"), "KEY=1", TestContext.Current.CancellationToken);
            var reader = IngestTestKit.CreateDirectoryReader();
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.All);

            // Act
            var count = 0;
            await foreach (var _ in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
            {
                count++;
            }

            // Assert
            count.Should().Be(0);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task ReadAsync_Db_NotRead()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "data.db"), "x", TestContext.Current.CancellationToken);
            var reader = IngestTestKit.CreateDirectoryReader();
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.All);

            // Act
            var count = 0;
            await foreach (var _ in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
            {
                count++;
            }

            // Assert
            count.Should().Be(0);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task ReadAsync_GitIgnore_SkipsTxt()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, ".gitignore"), "*.txt\n", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(root, "notes.txt"), "hello", TestContext.Current.CancellationToken);
            var reader = IngestTestKit.CreateDirectoryReader();
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.All);

            // Act
            var count = 0;
            await foreach (var _ in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
            {
                count++;
            }

            // Assert
            count.Should().Be(0);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task ReadAsync_GitIgnoreException_ReadsKeepTxt()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, ".gitignore"), "*.txt\n!keep.txt\n", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(root, "keep.txt"), "hello", TestContext.Current.CancellationToken);
            var reader = IngestTestKit.CreateDirectoryReader();
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.All);

            // Act
            var docs = new List<GrantedTextDocument>();
            await foreach (var doc in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
            {
                docs.Add(doc);
            }

            // Assert
            docs.Should().ContainSingle();
            docs[0].FileName.Should().Be("keep.txt");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task ReadAsync_GitIgnoreException_DoesNotReadEnv()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, ".gitignore"), "!.env\n", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(root, ".env"), "KEY=1", TestContext.Current.CancellationToken);
            var reader = IngestTestKit.CreateDirectoryReader();
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.All);

            // Act
            var count = 0;
            await foreach (var _ in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
            {
                count++;
            }

            // Assert
            count.Should().Be(0);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task ReadAsync_NodeModules_NotRead()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            var nested = Path.Combine(root, "node_modules");
            Directory.CreateDirectory(nested);
            await File.WriteAllTextAsync(Path.Combine(nested, "a.cs"), "class A {}", TestContext.Current.CancellationToken);
            var reader = IngestTestKit.CreateDirectoryReader();
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.All);

            // Act
            var count = 0;
            await foreach (var _ in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
            {
                count++;
            }

            // Assert
            count.Should().Be(0);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task ReadAsync_UserExtension_NotRead()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "notes.txt"), "hello", TestContext.Current.CancellationToken);
            var store = new FakeUserSkipStore();
            var service = new UserSkipService(store);
            service.TryAdd(UserSkipKind.Extension, ".txt");
            var reader = IngestTestKit.CreateDirectoryReader(store);
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.All);

            // Act
            var count = 0;
            await foreach (var _ in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
            {
                count++;
            }

            // Assert
            count.Should().Be(0);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task ReadAsync_DotGitHub_ReadsCs()
    {
        // Arrange
        var root = NewTempRoot();
        try
        {
            var nested = Path.Combine(root, ".github");
            Directory.CreateDirectory(nested);
            await File.WriteAllTextAsync(Path.Combine(nested, "a.cs"), "class A {}", TestContext.Current.CancellationToken);
            var reader = IngestTestKit.CreateDirectoryReader();
            var grant = new ScopeFolderGrant(root, "root", GrantFileKinds.All);

            // Act
            var docs = new List<GrantedTextDocument>();
            await foreach (var doc in reader.ReadAsync(grant, TestContext.Current.CancellationToken))
            {
                docs.Add(doc);
            }

            // Assert
            docs.Should().ContainSingle();
            docs[0].FileName.Should().Be("a.cs");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public async Task IngestFileAsync_CodePath_InsertsTitleAnd512()
    {
        // Arrange
        var encoder = new FakeTextEmbeddingEncoder();
        var index = new FakeTextVectorIndex();
        var oplogDir = NewTempRoot();
        try
        {
            Directory.CreateDirectory(oplogDir);
            var (engine, _, _) = IngestTestKit.CreateEngineParts(encoder, index, oplogDir);

            // Act
            await engine.IngestFileAsync(
                Path.Combine(oplogDir, "Note.cs"),
                "class Note {}",
                DateTimeOffset.UtcNow,
                oplogDir,
                "enc",
                codeFile: true,
                TestContext.Current.CancellationToken);

            // Assert
            encoder.LastPrefixed.Should().Be("title: Note.cs | text: class Note {}");
            index.Inserts.Should().ContainSingle();
            index.Inserts[0].FileName.Should().Be("Note.cs");
            index.Inserts[0].Vector512.Length.Should().Be(512);
        }
        finally
        {
            DeleteTempRoot(oplogDir);
        }
    }

    [Fact]
    public async Task IngestGrantedTextAsync_CodeOnly_CallsReader()
    {
        // Arrange
        var store = new FakeScopeGrantStore();
        var scope = new ScopeGrantService(store);
        scope.TryAddGrant(new ScopeFolderGrant("D:\\Code", "Code", GrantFileKinds.Code));
        scope.TryAddGrant(new ScopeFolderGrant("D:\\Pics", "Pics", GrantFileKinds.Image));
        var reader = new CountingReader();
        var encoder = new FakeTextEmbeddingEncoder();
        var index = new FakeTextVectorIndex();
        var oplogDir = NewTempRoot();
        try
        {
            Directory.CreateDirectory(oplogDir);
            var cursors = new FakeIngestCursorStore();
            var (engine, opLog, _) = IngestTestKit.CreateEngineParts(encoder, index, oplogDir);
            var service = IngestTestKit.CreateGrantedService(scope, reader, engine, cursors, index, opLog);

            // Act
            await service.IngestGrantedTextAsync(TestContext.Current.CancellationToken);

            // Assert
            reader.Calls.Should().Be(1);
        }
        finally
        {
            DeleteTempRoot(oplogDir);
        }
    }

    [Fact]
    public async Task FindAsync_CodeGrant_IncludesPath()
    {
        // Arrange
        var encoder = new FakeTextEmbeddingEncoder();
        var index = new FakeTextVectorIndex();
        var service = IngestTestKit.CreateSearchService(encoder, index);
        var grants = new List<ScopeFolderGrant>
        {
            new("D:\\Code", "Code", GrantFileKinds.Code),
            new("D:\\Pics", "Pics", GrantFileKinds.Image),
        };

        // Act
        await service.FindAsync("hello", grants, TestContext.Current.CancellationToken);

        // Assert
        index.LastRequest.Should().NotBeNull();
        ArgumentNullException.ThrowIfNull(index.LastRequest);
        index.LastRequest.GrantedPaths.Should().Contain("D:\\Code");
        index.LastRequest.GrantedPaths.Should().NotContain("D:\\Pics");
    }

    [Fact]
    public async Task FindAsync_Query_HasSearchPrefixNotCodePrefix()
    {
        // Arrange
        var encoder = new FakeTextEmbeddingEncoder();
        var index = new FakeTextVectorIndex();
        var service = IngestTestKit.CreateSearchService(encoder, index);
        var grants = new List<ScopeFolderGrant> { new("D:\\Code", "Code", GrantFileKinds.Code) };

        // Act
        await service.FindAsync("hello", grants, TestContext.Current.CancellationToken);

        // Assert
        encoder.LastPrefixed.Should().StartWith("task: search result | query: ");
        encoder.LastPrefixed.Should().NotContain("code:");
    }

    [Fact]
    public void AndroidReader_Source_CallsSharedAdmission()
    {
        // Arrange
        var root = FindRepoRoot();
        var path = Path.Combine(root, "src", "Recall.Maui", "Platform", "Folders", "MauiGrantedTextReader.Android.cs");

        // Act
        var text = File.ReadAllText(path);

        // Assert
        text.Should().Contain("FileAdmission.IsBlockedFolder");
        text.Should().Contain("FileAdmission.Decide");
        text.Should().Contain("GitIgnoreMatcher.IsIgnored");
        text.Should().Contain("IsSafeCodeText");
    }

    [Fact]
    public void CodeAndSkipCopy_MatchesCanon()
    {
        // Arrange
        var root = FindRepoRoot();
        var enPath = Path.Combine(root, "src", "Recall.UI", "Resources", "Strings.resx");
        var arPath = Path.Combine(root, "src", "Recall.UI", "Resources", "Strings.ar.resx");
        var expectedEn = new Dictionary<string, string>
        {
            ["Folders_TypeCode"] = "Code",
            ["Skips_Heading"] = "Skipped files",
            ["Skips_CodeExtensions"] = "Code extensions",
            ["Skips_Folders"] = "Folders never read",
            ["Skips_Secrets"] = "Secrets and databases",
            ["Skips_Add"] = "Add another",
            ["Skips_Note"] = "Recall never opens these, even inside a folder you granted.",
            ["Skips_CodeIndexed"] = "Indexed when Code is on.",
            ["Skips_Empty"] = "Type a name first.",
            ["Skips_Duplicate"] = "That row is already listed.",
            ["Skips_BuiltIn"] = "That row is built in and stays.",
            ["Skips_Remove"] = "Remove",
        };
        var expectedAr = new Dictionary<string, string>
        {
            ["Folders_TypeCode"] = "كود",
            ["Skips_Heading"] = "الملفات المتخطاة",
            ["Skips_CodeExtensions"] = "امتدادات الكود",
            ["Skips_Folders"] = "مجلدات لا تُقرأ",
            ["Skips_Secrets"] = "الأسرار وقواعد البيانات",
            ["Skips_Add"] = "أضف غيرها",
            ["Skips_Note"] = "لن يفتح ريكول هذه الملفات، حتى داخل مجلد سمحت به.",
            ["Skips_CodeIndexed"] = "تُفهرس عند تفعيل الكود.",
            ["Skips_Empty"] = "اكتب اسماً أولاً.",
            ["Skips_Duplicate"] = "هذا السطر موجود.",
            ["Skips_BuiltIn"] = "هذا السطر مدمج ويبقى.",
            ["Skips_Remove"] = "إزالة",
        };

        // Act
        var enValues = ReadResx(enPath);
        var arValues = ReadResx(arPath);

        // Assert
        foreach (var pair in expectedEn)
        {
            enValues.Should().ContainKey(pair.Key);
            enValues[pair.Key].Should().Be(pair.Value);
        }

        foreach (var pair in expectedAr)
        {
            arValues.Should().ContainKey(pair.Key);
            arValues[pair.Key].Should().Be(pair.Value);
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
