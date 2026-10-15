namespace Recall.Core.Scope;

public static class BuiltInSkips
{
    public static readonly string[] CodeExtensions =
    [
        ".asm", ".astro", ".bash", ".bzl", ".c", ".cake", ".cc", ".cjs", ".clj", ".cljs",
        ".cmake", ".cpp", ".cr", ".cs", ".csx", ".cu", ".cuh", ".cxx", ".d", ".dart",
        ".elm", ".erl", ".ex", ".exs", ".f90", ".fs", ".fsx", ".glsl", ".go", ".gql",
        ".graphql", ".groovy", ".h", ".hcl", ".hh", ".hpp", ".hs", ".hxx", ".java", ".jl",
        ".js", ".jsx", ".kt", ".kts", ".lua", ".m", ".metal", ".mjs", ".ml", ".mli",
        ".mm", ".nim", ".nix", ".php", ".pl", ".pm", ".proto", ".ps1", ".purs", ".py",
        ".pyi", ".pyw", ".r", ".rb", ".re", ".res", ".rkt", ".rs", ".scala", ".scm",
        ".sh", ".sol", ".sql", ".svelte", ".swift", ".tcl", ".tf", ".ts", ".tsx", ".vb",
        ".vue", ".wgsl", ".zig",
    ];

    public static readonly string[] ExactCodeNames =
    [
        "CMakeLists.txt", "Containerfile", "Dockerfile", "Gemfile", "GNUmakefile", "Jenkinsfile",
        "Justfile", "Makefile", "Podfile", "Procfile", "Rakefile", "Vagrantfile",
    ];

    public static readonly string[] SkipFolders =
    [
        "node_modules", "bower_components", "jspm_packages", "web_modules", "venv", ".venv",
        "site-packages", "__pycache__", ".mypy_cache", ".pytest_cache", ".ruff_cache", ".tox",
        "DerivedData", "Pods", "Carthage", ".gradle", "vendor", "third_party", "third-party", ".git",
        ".svn", ".hg", "bin", "obj", "dist", "build", "out", "target", "coverage", "htmlcov",
        ".next", ".nuxt", ".turbo", ".cache", ".parcel-cache", ".svelte-kit", ".angular", ".vs",
        ".idea", ".ssh", ".aws", ".azure", ".gnupg", ".kube", ".docker",
    ];

    public static readonly string[] SecretExtensions =
    [
        "pem", "key", "p12", "pfx", "p7b", "p7c", "jks", "keystore", "ppk", "kdb",
        "kdbx", "psafe3", "keychain", "tblk", "gpg", "asc", "pgp", "crt", "cer", "der",
        "p8", "mobileprovision", "ovpn", "rdp", "wallet",
    ];

    public static readonly string[] SecretNames =
    [
        ".netrc", ".npmrc", ".pypirc", ".pgpass", ".git-credentials", ".htpasswd", "htpasswd",
        "wp-config.php", "credentials.json", "secrets.json", "service-account.json",
        "google-services.json", "GoogleService-Info.plist",
    ];

    public static readonly string[] DatabaseExtensions =
    [
        "db", "sqlite", "sqlite3", "db3", "sdb", "s3db", "sl3", "sqlitedb", "mdb", "accdb",
        "odb", "mdf", "ldf", "ndf", "realm", "fdb", "gdb", "frm", "ibd", "dbf",
        "db-wal", "db-shm", "sqlite-wal", "sqlite-shm", "crypt12", "crypt14", "crypt15",
    ];

    public static readonly string[] LockNames =
    [
        "bun.lock", "Cargo.lock", "composer.lock", "flake.lock", "Gemfile.lock", "go.sum",
        "mix.lock", "package-lock.json", "Package.resolved", "packages.lock.json",
        "pnpm-lock.yaml", "poetry.lock", "pubspec.lock", "yarn.lock",
    ];

    public static readonly string[] MinifiedSuffixes =
    [
        ".min.js", ".min.mjs", ".min.cjs", ".min.css",
    ];

    public static readonly string[] SecretDisplayRows =
    [
        "id_rsa", "id_dsa", "id_ecdsa", "id_ed25519", ".env", "appsettings.*.json",
        "secret", "secrets", "credential", "credentials",
    ];

    public static bool ContainsFolder(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return SkipFolders.Any(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ContainsCodeExtension(string extensionWithDot)
    {
        if (string.IsNullOrEmpty(extensionWithDot))
        {
            return false;
        }

        return CodeExtensions.Any(candidate => string.Equals(candidate, extensionWithDot, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ContainsExactCodeName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        return ExactCodeNames.Any(candidate => string.Equals(candidate, fileName, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ContainsSecretExtension(string extensionWithoutDot)
    {
        if (string.IsNullOrEmpty(extensionWithoutDot))
        {
            return false;
        }

        return SecretExtensions.Any(candidate => string.Equals(candidate, extensionWithoutDot, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ContainsSecretName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        return SecretNames.Any(candidate => string.Equals(candidate, fileName, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ContainsDatabaseExtension(string extensionWithoutDot)
    {
        if (string.IsNullOrEmpty(extensionWithoutDot))
        {
            return false;
        }

        return DatabaseExtensions.Any(candidate => string.Equals(candidate, extensionWithoutDot, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ContainsLockName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        return LockNames.Any(candidate => string.Equals(candidate, fileName, StringComparison.OrdinalIgnoreCase));
    }
}
