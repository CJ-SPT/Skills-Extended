# Web configuration checks

Run from the repository root, with deployment disabled:

```powershell
dotnet run --project Tests/SkillsExtended.WebRegression -c Release -p:DeploySkillsExtended=false
dotnet run --project Tests/SkillsExtended.Regression -c Release -p:DeploySkillsExtended=false
dotnet build Server/SkillsExtended.Server/SkillsExtended.Server.csproj -c Release -p:DeploySkillsExtended=false
```

The web checks reference the production server project. They use copied configuration
fixtures, an in-memory profile store, a temporary filesystem directory, and Blazor's
offline renderers. They do not launch SPT or the game, deploy files, or touch installed
profiles/configuration.

Coverage includes all shipped settings, nested draft isolation, dirty state, discard,
numeric validation and culture, concurrent/external edits, staged replacement failures,
rollback and retry, PMC/Scav separation, fractional progress preservation, profile
conflicts, and failed profile saves. Real component event handlers are dispatched to
verify that input validation and switches update only the draft.

Authorization checks discover every production page, verify its SPT `Administrator`
policy, and check the administrator claim grants access. Actual `AuthorizeRouteView`
renders verify anonymous visitors and signed-in non-admin users cannot instantiate
the pages or their configuration/profile editor layout.

Actual layout and page components are also rendered into `bin/Release/net10.0/rendered`.
Inspect those fixtures at desktop and mobile widths using Playwright:

```powershell
# Set these only when Playwright or Chromium are outside the usual installation paths.
$env:PLAYWRIGHT_MODULE_PATH = 'C:\path\to\node_modules\playwright'
$env:PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH = 'C:\path\to\chrome-headless-shell.exe'
node Tests/SkillsExtended.WebRegression/CheckLayout.cjs
```

This checks viewport overflow, centered content, accessible field labels, keyboard
entry, and visible save controls at 2560, 1440, 768, and 390 pixels. Screenshots are
written beside the fixtures. These are static rendering checks; they do not claim
acceptance of a live SPT browser circuit or actual gameplay/profile persistence.

## Live acceptance

After installing and restarting on your normal schedule:

- Open Skills Extended while signed out and verify SPT prompts for sign-in. A
  non-admin account must be denied; an administrator must be able to open the
  overview, skill settings, profile skill editor, and release notes directly.
- Edit settings on multiple pages; verify drafts persist while navigating and another
  browser session sees only saved values. Check save, discard, invalid input, conflict
  feedback, and leaving/closing with pending edits.
- Check navigation and form scrolling at desktop and mobile widths. Verify the menu,
  search fields, all map tables, and custom weapon IDs.
- With the selected game client closed, edit one PMC skill and one Scav skill using
  the separate review/apply workflow. Verify the selected profile, preserved unrelated
  skills, and persistence after reloading. Cancel a profile/character switch and verify
  that selection and draft remain intact.

Configuration saves stage both files and roll back reported replacement failures.
The two files are not a filesystem-wide crash-atomic transaction. If rollback itself
fails, the error identifies retained backup files for recovery. Profile saves use
SPT's save API and verify the affected values on disk before reporting success.
