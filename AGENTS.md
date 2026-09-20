# Anchor PDF — Developer & Agent Guidelines

Guidelines for Anchor PDF (.NET 10 / WPF) and reusable C# desktop standards.

---

# Part 1: Core Developer Standards & UI Preferences

## 1. Core Philosophy & Working Style
* **Readability Over Cleverness**: Obvious construction over dense one-liners. Readability outranks brevity.
* **Incremental Development**: Build in small, verifiable steps. One feature working before the next.
* **Focused Changes**: Change only what was requested. Avoid unprompted refactors or bloated dependencies.
* **Plan Before Code**: For non-trivial tasks, outline the approach before writing code.
* **Pure Native WPF Presentation**: Avoid third-party UI suites (e.g. Wpf.Ui, MaterialDesign). Use pure, modern, hand-crafted XAML styles and vector geometry for complete layout control and zero runtime bloat.
* **Selective Build Validation**: Do not run `dotnet build` for small, low-risk changes (e.g. text edits, designer visibility toggles, or minor property updates). Run builds when completing substantial logic overhauls, introducing new dependencies, or when verifying milestones.

## 2. Project Architecture (MVVM)
* **Separation of Concerns**:
  * `Views/`: XAML layout and minimal code-behind. Code-behind contains only UI-specific wiring (e.g. drag & drop). Zero business logic or file operations.
  * `ViewModels/`: Presentation state and commands using `CommunityToolkit.Mvvm`. No direct UI control references.
  * `Models/`: Pure domain data classes and records.
  * `Services/`: Pure C# logic classes. Take data in, return data out. Zero UI/WPF dependencies.
* **Topic-Based Naming**: Logic classes are named after their domain topic (`PdfToImageService`, `ImageToPdfService`), never generic names like `Helper` or `Utils`.
* **One Class Per File**: File name matches the class name exactly.

## 3. C# Code Style & Readability
* **Explicit Types**: Always use explicit types (`string`, `FileInfo`, `ImageItem`) instead of `var`.
* **Allman Bracing**: Braces are always placed on their own line. Always use braces, even for single-line `if`/`foreach` blocks.
* **Flat Flow / Early Return**: Test failure and edge cases first with guard clauses; return early to eliminate nested `if` blocks.
* **Short Methods**: Keep methods focused on one job (under ~50 lines). Split if doing multiple tasks.
* **Naming Conventions**:
  * `PascalCase` for classes, methods, properties, and constants.
  * `camelCase` for local variables and parameters.
  * `_camelCase` with leading underscore for private fields.
  * Full words over abbreviations (`pdfDocumentPath` instead of `docPth`).
  * Booleans read as questions (`hasSelectedPdf`, `isConverting`, `canExecute`).

## 4. Organization, Comments & Documentation
* **Regions**: Group class members using `#region` blocks with plain English titles:
  * `#region Fields and Constructor`
  * `#region Properties`
  * `#region Relay Commands` (or `#region Public Methods`)
  * `#region Private Helper Methods`
* **XML Comments**: Concise XML doc comments (`/// <summary>`) on public methods only. Do not place XML comments on class declarations to keep classes lean.
* **Inline Comments**: Explain *why*, not *what*. Never add comments on closing braces or decorative banner lines.

## 5. Non-Blocking I/O & Error Handling
* **Async by Default**: All PDF rasterization, file scanning, and PDF compilation must be `async Task`. Never call `.Result` or `.Wait()`.
* **Cancellation & Progress**: Long-running operations must accept `CancellationToken` and report progress via `IProgress<T>`.
* **Batch Loop Isolation**: In file/page loops, catch errors per item so one corrupt item does not abort the entire batch operation.
* **No Silent Swallowing**: Never swallow exceptions silently; log or report user-friendly error messages.
* **Direct Stream Saving (No `%TEMP%` Staging)**: Stream output directly into destination `FileStream(..., FileMode.Create, FileAccess.Write, FileShare.None)` rather than staging in `%TEMP%` and calling `File.Move`. This avoids NTFS ACL inheritance issues and antivirus file locks on newly created temp files.
* **Destination Overwrite Safeguard**: When creating or overwriting an output file, check `File.Exists` and clear `FileInfo.IsReadOnly = false` before opening `FileStream` to prevent false access denied errors on existing files.
* **Read-Only Source Policy**: Original user source files (input PDFs and images) must never be modified, overwritten, or opened with exclusive locks (`FileShare.ReadWrite` required for reading).

