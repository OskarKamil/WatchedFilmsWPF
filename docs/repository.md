# Repository files and verification

Git should store the files needed to build, understand, test and publish the application. It should exclude generated output, local preferences, credentials and personal collection data.

## Folders added by the refactor

| Folder                                          | Purpose                                                                                                                                                         | Track in Git? |
|-------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------|
| `Source/Models`                                 | Collection metadata, types, delimiter choices and immutable datasource snapshots                                                                                | Yes           |
| `Source/Persistence`                            | CSV readers/writers, JSON storage and coordination of the two formats                                                                                           | Yes           |
| `Source/Statistics`                             | SQL templates, query definitions, execution and results                                                                                                         | Yes           |
| `Source/ViewModels` and `Source/Views`          | Statistics state, refresh behavior, controls and record-grid construction                                                                                       | Yes           |
| `Verification`                                  | Developer regression scripts and optional UI preview renderer                                                                                                   | Yes           |
| `Verification/Fixtures/Collections`             | Two fictional CSV examples per collection type, with JSON companions and sample statistics; see [fixture guide](../Verification/Fixtures/Collections/README.md) | Yes           |
| `docs`                                          | Architecture, file-format and repository documentation, plus the project TODO                                                                                   | Yes           |
| [`docs/development-log.md`](development-log.md) | Informal notes on changes across versions                                                                                                                       | Yes           |
| `docs/screenshots`                              | README images, historical screenshots and outtakes                                                                                                              | Yes           |
| `artifacts`                                     | Generated preview images or local validation output                                                                                                             | No            |

`Verification` is not part of the application's normal startup. Its PowerShell scripts exercise datasource queries, delimiter round-trips, metadata lifecycle, refresh behaviour and actual WPF controls. They fail with an error when an assertion fails. The preview script renders controls offscreen for layout inspection. See [architecture.md](architecture.md#verification) for the check commands.

After building, optionally generate local preview images:

```powershell
pwsh -NoProfile -STA -File Verification/RenderStatisticsPreview.ps1
```

Images go to the ignored `artifacts/previews` directory. Override the destination with `-OutputDirectory` if needed. Commit the script, not its generated images.

## Project and application files

| Files                                                                 | Policy and reason                                                                                                                      |
|-----------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------|
| `App.xaml`, `App.xaml.cs`                                             | Keep: application startup and resources. This project uses XAML, not an `App.xml` file.                                                |
| Root `AssemblyInfo.cs`                                                | Keep: handwritten WPF `ThemeInfo` attributes. Generated assembly-info files inside `obj` are excluded.                                 |
| `WatchedFilmsTracker.csproj`                                          | Keep: framework, NuGet dependency versions, resources and build/publish configuration.                                                 |
| `WatchedFilmsTracker.sln`                                             | Keep: the solution opened by Visual Studio and used by build commands.                                                                 |
| `.gitignore`                                                          | Keep: shared rules for excluding local/generated files.                                                                                |
| `Properties/PublishProfiles/FolderProfile.pubxml`                     | Keep: this inspected profile has relative output paths and no credentials; it defines the shared Windows folder-publish configuration. |
| `*.pubxml.user`, `*.csproj.user`, `.vs`, `.idea`                      | Exclude: user/machine-specific settings and IDE caches.                                                                                |
| `bin`, `obj`, `publish`, `artifacts`                                  | Exclude: build, publish and verification outputs. NuGet dependencies are restored from the project configuration.                      |
| `settings.conf`, root `MyFilms.csv`, `MyData`, root `*.metadata.json` | Exclude: local program preferences and working collection data.                                                                        |
| `Assets`, `Verification/Fixtures`, `docs/screenshots`                 | Keep: application resources, intentional test/example data and documentation images.                                                   |

Other publish profiles remain ignored until reviewed. Do not add profiles containing passwords, tokens, private server paths or account identifiers. The `.pubxml.user` companion is not shareable. The shared folder profile is explicitly allowed through `.gitignore`.

Do not ignore all CSV or JSON files: intentional fixtures and example metadata can belong in the repository. The rules target runtime data locations instead.

Adding a pattern to `.gitignore` does not stop tracking an existing file. For an already tracked local file, use `git rm --cached -- path` to remove it from the index while retaining its local copy. This does not erase older commits. See the [Git ignore documentation](https://git-scm.com/docs/gitignore) and Microsoft's [publish-profile documentation](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/visual-studio-publish-profiles) for the underlying behaviour.
