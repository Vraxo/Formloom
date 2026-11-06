# Windows desktop smoke test

Run this after desktop interaction/save changes. Linux tests do not launch Photino or verify native focus, dialogs or window lifecycle. Record the commit, Windows version, .NET SDK version and pass/fail notes; do not infer completion from CI alone.

```powershell
dotnet build Formloom.slnx --configuration Release
dotnet run --project src/Formloom.Desktop/Formloom.Desktop.csproj --configuration Release --no-build
```

- [ ] Generate default prompt / seed 42 / variation 35%; preview appears and controls re-enable.
- [ ] Generate identical inputs again; image is unchanged. Change seed/variation and generate; result changes coherently.
- [ ] Edit prompt/seed/variation immediately before Generate; visible values are used.
- [ ] Enter an unsupported prompt, then an invalid seed; useful errors appear and the last image/metadata remain.
- [ ] Double-click Generate and attempt Save/Randomize while generating; no duplicate jobs, stale preview or crash.
- [ ] Edit seed after successful generation without generating again; Save suggests the successful image's seed and saves its actual image.
- [ ] Cancel the native save dialog; image remains and another Generate/Save works.
- [ ] Save to a writable folder, overwrite an existing PNG, and open it in an independent viewer; verify dimensions and image.
- [ ] Exercise an inaccessible destination where possible; failure is actionable, image remains, and another save works.
- [ ] Tab through controls: visible focus, useful labels and no keyboard trap. Operate Generate/Save using the keyboard.
- [ ] Resize narrow/wide and check controls/preview remain usable; close and relaunch twice without startup/shutdown errors.

The headless desktop tests exercise session guards, failure recovery, metadata snapshots, cancellation results and real file writing/replacement. They do not prove native dialog cancellation, OS overwrite prompts, real disk-full behavior, rendering responsiveness or accessibility. Those remain manual checks above.
