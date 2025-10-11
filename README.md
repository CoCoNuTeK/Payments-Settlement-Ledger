# PersistentState AllowUpdates + Interactive WASM Repro

Minimal reproduction for the Blazor Web App bug where `[PersistentState(AllowUpdates = true)]` fails to hydrate state when navigating between pages rendered with `@rendermode InteractiveWebAssembly`.

## Environment

- .NET SDK: **10.0.0-rc.1** (arm64 build)
- Host OS: macOS Sonoma 14.xx (Apple Silicon)
- Browser: Google Chrome (desktop) with DevTools console open

## Reproduce

```bash
git clone https://github.com/CoCoNuTeK/-PersistentState-AllowUpdates-true-issue-when-InteractiveWebAssembly.git
cd -PersistentState-AllowUpdates-true-issue-when-InteractiveWebAssembly/PersistenceStateIssue
dotnet run
```

Open <http://localhost:5041/persist-demo-a>. For clarity:

- Both `PersistDemoA` and `PersistDemoB` use `@rendermode InteractiveWebAssembly`
- Each component delays for 2 seconds, then stores a random 1‑100 value via `[PersistentState(AllowUpdates = true)]`
- Navigation buttons use `NavigationManager.NavigateTo(..., forceLoad: false)` (enhanced navigation)

## What to check

1. Load `/persist-demo-a` and then click through to `/persist-demo-b` once. The first pass should hydrate correctly and emit console lines such as:
   ```
   [PersistDemoA] No persisted random value found. Simulating load before generating a new value.
   [PersistDemoA] Generated random value '72'.
   [PersistDemoB] No persisted random value found. Simulating load before generating a new value.
   [PersistDemoB] Generated random value '25'.
   ```
2. After those initial messages have appeared (both pages now have values), continue switching between the two pages with the on-page buttons. Watch the DevTools console and the Network tab:
   - The prerender request fires on every enhanced navigation (expected).
   - The client-side instance also reruns `OnInitializedAsync` and generates a different random value, proving the persisted snapshot was ignored.
   - The numbers you see rendered in the browser differ from those returned by the prerender.
3. For comparison, set `forceLoad: true` or switch the render mode to `InteractiveServer`; in those scenarios the previously generated value is reused, so the issue doesn’t occur.

Meaning two issues:
1. `[PersistentState(AllowUpdates = true)]` is ignored when `@rendermode InteractiveWebAssembly` participates in enhanced navigation.
2. Enhanced navigation re-prerenders the page but also re-executes the WASM lifecycle, so prerendering is effectively lost after the first load.

I don’t see these problems if every navigation forces a full reload or if `@rendermode InteractiveServer` is used with the same `[PersistentState(AllowUpdates = true)]`.