## 6. Modern Minimalistic Light UI Standards
* **Color Palette**:
  * Window Background: Pure white (`#FFFFFF`).
  * Top Navigation Bar: Pure white (`#FFFFFF`) with subtle bottom border (`#E5E7EB`).
  * Card / Container Background: Soft off-white (`#F9FAFB`) or white (`#FFFFFF`) with subtle border (`#E5E7EB`).
  * Text: Deep slate / charcoal (`#111827` / `#1F2937`) for high-contrast, comfortable reading. Secondary text: `#6B7280`.
  * Accent: Modern industrial blue (`#2563EB`) for brand logo, active indicators, and count badges.
  * Action Buttons: Soft modern grey (`#F3F4F6` with `#111827` text and `#E5E7EB` border).
  * Primary Action Button: Modern dark slate (`#111827` background, white text, hover `#1F2937`).
* **Visual Polish & Layout**:
  * Top Flat Underline Navigation: Horizontal tab header with active blue indicator line (`#2563EB`) on the active tab.
  * Centered Page Headers without Icons: Clean, elegant typography with centered 24px bold title and subtle 13px description subtitle.
  * 50/50 Split Configuration Cards: Source file selection on the left, configuration/options dropdowns on the right.
  * Clean UI Surface: No mention of underlying frameworks (.NET, WPF, libraries) or technical telemetry in the user-facing interface. Keep the experience minimalist, modern, and uncluttered.
  * Flat controls with subtle corner rounding (`CornerRadius="6"`, `"8"`, or `"10"`).
  * Generous whitespace: 12px/16px/24px padding and margins. Dynamic `Grid` (`*` and `Auto`) and `StackPanel` sizing.
  * Vector Icons Over Text Glyphs: Use vector `Path` geometry (`M ... Z`, `StrokeThickness="1.8"`, `StrokeStartLineCap="Round"`, `StrokeLineJoin="Round"`).
  * Dashed Empty State Cards: When a list/grid has no items, replace the empty space with a subtle dashed or soft container containing a vector icon, explanatory message, and an action button.
  * Pinned Bottom Action Bar: Status badge on the left (green check circle) + prominent dark action button on the right.
  * User Feedback: Disable action buttons during execution (`IsConverting`). Provide responsive progress percentages and live status messages.

## 7. Visual Studio XAML Designer & Architecture Standards
* **Multi-View Overlap Prevention**:
  * When multiple pages share the same `Grid` container or mutually exclusive controls exist in the same cell (empty-state card vs loaded list), always use design-time visibility attributes (`d:Visibility="Visible"` on the default active page/view, and `d:Visibility="Collapsed"` on non-active pages and loaded/converting states).
  * This prevents the Visual Studio XAML Designer from rendering all pages and overlapping states simultaneously on top of each other.
* **Design-Time DataContext**:
  * Use `d:DataContext="{d:DesignInstance Type=viewmodels:MainViewModel, IsDesignTimeCreatable=False}"`.
  * Setting `IsDesignTimeCreatable=False` provides full IntelliSense for bindings without running runtime constructors or instantiating native services in the designer host.
* **Explicit Type Syntax in Markup Extensions**:
  * Always use explicit type syntax in XAML relative bindings: `AncestorType={x:Type ItemsControl}`. Avoid bare type names (`AncestorType=ItemsControl`) which trigger resolution warnings in Visual Studio.
