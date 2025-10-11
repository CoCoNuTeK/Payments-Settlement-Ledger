# Payments Ledger Blazor Agent Guide

This guide covers how we author UI for the Payments & Settlement Ledger Blazor Web App. It replaces the older WASM-only guidance. Follow it whenever you add or update components, pages, or supporting assets.

## Project Template & Render Modes
- We use the **Blazor Web App** template (`blazor` CLI shorthand). It hosts Razor components that can render statically on the server, stream results, or switch to client interactivity.
- Default render mode is **`@rendermode InteractiveAuto`** for interactive components. Auto starts with interactive SSR and swaps to WebAssembly once the runtime downloads. Prefer it unless a component must stay server-only or strictly client-only.
- Available render modes and when to use them:
  - `@rendermode InteractiveServer`: WebSockets interactivity from the server when latency must be minimal and no WASM payload is desired.
  - `@rendermode InteractiveWebAssembly`: Full client execution when offline support or client-side resources are required.
  - `@attribute [StreamRendering]`: Optional for static SSR pages to stream placeholder content (for example, `Loading...`) before data resolves. Avoid using it on SEO-sensitive pages because crawlers may capture the placeholder.
  - Omit `@rendermode` for purely static SSR content.
- Components using Interactive WebAssembly or Auto render modes must live in the `.Client` project. The server project can use any mode that fits.

## Solution Layout
- **Server project** (`PaymentsLedger.Blazor`):
  - `Components/` holds server-side Razor components. `Components/Pages/` contains routable pages with `@page`.
  - `Components/Layout/` stores layout scaffolding such as `MainLayout.razor`, `NavMenu.razor`, and `ReconnectModal.razor`.
  - `Components/Routes.razor` configures routing via the `Router` component.
  - `App.razor` hosts `<head>` markup, the `Routes` component, and script tags.
  - `_Imports.razor` carries shared namespaces for server components; add or adjust directives when you introduce new folders.
  - `wwwroot/` exposes static assets; treat it as the server web root.
  - `Program.cs` bootstraps Razor components with `AddRazorComponents`, `AddInteractiveServerComponents`, and `AddInteractiveWebAssemblyComponents`. Routing is registered via `MapRazorComponents` with the `App` root and `AddInteractive*RenderMode` calls.
- **Client project** (`PaymentsLedger.Blazor.Client`):
  - `Pages/` houses routable WASM components that require WebAssembly or Auto render modes.
  - `wwwroot/` is the client web root for static assets downloaded to browsers.
  - `Program.cs` configures the WebAssembly host and dependency registrations that only run in the browser context.
- Keep namespaces aligned with folder structure. Use `_Imports.razor` or explicit `@namespace` statements when moving files.

## Component Authoring Standards
- Components and files use PascalCase (`TransactionHistory.razor`). Routes map to kebab-case (`@page "/transaction-history"`).
- Directive order (no blank lines between directives, blank line before markup):
  1. `@page`
  2. `@rendermode`
  3. `@attribute` directives
  4. `@using` statements (System → Microsoft → third-party → solution, alphabetized)
  5. Remaining directives alphabetized (`@implements`, `@inherits`, `@inject`, etc.)
- Place markup first, then a single `@code` block (or a code-behind partial class). Use `[Parameter]` auto-properties, `[EditorRequired]` for required values, and avoid mutating parameters after first render.
- Capture component references with `@ref` only when events/parameters cannot solve the problem.

## Navigation & Routing
- Register nav links in `NavMenu.razor` to expose new routes. Use `NavLink` so the active state updates automatically.
- Prefer asynchronous lifecycle methods (`OnInitializedAsync`, `OnParametersSetAsync`) when loading data.
- When composing shared layouts, keep them in `Components/Layout` and ensure they are render-mode agnostic unless they host interactive child content.

## Tailwind-Only Styling
- Styling is exclusively Tailwind utility classes. Do not reintroduce Bootstrap or scoped `.razor.css` files.
- `Styles/tailwind.css` imports Tailwind (`@import "tailwindcss"`) and any shared utilities. The generated CSS lives at `wwwroot/css/tailwind.css`.
- Development workflow:
  - `npm run tailwind:watch` for live builds.
  - `npm run tailwind:build` for production builds.
  - `npm run dev` runs Tailwind watch alongside `dotnet watch` (HTTPS profile). Local cert is already trusted via `dotnet dev-certs https --trust`.
- Use Tailwind classes directly in Razor markup. Consolidate reusable patterns via partial components or Tailwind `@apply` helpers in the shared stylesheet when duplication grows.

## Data & Mocking
- Until backend APIs are integrated, store sample payloads in the appropriate `wwwroot` folder (`server` or `.Client`). Fetch them over HTTP to mimic production flows.
- Maintain mock data near the feature consuming it (for example, `wwwroot/data/transactions.json`). Document temporary datasets inline if they are not obvious.

## Documentation & Comments
- Keep XML comments for public APIs concise. Add inline comments only to clarify non-obvious decisions.
- Reuse verbiage from this guide when documenting new feature folders so future agents inherit the same expectations.

## Authorization (Upcoming)
- We are refreshing the authorization model for the Blazor Web App. Treat the current guidance as provisional; a dedicated section will be published soon. Until then, keep auth-aware code changes minimal and coordinate with the platform team.

Adhering to this guide keeps the Blazor Web App consistent across render modes, Tailwind styling, and project organization.
