# WPF Image Processing Studio

A Windows desktop application for interactive image processing, built with C#, WPF, and .NET 8.

## Branch layout

This repository currently has two divergent lines of history:

- `master` is the default branch and contains commits that are not present on `main`;
- `main` also contains commits that are not present on `master`.

A commit made on one branch is therefore not automatically available on the
other. The behavior, limitations, and commands in this README describe
`main`. Choose the intended branch explicitly before building or contributing.
To inspect the differences locally without changing either branch:

```bash
git fetch origin
git log --left-right --graph --oneline origin/main...origin/master
```

## Features

The application displays the original image and the processed result side by side. Its current interface supports:

- opening PNG, JPEG, and BMP images;
- exporting processed images as PNG, JPEG, or BMP;
- point operations: contrast, additive shift, multiplicative scaling, negative, and thresholding;
- Gaussian and salt-and-pepper noise generation;
- low-pass filters: mean, Gaussian, pyramidal, conical, and median;
- edge and high-pass filters: Sobel, Prewitt, Roberts, Laplacian, Canny, Kirsch, and Marr–Hildreth;
- an ideal low-pass filter in the frequency domain;
- morphology operations: erosion, dilation, opening, closing, internal/external gradients, morphological gradient, and white/black top-hat transforms;
- resetting the processed-image view to start a new operation.

## Current limitations

The following menu entries are present but are not fully implemented yet:

- the generic gradient handler displays a notice and returns an unchanged copy of the source image;
- the Nagao handler currently delegates to the median filter;
- Butterworth low-pass/high-pass, frequency-domain high-pass, band-pass, and homomorphic filtering currently display placeholder notices.

## Technology

- C#
- .NET 8 for Windows
- WPF
- MahApps.Metro
- Material Design in XAML
- Emgu CV

The project declares both **Any CPU** and **x86** build platforms.

## Requirements

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 with the **.NET desktop development** workload, or another environment that supports WPF

WPF targets Windows, so the application is not intended to run natively on Linux or macOS.

## Build and run

The instructions below target `main`. The repository's default branch is currently `master`, so select `main` explicitly when cloning:

```powershell
git clone --branch main https://github.com/Jalal-Zerroudi/Wpf_App_Computer_Vision.git
cd Wpf_App_Computer_Vision
```

Restore, build, and run the application:

```powershell
dotnet restore WpfApp_CV_App.sln
dotnet build WpfApp_CV_App.sln
dotnet run --project WpfApp_CV_App/WpfApp_CV_App.csproj
```

Alternatively, open `WpfApp_CV_App.sln` in Visual Studio, restore the NuGet packages, select a compatible platform, and start the `WpfApp_CV_App` project.

## Usage

1. Start the application.
2. Select **Fichier → Ouvrir**.
3. Choose a PNG, JPEG, or BMP image.
4. Select an operation from the sidebar.
5. Compare the original image with the processed result.
6. Use **Réinitialiser** to discard the current processed result when needed.
7. Select **Fichier → Enregistrer** to export the result.

### Source image, reset, and export

Most processing handlers read `ImageOriginal.Source` rather than the previous processed result, so selecting two filters does not generally apply them as a cumulative pipeline.

- **Réinitialiser** only clears `ImageProcessed.Source`. It does not reload the file or undo changes to the source view.
- **Opening** replaces the source view with the erosion intermediate result; **closing** replaces it with the dilation intermediate result. The white/black top-hat handlers call these operations and inherit that source-view change.
- To compare independent operations against the same original pixels, reopen the input file before each experiment.
- Export requires a processed result. It writes to the selected destination using create/overwrite mode; choose a new filename to preserve the input image.

These observations describe the current `main` implementation, not an undo/history feature.

## Project structure

```text
Wpf_App_Computer_Vision/
├── WpfApp_CV_App.sln
└── WpfApp_CV_App/
    ├── App.xaml
    ├── App.xaml.cs
    ├── MainWindow.xaml
    ├── MainWindow.xaml.cs
    └── WpfApp_CV_App.csproj
```

- `WpfApp_CV_App.sln` — Visual Studio solution.
- `WpfApp_CV_App/WpfApp_CV_App.csproj` — target framework, platforms, and NuGet dependencies.
- `WpfApp_CV_App/MainWindow.xaml` — application layout and controls.
- `WpfApp_CV_App/MainWindow.xaml.cs` — image-processing logic and event handlers.
- `WpfApp_CV_App/App.xaml` — shared application resources and styles.

## Troubleshooting

- If package types cannot be resolved, run `dotnet restore WpfApp_CV_App.sln`.
- If WPF build targets are unavailable, install the Visual Studio **.NET desktop development** workload.
- If a native image-processing dependency fails to load, try the `x86` platform defined in the solution.