* **ComboBox Enum Binding**:
  * Never instantiate enum values as direct XML child tags inside a `ComboBox` (e.g. `<models:SplitMode>AllPages</models:SplitMode>`), because enums lack parameterless constructors and trigger designer errors.
  * Expose an `IReadOnlyList<TEnum> Available...` property on the ViewModel and bind `<ComboBox ItemsSource="{Binding Available...}" SelectedItem="{Binding Selected..., Mode=TwoWay}" />`.
* **64-Bit Windows Architecture (x64)**:
  * Anchor PDF targets 64-bit Windows (`win-x64` / AMD64) exclusively.
  * In `Anchor PDF.csproj`:
    * **Debug Configuration**: Uses `PlatformTarget=AnyCPU` with `<Prefer32Bit>false</Prefer32Bit>`. On 64-bit Windows, this always runs as a native 64-bit process, but allows the Visual Studio XAML Designer (`WpfSurface.exe`) to load local project assemblies and custom elements without throwing "Some custom elements have not been built".
    * **Release Configuration**: Explicitly compiles directly as native `win-x64` with `<PlatformTarget>x64</PlatformTarget>` and `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`.
  * In `app.manifest`: Explicitly declare `processorArchitecture="amd64"`.
  * In `App.xaml.cs`: Verify `Environment.Is64BitOperatingSystem` on startup with user-friendly error handling if run on an unsupported 32-bit OS.

---

# Part 2: Anchor PDF Architecture & Engines

## 1. Application Purpose
Anchor PDF is a high-speed, offline, privacy-first 64-bit desktop utility for converting PDFs to high-resolution images, compiling image sequences into structured PDF documents, merging, splitting, reorganizing, and compressing PDF files with zero telemetry and zero external server dependencies.

## 2. Core Conversion & Processing Engines
* **PDF to Image & Page Rendering**: `PDFtoImage` (Google PDFium wrapper, fast, battle-tested, MIT/Apache permissive).
* **PDF Document Processing**: `PDFsharp 6` (Clean, pure .NET PDF composition, merging, splitting, page reordering, and compression, MIT license).

## 3. Supported Feature & Format Matrix
* **PDF to Images**:
  * Source: `.pdf`
  * Output Formats: PNG (`.png`), JPEG (`.jpg`), WebP (`.webp`)
  * Resolution Presets: 72, 96, 150, 200, 300, 600 DPI
  * Features: Visual page thumbnails, individual page checkboxes, select all/deselect all, direct image extraction or consolidated `.zip` archive.
* **Images to PDF**:
  * Source Images: `.png`, `.jpg`, `.jpeg`, `.webp`, `.bmp`
  * Output: Standard `.pdf` document
  * Layout Presets: Fit to Image, A4 (Portrait/Landscape/Auto), Letter (Portrait/Landscape/Auto) with configurable margins.
  * Features: Drag & drop reordering, move up/down, remove item, clear all.
* **Merge PDF**:
  * Source: Multiple `.pdf` documents
  * Output: Consolidated `.pdf` document
  * Features: Reorder documents up/down, page count indicators, safe batch merging.
* **Split PDF**:
  * Source: `.pdf`
  * Extraction Modes:
    * *Extract Every Page*: Each page saved as a separate PDF.
    * *Custom Range*: User-defined ranges (e.g. `1-3, 5, 8-10`), with optional single-file combination.
    * *Fixed Interval*: Chunks of *N* pages per document.
* **Reorganize PDF**:
  * Source: `.pdf`
  * Operations: Drag & drop / button reordering, rotate 90° clockwise/counter-clockwise, duplicate page, delete page, reset to original.
  * Previews: Live visual thumbnail rendering of each page.
* **Compress PDF**:
  * Source: `.pdf`
  * Compression Presets:
    * *Balanced*: Flate compression with standard stream optimization.
    * *Maximum Compression*: Aggressive stream optimization and object flate encoding.
    * *Lossless Stream Optimization*: Structural and stream deduplication with zero quality loss.
  * Metrics: Live calculation of original size, compressed size, and percentage saved.
