# Anchor PDF

**Anchor PDF** is a fast, offline desktop utility for converting PDFs to high-resolution images, compiling image collections into structured PDF documents, merging, splitting, and visually reorganizing PDF files with complete local privacy.

Working with PDF documents often requires uploading sensitive personal files, legal contracts, or confidential invoices to online conversion websites with unknown privacy policies and file retention practices. Offline desktop utilities, on the other hand, are frequently bogged down with slow performance, complex multi-step wizards, or intrusive licensing prompts.

Anchor PDF provides a modern, high-speed, 100% offline alternative for 64-bit Windows. All conversions, rasterizations, and document operations happen strictly locally on your computer with zero telemetry and zero server dependencies. Your files remain private, secure, and under your control at all times.

&nbsp;
<p align="center">◈ ◈ ◈</p>
&nbsp;

## Application Walkthrough & Features

### 1. PDF to Images Screen
Convert document pages into crisp, high-resolution raster images with selective page extraction and instant ZIP packaging.

* **Arbitrary High-Resolution Rasterization**: Choose from 96, 150, 300, 600, or 1200 DPI presets for sharp screen previews or fine archival printing.
* **Format Flexibility**: Export pages directly as PNG (`.png`), JPEG (`.jpg`), or WebP (`.webp`).
* **Visual Page Selection**: Browse real-time page thumbnails and selectively check or uncheck individual pages, or use Select All and Deselect All.
* **Direct Image or ZIP Packaging**: Automatically save individual page images or package multiple pages into a consolidated `.zip` archive.

&nbsp;
<p align="center">◈ ◈ ◈</p>
&nbsp;

### 2. Images to PDF Screen
Compile collections of images into clean, structured PDF documents with custom page sizing and orientation controls.

* **Drag-and-Drop Drop Zone**: Drop single or batch image files directly into the window, or browse with standard system dialogs.
* **Standard Image Formats**: Full support for PNG, JPEG, WebP, and BMP images.
* **Page Layout Presets**: Fit to Image, A4 (Portrait, Landscape, Auto), and Letter (Portrait, Landscape, Auto) with configurable margin point controls.
* **Sequence Management**: Reorder images, move items up or down, remove items, or clear all before generating the document.

&nbsp;
<p align="center">◈ ◈ ◈</p>
&nbsp;

### 3. Merge PDFs Screen
Consolidate multiple PDF documents into a single organized file with safe stream composition.

* **Batch Document Combining**: Add multiple PDF documents and merge them into a single file with one click.
* **Document Reordering**: Reorder files up and down to establish the exact sequence of chapters or attachments.
* **Page Count Indicators**: Automatic page detection and total document page count summaries.
* **Safe Direct Stream Saving**: Writes directly to the destination path without temporary file staging or lockups.

&nbsp;
<p align="center">◈ ◈ ◈</p>
&nbsp;

### 4. Split PDF Screen
Extract individual pages or custom ranges with interactive thumbnail previews and dynamic range synchronization.

* **Interactive Visual Pages Gallery**: Real-time thumbnail previews of every page in the source document.
* **Extraction Modes**:
  * *All Pages*: Automatically extracts each page of the document into its own standalone single-page PDF.
  * *Custom Range*: User-defined ranges (e.g. `1-3, 5, 8-10`) with live two-way synchronization between the text expression and page thumbnail checkboxes.
* **Single or Multi-File Output**: Choose whether custom ranges are combined into a single consolidated PDF or saved as separate files.
* **Select All / Deselect All**: Fast toolbar buttons to select or deselect all pages in one click.

&nbsp;
<p align="center">◈ ◈ ◈</p>
&nbsp;

### 5. Reorganize PDF Screen
Rearrange, rotate, duplicate, and remove pages with real-time visual previews.

* **Centered Visual Thumbnail Previews**: High-resolution rendered cards centered in the view for clean inspection.
* **Page Movement**: Move selected pages left or right to customize the document flow.
* **Rotation Controls**: Rotate individual pages 90° clockwise or counter-clockwise with immediate visual preview.
* **Page Duplication & Deletion**: Duplicate recurring forms or remove unwanted pages directly from card quick action buttons.
* **Reset Order**: Restore the original page order at any time with a single click.

&nbsp;
<p align="center">◈ ◈ ◈</p>
&nbsp;

### 6. About Screen
Application details, local privacy architecture, and GitHub repository information.

* **Privacy Architecture**: 100% offline local processing with zero telemetry and zero cloud storage.
* **GitHub Repository**: Direct links to access source code and updates.

&nbsp;
<p align="center">◈ ◈ ◈</p>
&nbsp;

## Supported Feature & Format Matrix

<div align="center">

| Module | Source Formats | Output Formats | Specifications & Options |
| :--- | :--- | :--- | :--- |
| **PDF to Images** | `.pdf` | `.png`, `.jpg`, `.webp` | 96, 150, 300, 600, 1200 DPI; Individual or ZIP packaging |
| **Images to PDF** | `.png`, `.jpg`, `.jpeg`, `.webp`, `.bmp` | `.pdf` | Fit to Image, A4, Letter; Portrait, Landscape, Auto |
| **Merge PDF** | Multiple `.pdf` files | `.pdf` | Multi-file sequence reordering; Total page count indicator |
| **Split PDF** | `.pdf` | Single or multiple `.pdf` | All Pages or Custom Range; Visual thumbnail gallery selection |
| **Reorganize PDF** | `.pdf` | `.pdf` | Move Left/Right, Rotate 90° CW/CCW, Duplicate, Delete, Reset |

</div>

&nbsp;
<p align="center">◈ ◈ ◈</p>
&nbsp;

## Privacy & Non-Destructive File Safety Policy

Anchor PDF is built with strict safety and privacy guarantees:

* **100% Offline Processing**: Zero server dependencies, zero telemetry, and zero network calls. All rendering and compilation operations execute locally on your machine.
* **Strict Read-Only Source Policy**: Original user source files (input PDFs and images) are opened with shared read access and are never modified, overwritten, or deleted.
* **Direct Stream Saving**: Output files stream directly into destination FileStreams rather than staging in `%TEMP%` directories, preventing permission inheritance issues and antivirus file locks.

&nbsp;
<p align="center">◈ ◈ ◈</p>
&nbsp;

## Download & Run Instructions

Anchor PDF is distributed as a **portable single-file executable (`.exe`)**:

1. Navigate to the Releases tab on the right side of this GitHub repository.
2. Download the latest **`Anchor PDF.exe`**.
3. Double-click **`Anchor PDF.exe`** to launch immediately.
4. **No installation required**: Runs standalone on 64-bit Windows 10/11 (x64) without administrative privileges or separate runtime installers.

&nbsp;
<p align="center">◈ ◈ ◈</p>
&nbsp;

## Reporting Bugs & Feedback

* If you encounter issues, formatting anomalies, or have feature suggestions, please report them on the [GitHub Issues](https://github.com/arshdeepsingh404/Anchor-PDF-Project/issues) page.
* Attaching sample non-confidential PDF documents helps diagnose rendering edge cases and expedite fixes.
