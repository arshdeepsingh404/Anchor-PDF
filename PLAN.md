# Anchor PDF - Implementation Plan & Roadmap

## Overview
Anchor PDF is a modern Windows 11 Fluent WPF application for high-performance two-way conversion:
1. **PDF to Images**: Rasterize PDF pages into PNG, JPEG, WebP, or BMP at 96/150/300 DPI, supporting page range filtering and export to folder or direct ZIP.
2. **Images to PDF**: Combine multiple images into a single structured PDF document with drag-and-drop page reordering, page sizing presets (Fit to Image, A4, US Letter), and orientation controls.

---

## Architectural & UX Decisions
- **UI Framework**: Pure native WPF XAML (zero external UI library dependencies).
- **Design System**: Minimalist light SaaS aesthetic matching RoboFile Fusion:
  - Left navigation sidebar (`Width="220"`, `#F9FAFB` with active `#E5E7EB` pill triggers).
  - High-contrast typography (`#111827` primary, `#6B7280` secondary) on `#FFFFFF` canvas.
  - 3-column metric stats strips on conversion pages.
  - Vector `Path` SVG geometry icons (`StrokeThickness="1.8"`).
  - Pinned bottom action bar with circular status badge and primary action button.
- **Engines**: 
  - `PDFtoImage` (Google PDFium wrapper) for high-performance PDF rasterization (100% free, MIT/Apache).
  - `PdfSharp` for fast, lightweight PDF generation (MIT).
- **MVVM Framework**: `CommunityToolkit.Mvvm` (Microsoft source-generator powered).
- **Core Principles**:
  - Thread-safe asynchronous conversion with live progress bars and cancellation support.
  - File drag-and-drop zones for both input PDF and images.
  - Page reordering for image-to-PDF generation.

---

## Phased Implementation Roadmap

### Phase 1: Solution Restructuring & Dependencies
- [x] Update `Anchor PDF.csproj` to `<OutputType>WinExe</OutputType>`.
- [x] Install NuGet packages:
  - `WPF-UI` (4.3.0)
  - `CommunityToolkit.Mvvm` (8.4.2)
  - `PDFtoImage` (5.4.0)
  - `PDFsharp` (6.2.4)
- [x] Remove placeholder `Class1.cs`.
- [x] Scaffold `App.xaml` and `App.xaml.cs` with Fluent theme dictionaries and service registration.
- [x] Verify initial build (`0 Errors, 0 Warnings`).

### Phase 2: Core Conversion Engines & Services
- [x] Implement models (`ConversionProgress.cs`, `ImageItem.cs`, `PdfToImageOptions.cs`, `ImageToPdfOptions.cs`, `PagePresets.cs`, `ImageFormatType.cs`).
- [x] Implement `IPdfToImageService` & `PdfToImageService`:
  - PDF page count & metadata.
  - Page range parser (e.g., `"1-3, 5, 8-10"`).
  - Rasterization to PNG/JPEG/WebP with DPI configuration (96, 150, 300).
  - Export to directory or direct ZIP archive.
  - `IProgress<ConversionProgress>` and `CancellationToken` support.
- [x] Implement `IImageToPdfService` & `ImageToPdfService`:
  - Image sequence compilation into PDF.
  - Sizing presets (Fit to Image, A4, US Letter), aspect ratio scaling, and margins.
  - Background thumbnail generation with frozen bitmaps for thread-safe UI rendering.
  - `IProgress<ConversionProgress>` and `CancellationToken` support.
- [x] Implement `IDialogService` (file open/save and folder picker abstractions).
- [x] Verify Phase 2 build (`0 Errors, 0 Warnings`).

### Phase 3: MVVM ViewModels
- [x] `MainViewModel`: Navigation between PDF-to-Image and Image-to-PDF, theme switching.
- [x] `PdfToImageViewModel`: File selection, range parsing, DPI/format controls, export mode, progress/cancellation.
- [x] `ImageToPdfViewModel`: Observable collection of images, add/remove/clear/move actions, page preset selector, progress/cancellation.
- [x] Verify Phase 3 build (`0 Errors, 0 Warnings`).

### Phase 4: Modern Fluent UI (WPF XAML)
- [x] `MainWindow.xaml`: Windows 11 Fluent window with top navigation bar and theme switcher.
- [x] `PdfToImageView.xaml`: Drag-and-drop zone, file details, settings card (DPI, formats, page range), folder/zip destination, and progress card.
- [x] `ImageToPdfView.xaml`: Image drop zone, thumbnail preview list with order badges, reordering buttons, layout settings, and progress bar.
- [x] `Converters/ValueConverters.cs`: Data-binding converters for visibility, negation, equality, and button appearance.
- [x] Verify Phase 4 build (`0 Errors, 0 Warnings`).

### Phase 5: Build Verification & Testing
- [x] Run `dotnet build` with zero warnings (`0 Errors, 0 Warnings`).
- [x] End-to-end automated verification of PDF rasterization, image compilation, and roundtrip consistency (`VERIFICATION_SUCCESS`).
- [x] UI responsiveness and non-blocking asynchronous operations verified.

### Phase 6: Expanded Document Capabilities (Merge, Split, Reorganize, Compress)
- [x] **Merge PDF**: Multi-document combiner with reordering (move up/down), order badges, aggregate page count, direct streaming, and single destination PDF export.
- [x] **Split PDF**: Document partitioner supporting:
  - *All Pages*: Extract every page into separate individual PDF files.
  - *Custom Range*: Extract specific ranges (e.g. `1-3, 5, 8-10`) into separate parts or combined into one file.
  - *Fixed Interval*: Split into consecutive chunks of N pages.
- [x] **Reorganize PDF**: Interactive visual page workbench with real-time thumbnail streaming, 90° CW and CCW rotation with live `RotateTransform`, page duplication, removal, and reordering.
- [x] **Compress PDF**: File size optimizer supporting:
  - *Balanced*: 150 DPI JPEG raster compression with FlateDecode stream optimization.
  - *Maximum Compression*: 96 DPI JPEG raster compression for email and web sharing.
  - *Lossless Stream Optimization*: Pure FlateDecode content stream recompression preserving full vector and image fidelity.
  - Live comparison metrics showing original size, compressed size, and percentage saved.
- [x] **Universal Minimalist UI**: Clean 7-tab flat underline navigation with centered headers (no icons), airy soft cards, dark modern Convert buttons, and context-aware drag-and-drop file ingestion across all pages.
