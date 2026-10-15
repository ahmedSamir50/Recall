using Recall.Core.Preferences;
using Recall.Core.Text;

namespace Recall.Core.Scope;

public enum FileAdmissionResult
{
    Read = 0,
    SkipFolder = 1,
    SkipSecret = 2,
    SkipDatabase = 3,
    SkipLock = 4,
    SkipUserFile = 5,
    SkipUserExtension = 6,
    SkipMinified = 7,
    SkipGitIgnore = 8,
    SkipKind = 9,
    SkipOther = 10,
}

public static class FileAdmission
{
    public static FileAdmissionResult Decide(string fileName, GrantFileKinds kinds, UserSkipAdditions user, bool gitIgnored)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return FileAdmissionResult.SkipOther;
        }

        if (IsSecret(fileName))
        {
            return FileAdmissionResult.SkipSecret;
        }

        if (IsDatabase(fileName))
        {
            return FileAdmissionResult.SkipDatabase;
        }

        if (IsLockFile(fileName))
        {
            return FileAdmissionResult.SkipLock;
        }

        if (user.FileNames.Any(candidate => string.Equals(candidate, fileName, StringComparison.OrdinalIgnoreCase)))
        {
            return FileAdmissionResult.SkipUserFile;
        }

        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(extension)
            && user.Extensions.Any(candidate => string.Equals(candidate, extension, StringComparison.OrdinalIgnoreCase)))
        {
            return FileAdmissionResult.SkipUserExtension;
        }

        if (IsMinified(fileName))
        {
            return FileAdmissionResult.SkipMinified;
        }

        if (gitIgnored)
        {
            return FileAdmissionResult.SkipGitIgnore;
        }

        if (IsCodeFile(fileName))
        {
            if (kinds.HasFlag(GrantFileKinds.Code))
            {
                return FileAdmissionResult.Read;
            }

            return FileAdmissionResult.SkipKind;
        }

        if (GrantTextFiles.IsTextFile(fileName))
        {
            if (kinds.HasFlag(GrantFileKinds.Text))
            {
                return FileAdmissionResult.Read;
            }

            return FileAdmissionResult.SkipKind;
        }

        return FileAdmissionResult.SkipOther;
    }

    public static bool IsBlockedFolder(string folderName, UserSkipAdditions user)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return false;
        }

        if (BuiltInSkips.ContainsFolder(folderName))
        {
            return true;
        }

        if (user.FolderNames.Any(candidate => string.Equals(candidate, folderName, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    public static bool IsSecret(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(extension))
        {
            var withoutDot = extension.StartsWith('.') ? extension[1..] : extension;
            if (BuiltInSkips.ContainsSecretExtension(withoutDot))
            {
                return true;
            }
        }

        if (BuiltInSkips.ContainsSecretName(fileName))
        {
            return true;
        }

        if (fileName.Equals("id_rsa", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("id_dsa", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("id_ecdsa", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("id_ed25519", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("id_rsa", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("id_dsa", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("id_ecdsa", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("id_ed25519", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (fileName.StartsWith(".env", StringComparison.OrdinalIgnoreCase)
            && !fileName.Equals(".env.example", StringComparison.OrdinalIgnoreCase)
            && !fileName.EndsWith(".env.example", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!fileName.Equals("appsettings.json", StringComparison.OrdinalIgnoreCase)
            && fileName.StartsWith("appsettings.", StringComparison.OrdinalIgnoreCase)
            && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var bare = Path.GetFileNameWithoutExtension(fileName);
        if (bare.Equals("secret", StringComparison.OrdinalIgnoreCase)
            || bare.Equals("secrets", StringComparison.OrdinalIgnoreCase)
            || bare.Equals("credential", StringComparison.OrdinalIgnoreCase)
            || bare.Equals("credentials", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static bool IsDatabase(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        var withoutDot = extension.StartsWith('.') ? extension[1..] : extension;
        return BuiltInSkips.ContainsDatabaseExtension(withoutDot);
    }

    public static bool IsLockFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return BuiltInSkips.ContainsLockName(fileName);
    }

    public static bool IsMinified(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return BuiltInSkips.MinifiedSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsCodeFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        if (IsMinified(fileName))
        {
            return false;
        }

        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(extension) && BuiltInSkips.ContainsCodeExtension(extension))
        {
            return true;
        }

        if (BuiltInSkips.ContainsExactCodeName(fileName))
        {
            return true;
        }

        return false;
    }
}
