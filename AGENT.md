# CorUI - Blazor UI Library + Native Hosts

Abstract

CorUI is a cross-platform UI system built on Blazor that cleanly separates reusable UI components from host-specific windowing and runtime concerns. The library (CorUI) provides a consistent component model, styling, and minimal service contracts for windowing and storage. Host projects (e.g., CorUI.macOS) adapt those contracts to their native platforms with custom window creation, dialog sheets, lifecycle coordination, and platform UX affordances. A shared TestApp provides the actual app UI and control demos and is consumed by multiple hosts: a native macOS app (real AppKit, not Catalyst) and a Blazor WebAssembly site. This design lets you:

- Develop UI once in Blazor components and run it natively or on the web
- Navigate to pages (routes) as window/dialog content via a simple service API
- Preserve native affordances (AppKit sheets, window chrome, vibrancy) without leaking host complexity into component code
- Keep imperative behaviors (e.g., dialog dismissal) available via DI services without needing JavaScript
- Gate UI visibility on the embedded Blazor lifecycle (Ready signal) with a deterministic fallback to avoid perceived hangs

Repository structure (high level):

- CorUI (library)
  - Blazor UI component library (controls in `CorUI/Controls`, styles in `CorUI/wwwroot/css`).
  - Platform-agnostic service contracts in `CorUI/Services`:
    - `IWindowService` – open windows and dialogs by navigating to a Blazor route (`ContentPath`).
    - `IViewStorage` – simple key/value storage abstraction.
    - `IDialogControlService` – imperative close for the active dialog/sheet.
  - Built-in web implementations:
    - `WebWindowService` (WASM): opens new tabs/windows and presents dialogs using the `Dialog` component.
    - `WebViewStorage` (WASM): persists via `localStorage` (with in-memory fallback when JS is unavailable).
  - UI primitives used by hosts and demos: `Dialog.razor`, `Portal.razor`, etc.

- CorUI.macOS (host)
  - Real AppKit macOS host (not MAUI), targets `net9.0-macos26`.
  - Custom WebView pipeline: `BlazorWebView`, `AppKitWebViewManager`, `AppUrlSchemeHandlerWithManager`.
  - Windowing:
    - `BlazorWindowController` creates native `NSWindow` and shows it when Blazor signals Ready, with a 3s fallback so windows always appear promptly.
  - Dialogs (native sheets):
    - `MacWindowService` implements `IWindowService` and `IDialogControlService`.
    - `OpenDialog(Dialog)` attaches a native sheet to the key window, hosts a `BlazorWebView` pointed at `Dialog.ContentPath`, and shows when Ready (with 3s fallback).
    - Keyboard dismissal is robust: Esc and Command-. end the sheet.
    - `CloseActiveDialog()` ends the currently tracked sheet (no JS; C# only).
  - DI entrypoint: `CorUI.macOS/ServiceCollectionExtensions.AddMacOS()` wires native services (`IWindowService`, `IDialogControlService`, `IViewStorage`).

- TestApp (showcase UI)
  - The Blazor UI used across hosts. Contains pages and demos in `TestApp/Components/Demos` and `TestApp/Pages`.
  - Example: `WindowServiceDemo` shows how to call `IWindowService.OpenWindow` and `IWindowService.OpenDialog`. `ShowcaseDialog.razor` demonstrates C# service-based closing via `IDialogControlService`.

- TestApp.CorUI (host apps)
  - Multi-target host project for native platforms (macOS today, Windows planned). References `TestApp` and `CorUI.macOS`.
  - Contains platform bootstraps under `Platforms/` (e.g., macOS App start) and a host `wwwroot`.

- TestApp.Web (WASM)
  - Separate Blazor WebAssembly host (recommended) that references `TestApp` + `CorUI` and registers web DI (`AddCorUIWeb`).
  - Serves `wwwroot/index.html` with `<script src="_framework/blazor.webassembly.js"></script>`.

Key flows

- Open a new native window (macOS): `IWindowService.OpenWindow(new Window { ContentPath = "/route", Title = "Title" })`.
- Open a native dialog sheet (macOS): `IWindowService.OpenDialog(new Dialog { ContentPath = "/route", Title = "Dialog" })`.
- Close active dialog from Razor (all platforms): inject `IDialogControlService` and call `CloseActiveDialog()`.
- Web dialogs (WASM): the `WebWindowService` raises a presenter event; the `Dialog` component is used to display modal content.

Notes for hosts

- macOS host delays visibility until the embedded Blazor app signals Ready, with a 3s fallback so windows/sheets don’t appear stuck.
- Dialog sheets are attached to the key window and respect Escape/Command-. dismissal.
- Storage: native hosts provide their own `IViewStorage`; web host uses `WebViewStorage` (localStorage).