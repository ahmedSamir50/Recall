Status: decided-absent

## Code files

Task 2.2.6 is not started. A code file is text in `items_text` at 512 when that task is built. There is no separate code collection and no `code:` query prefix. `GrantFileKinds` has no Code flag today.

## Skips

A secret, database, lock file, or skip folder stays skipped even when a gitignore exception would include it. The Preferences skip screen is part of the same task and is not drawn.

## Not in src

Not in src. A grant can include code, beside text, images, video, and audio. The flag value is 16, and All includes it. There is no code modality. A code file is embedded with the text encoder into `items_text` at 512, with `title: {file name} | text: {content}`. Search does not require a `code:` prefix. Opening a hit opens the file. Extensions are `.asm`, `.astro`, `.bash`, `.bzl`, `.c`, `.cake`, `.cc`, `.cjs`, `.clj`, `.cljs`, `.cmake`, `.cpp`, `.cr`, `.cs`, `.csx`, `.cu`, `.cuh`, `.cxx`, `.d`, `.dart`, `.elm`, `.erl`, `.ex`, `.exs`, `.f90`, `.fs`, `.fsx`, `.glsl`, `.go`, `.gql`, `.graphql`, `.groovy`, `.h`, `.hcl`, `.hh`, `.hpp`, `.hs`, `.hxx`, `.java`, `.jl`, `.js`, `.jsx`, `.kt`, `.kts`, `.lua`, `.m`, `.metal`, `.mjs`, `.ml`, `.mli`, `.mm`, `.nim`, `.nix`, `.php`, `.pl`, `.pm`, `.proto`, `.ps1`, `.purs`, `.py`, `.pyi`, `.pyw`, `.r`, `.rb`, `.re`, `.res`, `.rkt`, `.rs`, `.scala`, `.scm`, `.sh`, `.sol`, `.sql`, `.svelte`, `.swift`, `.tcl`, `.tf`, `.ts`, `.tsx`, `.vb`, `.vue`, `.wgsl`, `.zig`. Exact names, with any case, are `CMakeLists.txt`, `Containerfile`, `Dockerfile`, `Gemfile`, `GNUmakefile`, `Jenkinsfile`, `Justfile`, `Makefile`, `Podfile`, `Procfile`, `Rakefile`, `Vagrantfile`. Skip a name ending in `.min.js`, `.min.mjs`, `.min.cjs`, or `.min.css`. The grant labels are English `Code` and Arabic `كود`.

Not in src. Never read inside a folder whose name, in any case, is `node_modules`, `bower_components`, `jspm_packages`, `web_modules`, `venv`, `.venv`, `site-packages`, `__pycache__`, `.mypy_cache`, `.pytest_cache`, `.ruff_cache`, `.tox`, `DerivedData`, `Pods`, `Carthage`, `.gradle`, `vendor`, `third_party`, `third-party`, `.git`, `.svn`, `.hg`, `bin`, `obj`, `dist`, `build`, `out`, `target`, `coverage`, `htmlcov`, `.next`, `.nuxt`, `.turbo`, `.cache`, `.parcel-cache`, `.svelte-kit`, `.angular`, `.vs`, `.idea`, `.ssh`, `.aws`, `.azure`, `.gnupg`, `.kube`, or `.docker`. Any other folder whose name starts with a dot stays in scope when the grant includes it.

Not in src. Never read a file whose extension, in any case, is `pem`, `key`, `p12`, `pfx`, `p7b`, `p7c`, `jks`, `keystore`, `ppk`, `kdb`, `kdbx`, `psafe3`, `keychain`, `tblk`, `gpg`, `asc`, `pgp`, `crt`, `cer`, `der`, `p8`, `mobileprovision`, `ovpn`, `rdp`, or `wallet`. Never read a file whose name, in any case, is `id_rsa`, `id_dsa`, `id_ecdsa`, `id_ed25519`, or one of those plus `.pub`, or starts with `id_rsa`, `id_dsa`, `id_ecdsa`, or `id_ed25519`. Never read a name that starts with `.env` unless the name is `.env.example` or ends with `.env.example`. Never read `.netrc`, `.npmrc`, `.pypirc`, `.pgpass`, `.git-credentials`, `.htpasswd`, `htpasswd`, `wp-config.php`, `credentials.json`, `secrets.json`, `service-account.json`, `google-services.json`, or `GoogleService-Info.plist`. Never read a name shaped `appsettings.*.json` other than `appsettings.json`. Never read a file whose name without an extension is `secret`, `secrets`, `credential`, or `credentials`.

Not in src. Never read a file whose extension, in any case, is `db`, `sqlite`, `sqlite3`, `db3`, `sdb`, `s3db`, `sl3`, `sqlitedb`, `mdb`, `accdb`, `odb`, `mdf`, `ldf`, `ndf`, `realm`, `fdb`, `gdb`, `frm`, `ibd`, `dbf`, `db-wal`, `db-shm`, `sqlite-wal`, `sqlite-shm`, `crypt12`, `crypt14`, or `crypt15`. A `.sql` source file stays readable when code is granted.

Not in src. Never read `bun.lock`, `Cargo.lock`, `composer.lock`, `flake.lock`, `Gemfile.lock`, `go.sum`, `mix.lock`, `package-lock.json`, `Package.resolved`, `packages.lock.json`, `pnpm-lock.yaml`, `poetry.lock`, `pubspec.lock`, or `yarn.lock`.

Not in src. A file matched by a `.gitignore`, or by `.git/info/exclude`, between that file and the grant root is not read. A `!` exception applies. A secret, a database, a lock file, or a skip-folder match is still skipped when a gitignore exception would have included it. Recall does not run git and does not read an excludes file outside the grant.
