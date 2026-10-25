# WPF Image Processing Studio

A Windows desktop application for interactive image processing, built with C# and WPF on .NET 8.

## Features

The application displays the original image and the processed result side by side. Its current interface provides:

- image opening for PNG, JPEG, and BMP files;
- export of processed images as PNG, JPEG, or BMP;
- point operations: contrast, additive shift, multiplicative scaling, negative, and thresholding;
- Gaussian and salt-and-pepper noise generation;
- spatial low-pass filters: mean, Gaussian, pyramidal, conical, and median, with a Nagao control currently backed by the median implementation;
- edge filters: Sobel, Prewitt, Roberts, Laplacian, Canny, Kirsch, and Marr–Hildreth, plus a placeholder gradient control;
- an ideal frequency-domain low-pass filter, with additional frequency controls present as placeholders;
- morphology operations: erosion, dilation, opening, closing, gradients, and top-hat transforms;
- reset of the processed-image view.

## Technology

- C#
- .NET 8 for Windows
- WPF
- MahApps.Metro
- Material Design in XAML

## Requirements

- Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A development environment that supports WPF, such as Visual Studio 2022

WPF targets Windows, so this project is not intended to run natively on Linux or macOS.

## Build and run

From the repository root:

```powershell
dotnet restore WpfApp_CV_App.sln
dotnet build WpfApp_CV_App.sln
dotnet run --project WpfApp_CV_App/WpfApp_CV_App.csproj
```

Alternatively, open `WpfApp_CV_App.sln` in Visual Studio, restore the NuGet packages, and start the `WpfApp_CV_App` project.

## Usage

1. Start the application.
2. Select **Fichier → Ouvrir** and choose a PNG, JPEG, or BMP image.
3. Choose an operation from the sidebar.
4. Compare the original and processed images.
5. Select **Fichier → Enregistrer** to export the result.

## Processing behavior

- Each operation starts from the currently loaded **original image** and replaces the processed preview. Selecting several filters in sequence does not build a cumulative pipeline.
- Compound morphology operations combine intermediate erosion or dilation results internally while preserving the loaded original image for later independent operations.
- **Reset** clears only the processed preview; the original image remains loaded.
- **Save** exports only the processed preview. If no operation has produced a result, the application displays an error instead of saving the original image.
- Opening another image clears the previous processed result.

## Current implementation limits

- **Nagao** delegates directly to the median-filter handler; it is not a distinct Nagao implementation yet.
- **Gradient** displays an informational message and copies the original pixels unchanged.
- In the frequency section, only **Filtre passe-bas** currently produces an image. The Butterworth low/high-pass, high-pass, homomorphic, and band-pass controls only display “to be implemented” messages.
- The implemented frequency low-pass uses direct two-dimensional DFT and inverse DFT loops rather than an FFT. Its cost grows as `O(W²H²)`, so use small images to avoid long UI freezes.

## Project structure

- `WpfApp_CV_App.sln` — Visual Studio solution.
- `WpfApp_CV_App/WpfApp_CV_App.csproj` — .NET project and NuGet dependencies.
- `WpfApp_CV_App/MainWindow.xaml` — application interface.
- `WpfApp_CV_App/MainWindow.xaml.cs` — image-processing logic and event handlers.
- `WpfApp_CV_App/App.xaml` — shared application resources and styles.
