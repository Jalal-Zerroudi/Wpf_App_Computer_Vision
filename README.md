# WPF Image Processing Studio

A Windows desktop application for interactive image processing, built with C# and WPF on .NET 8.

## Features

The application displays the original image and the processed result side by side. Its current interface provides:

- image opening for PNG, JPEG, and BMP files;
- export of processed images as PNG, JPEG, or BMP;
- point operations: contrast, additive shift, multiplicative scaling, negative, and thresholding;
- Gaussian and salt-and-pepper noise generation;
- low-pass filters: mean, Gaussian, pyramidal, conical, median, and Nagao;
- edge and high-pass filters: gradient, Sobel, Prewitt, Roberts, Laplacian, Canny, Kirsch, and Marr–Hildreth;
- frequency-domain filters, including Butterworth and homomorphic filtering;
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

## Project structure

- `WpfApp_CV_App.sln` — Visual Studio solution.
- `WpfApp_CV_App/WpfApp_CV_App.csproj` — .NET project and NuGet dependencies.
- `WpfApp_CV_App/MainWindow.xaml` — application interface.
- `WpfApp_CV_App/MainWindow.xaml.cs` — image-processing logic and event handlers.
- `WpfApp_CV_App/App.xaml` — shared application resources and styles.
