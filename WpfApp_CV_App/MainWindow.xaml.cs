using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace WpfApp_CV_App
{
    public partial class MainWindow : MetroWindow
    {
        private Random rand = new Random();

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Méthodes auxiliaires de conversion et de traitement

        // Convertit une image en WriteableBitmap au format Bgra32.
        private WriteableBitmap ConvertToWriteableBitmap(BitmapSource source)
        {
            if (source.Format != PixelFormats.Bgra32)
            {
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            }
            return new WriteableBitmap(source);
        }

        // Applique une opération point par point à tous les pixels.
        private WriteableBitmap ProcessPixelwise(BitmapSource source, Func<Color, Color> operation)
        {
            WriteableBitmap wb = ConvertToWriteableBitmap(source);
            int width = wb.PixelWidth;
            int height = wb.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                Color orig = Color.FromArgb(pixels[i + 3], pixels[i + 2], pixels[i + 1], pixels[i]);
                Color newColor = operation(orig);
                pixels[i] = newColor.B;
                pixels[i + 1] = newColor.G;
                pixels[i + 2] = newColor.R;
                pixels[i + 3] = newColor.A;
            }

            WriteableBitmap result = new WriteableBitmap(width, height, wb.DpiX, wb.DpiY, PixelFormats.Bgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return result;
        }

        // Applique un filtre par convolution avec un noyau donné.
        private WriteableBitmap ApplyConvolutionFilter(BitmapSource source, double[,] kernel)
        {
            WriteableBitmap wb = ConvertToWriteableBitmap(source);
            int width = wb.PixelWidth;
            int height = wb.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels, stride, 0);
            byte[] resultPixels = new byte[pixels.Length];
            int kWidth = kernel.GetLength(0);
            int kHeight = kernel.GetLength(1);
            int offsetX = kWidth / 2;
            int offsetY = kHeight / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double sumR = 0, sumG = 0, sumB = 0;
                    for (int ky = 0; ky < kHeight; ky++)
                    {
                        int py = y + ky - offsetY;
                        py = Math.Max(0, Math.Min(height - 1, py));
                        for (int kx = 0; kx < kWidth; kx++)
                        {
                            int px = x + kx - offsetX;
                            px = Math.Max(0, Math.Min(width - 1, px));
                            int index = (py * stride) + (px * 4);
                            double factor = kernel[kx, ky];
                            sumB += pixels[index] * factor;
                            sumG += pixels[index + 1] * factor;
                            sumR += pixels[index + 2] * factor;
                        }
                    }
                    int idx = (y * stride) + (x * 4);
                    resultPixels[idx] = (byte)Clamp(sumB);
                    resultPixels[idx + 1] = (byte)Clamp(sumG);
                    resultPixels[idx + 2] = (byte)Clamp(sumR);
                    resultPixels[idx + 3] = pixels[idx + 3];
                }
            }
            WriteableBitmap result = new WriteableBitmap(width, height, wb.DpiX, wb.DpiY, PixelFormats.Bgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, stride, 0);
            return result;
        }

        // Permet de “clipper” une valeur entre 0 et 255.
        private int Clamp(double value)
        {
            return (int)Math.Max(0, Math.Min(255, value));
        }

        // Génère une valeur selon une loi normale (pour le bruit gaussien).
        private double NextGaussian(double mean = 0, double stdDev = 1)
        {
            // Transformation Box-Muller
            double u1 = 1.0 - rand.NextDouble();
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        // Crée un WriteableBitmap à partir d’un tableau de pixels.
        private WriteableBitmap CreateBitmapFromPixels(byte[] pixels, int width, int height, double dpiX, double dpiY)
        {
            WriteableBitmap wb = new WriteableBitmap(width, height, dpiX, dpiY, PixelFormats.Bgra32, null);
            int stride = width * 4;
            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return wb;
        }

        #endregion

        #region Boutons et Menu Fichier

        private void ResetImageButton_Click(object sender, RoutedEventArgs e)
        {
            ImageProcessed.Source = null;
        }

        private void MenuItem_Ouvrir_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" };
            if (openFileDialog.ShowDialog() == true)
            {
                ImageOriginal.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                ImageProcessed.Source = null;
            }
        }

        private void MenuItem_Enregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (ImageProcessed.Source == null)
            {
                MessageBox.Show("Aucune image à enregistrer", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            BitmapSource bitmapSource = ImageProcessed.Source as BitmapSource;
            if (bitmapSource == null)
            {
                MessageBox.Show("Impossible de récupérer l'image", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp",
                Title = "Enregistrer l'image",
                FileName = "ImageTraitée",
                DefaultExt = ".png"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (FileStream stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        BitmapEncoder encoder;
                        string extension = Path.GetExtension(saveFileDialog.FileName).ToLower();
                        switch (extension)
                        {
                            case ".jpg":
                                encoder = new JpegBitmapEncoder();
                                break;
                            case ".bmp":
                                encoder = new BmpBitmapEncoder();
                                break;
                            default:
                                encoder = new PngBitmapEncoder();
                                break;
                        }
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(stream);
                    }
                    MessageBox.Show("Image enregistrée avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'enregistrement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Opérations ponctuelles

        private void MenuItem_Contraste_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Exemple : augmentation du contraste de 50 (valeur fixe)
            double contrast = 50;
            double factor = (259 * (contrast + 255)) / (255 * (259 - contrast));
            var result = ProcessPixelwise((BitmapSource)ImageOriginal.Source, c =>
            {
                byte r = (byte)Clamp(factor * (c.R - 128) + 128);
                byte g = (byte)Clamp(factor * (c.G - 128) + 128);
                byte b = (byte)Clamp(factor * (c.B - 128) + 128);
                return Color.FromArgb(c.A, r, g, b);
            });
            ImageProcessed.Source = result;
        }

        private void MenuItem_Histogramme_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Calcul de l'histogramme en niveaux de gris
            WriteableBitmap wb = ConvertToWriteableBitmap((BitmapSource)ImageOriginal.Source);
            int width = wb.PixelWidth, height = wb.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels, stride, 0);
            int[] histogram = new int[256];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                int gray = (pixels[i + 2] + pixels[i + 1] + pixels[i]) / 3;
                histogram[gray]++;
            }
            // Affichage de quelques valeurs (les 10 premières)
            string histoText = string.Join(Environment.NewLine, histogram.Select((val, idx) => $"{idx}: {val}").Take(10));
            MessageBox.Show("Histogram (10 premières valeurs) :\n" + histoText);
        }

        private void MenuItem_DecalageAdditif_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Décalage additif de +30 sur chaque canal
            var result = ProcessPixelwise((BitmapSource)ImageOriginal.Source, c =>
            {
                byte r = (byte)Clamp(c.R + 30);
                byte g = (byte)Clamp(c.G + 30);
                byte b = (byte)Clamp(c.B + 30);
                return Color.FromArgb(c.A, r, g, b);
            });
            ImageProcessed.Source = result;
        }

        private void MenuItem_MiseAEchelleMultiplicative_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Mise à l'échelle multiplicative (facteur 1.2)
            var result = ProcessPixelwise((BitmapSource)ImageOriginal.Source, c =>
            {
                byte r = (byte)Clamp(c.R * 1.2);
                byte g = (byte)Clamp(c.G * 1.2);
                byte b = (byte)Clamp(c.B * 1.2);
                return Color.FromArgb(c.A, r, g, b);
            });
            ImageProcessed.Source = result;
        }

        private void MenuItem_Inversion_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Inversion de chaque canal (négatif)
            var result = ProcessPixelwise((BitmapSource)ImageOriginal.Source, c =>
                Color.FromArgb(c.A, (byte)(255 - c.R), (byte)(255 - c.G), (byte)(255 - c.B)));
            ImageProcessed.Source = result;
        }

        private void MenuItem_Seuillage_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Seuillage à 128 : si la valeur moyenne est >= 128, on met à 255 sia à 0.
            var result = ProcessPixelwise((BitmapSource)ImageOriginal.Source, c =>
            {
                int gray = (c.R + c.G + c.B) / 3;
                byte value = (byte)(gray >= 128 ? 255 : 0);
                return Color.FromArgb(c.A, value, value, value);
            });
            ImageProcessed.Source = result;
        }

        #endregion

        #region Bruit

        private void MenuItem_GaussienBruit_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Bruit gaussien (écart-type = 20)
            var result = ProcessPixelwise((BitmapSource)ImageOriginal.Source, c =>
            {
                int r = Clamp(c.R + NextGaussian(0, 20));
                int g = Clamp(c.G + NextGaussian(0, 20));
                int b = Clamp(c.B + NextGaussian(0, 20));
                return Color.FromArgb(c.A, (byte)r, (byte)g, (byte)b);
            });
            ImageProcessed.Source = result;
        }

        private void MenuItem_PoivreEtSel_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Bruit poivre et sel (probabilité 5%)
            double prob = 0.05;
            var result = ProcessPixelwise((BitmapSource)ImageOriginal.Source, c =>
            {
                double p = rand.NextDouble();
                if (p < prob)
                    return Color.FromArgb(c.A, 0, 0, 0);
                else if (p > 1 - prob)
                    return Color.FromArgb(c.A, 255, 255, 255);
                else
                    return c;
            });
            ImageProcessed.Source = result;
        }

        #endregion

        #region Filtres passe-bas (Filtres linéaires)

        private void MenuItem_Moyenneur33_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            double[,] kernel = {
                { 1.0/9, 1.0/9, 1.0/9 },
                { 1.0/9, 1.0/9, 1.0/9 },
                { 1.0/9, 1.0/9, 1.0/9 }
            };
            var result = ApplyConvolutionFilter((BitmapSource)ImageOriginal.Source, kernel);
            ImageProcessed.Source = result;
        }

        private void MenuItem_Moyenneur55_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            double factor = 1.0 / 25;
            double[,] kernel = new double[5, 5];
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    kernel[i, j] = factor;
            var result = ApplyConvolutionFilter((BitmapSource)ImageOriginal.Source, kernel);
            ImageProcessed.Source = result;
        }

        private void MenuItem_Gaussien33_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            double[,] kernel = {
                { 1.0/16, 2.0/16, 1.0/16 },
                { 2.0/16, 4.0/16, 2.0/16 },
                { 1.0/16, 2.0/16, 1.0/16 }
            };
            var result = ApplyConvolutionFilter((BitmapSource)ImageOriginal.Source, kernel);
            ImageProcessed.Source = result;
        }

        private void MenuItem_Gaussien55_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Exemple d'un noyau gaussien 5x5
            double[,] kernel = {
                { 1,  4,  6,  4, 1 },
                { 4, 16, 24, 16, 4 },
                { 6, 24, 36, 24, 6 },
                { 4, 16, 24, 16, 4 },
                { 1,  4,  6,  4, 1 }
            };
            double sum = kernel.Cast<double>().Sum();
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    kernel[i, j] /= sum;
            var result = ApplyConvolutionFilter((BitmapSource)ImageOriginal.Source, kernel);
            ImageProcessed.Source = result;
        }

        private void MenuItem_Pyramidal_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Filtre pyramidal : réduction de l'image de moitié
            BitmapSource? source = ImageOriginal.Source as BitmapSource;
            TransformedBitmap tb = new TransformedBitmap(source, new ScaleTransform(0.5, 0.5));
            ImageProcessed.Source = tb;
        }

        private void MenuItem_Conique_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Filtre conique : moyenne pondérée (plus de poids au centre)
            double[,] kernel = {
                { 1, 2, 1 },
                { 2, 4, 2 },
                { 1, 2, 1 }
            };
            double sum = kernel.Cast<double>().Sum();
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    kernel[i, j] /= sum;
            var result = ApplyConvolutionFilter((BitmapSource)ImageOriginal.Source, kernel);
            ImageProcessed.Source = result;
        }

        #endregion

        #region Filtres passe-bas (Filtres a linéaires)

        private void MenuItem_Median_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            WriteableBitmap wb = ConvertToWriteableBitmap((BitmapSource)ImageOriginal.Source);
            int width = wb.PixelWidth;
            int height = wb.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels, stride, 0);
            byte[] resultPixels = new byte[pixels.Length];

            int windowSize = 3;
            int offset = windowSize / 2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte[] windowR = new byte[windowSize * windowSize];
                    byte[] windowG = new byte[windowSize * windowSize];
                    byte[] windowB = new byte[windowSize * windowSize];
                    int count = 0;
                    for (int wy = -offset; wy <= offset; wy++)
                    {
                        int py = Math.Max(0, Math.Min(height - 1, y + wy));
                        for (int wx = -offset; wx <= offset; wx++)
                        {
                            int px = Math.Max(0, Math.Min(width - 1, x + wx));
                            int index = (py * stride) + (px * 4);
                            windowB[count] = pixels[index];
                            windowG[count] = pixels[index + 1];
                            windowR[count] = pixels[index + 2];
                            count++;
                        }
                    }
                    Array.Sort(windowR);
                    Array.Sort(windowG);
                    Array.Sort(windowB);
                    int mid = count / 2;
                    int idx = (y * stride) + (x * 4);
                    resultPixels[idx] = windowB[mid];
                    resultPixels[idx + 1] = windowG[mid];
                    resultPixels[idx + 2] = windowR[mid];
                    resultPixels[idx + 3] = pixels[idx + 3];
                }
            }
            ImageProcessed.Source = CreateBitmapFromPixels(resultPixels, width, height, wb.DpiX, wb.DpiY);
        }

        private void MenuItem_Nagao_Click(object sender, RoutedEventArgs e)
        {
            // Pour simplifier, nous utilisons ici le filtre médian comme substitut
            MenuItem_Median_Click(sender, e);
        }

        #endregion

        #region Filtres passe-haut

        private void MenuItem_Gradient_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Pour le gradient, un vrai calcul nécessiterait une convolution.
            MessageBox.Show("Filtre gradient a implémenté complètement.");
            ImageProcessed.Source = ProcessPixelwise((BitmapSource)ImageOriginal.Source, c => c);
        }

        private void MenuItem_Sobel_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Opérateur de Sobel
            double[,] gx = {
                { -1, 0, 1 },
                { -2, 0, 2 },
                { -1, 0, 1 }
            };
            double[,] gy = {
                { -1, -2, -1 },
                {  0,  0,  0 },
                {  1,  2,  1 }
            };

            WriteableBitmap wb = ConvertToWriteableBitmap((BitmapSource)ImageOriginal.Source);
            int width = wb.PixelWidth, height = wb.PixelHeight, stride = width * 4;
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels, stride, 0);
            byte[] resultPixels = new byte[pixels.Length];
            int offset = 1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double sumX = 0, sumY = 0;
                    for (int ky = -offset; ky <= offset; ky++)
                    {
                        int py = Math.Max(0, Math.Min(height - 1, y + ky));
                        for (int kx = -offset; kx <= offset; kx++)
                        {
                            int px = Math.Max(0, Math.Min(width - 1, x + kx));
                            int index = (py * stride) + (px * 4);
                            double intensity = (pixels[index + 2] + pixels[index + 1] + pixels[index]) / 3.0;
                            sumX += intensity * gx[kx + offset, ky + offset];
                            sumY += intensity * gy[kx + offset, ky + offset];
                        }
                    }
                    int gradient = Clamp(Math.Sqrt(sumX * sumX + sumY * sumY));
                    int idx = (y * stride) + (x * 4);
                    resultPixels[idx] = (byte)gradient;
                    resultPixels[idx + 1] = (byte)gradient;
                    resultPixels[idx + 2] = (byte)gradient;
                    resultPixels[idx + 3] = pixels[idx + 3];
                }
            }
            ImageProcessed.Source = CreateBitmapFromPixels(resultPixels, width, height, wb.DpiX, wb.DpiY);
        }

        private void MenuItem_Prewitt_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Opérateur de Prewitt
            double[,] gx = {
                { -1, 0, 1 },
                { -1, 0, 1 },
                { -1, 0, 1 }
            };
            double[,] gy = {
                { -1, -1, -1 },
                {  0,  0,  0 },
                {  1,  1,  1 }
            };

            WriteableBitmap wb = ConvertToWriteableBitmap((BitmapSource)ImageOriginal.Source);
            int width = wb.PixelWidth, height = wb.PixelHeight, stride = width * 4;
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels, stride, 0);
            byte[] resultPixels = new byte[pixels.Length];
            int offset = 1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double sumX = 0, sumY = 0;
                    for (int ky = -offset; ky <= offset; ky++)
                    {
                        int py = Math.Max(0, Math.Min(height - 1, y + ky));
                        for (int kx = -offset; kx <= offset; kx++)
                        {
                            int px = Math.Max(0, Math.Min(width - 1, x + kx));
                            int index = (py * stride) + (px * 4);
                            double intensity = (pixels[index + 2] + pixels[index + 1] + pixels[index]) / 3.0;
                            sumX += intensity * gx[kx + offset, ky + offset];
                            sumY += intensity * gy[kx + offset, ky + offset];
                        }
                    }
                    int gradient = Clamp(Math.Sqrt(sumX * sumX + sumY * sumY));
                    int idx = (y * stride) + (x * 4);
                    resultPixels[idx] = (byte)gradient;
                    resultPixels[idx + 1] = (byte)gradient;
                    resultPixels[idx + 2] = (byte)gradient;
                    resultPixels[idx + 3] = pixels[idx + 3];
                }
            }
            ImageProcessed.Source = CreateBitmapFromPixels(resultPixels, width, height, wb.DpiX, wb.DpiY);
        }

        private void MenuItem_Roberts_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Opérateur de Roberts
            WriteableBitmap wb = ConvertToWriteableBitmap((BitmapSource)ImageOriginal.Source);
            int width = wb.PixelWidth, height = wb.PixelHeight, stride = width * 4;
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels, stride, 0);
            byte[] resultPixels = new byte[pixels.Length];

            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int idx = (y * stride) + (x * 4);
                    int idxDiag = ((y + 1) * stride) + ((x + 1) * 4);
                    double intensity = (pixels[idx + 2] + pixels[idx + 1] + pixels[idx]) / 3.0;
                    double intensityDiag = (pixels[idxDiag + 2] + pixels[idxDiag + 1] + pixels[idxDiag]) / 3.0;
                    double gradient = Math.Abs(intensity - intensityDiag);
                    byte val = (byte)Clamp(gradient);
                    resultPixels[idx] = val;
                    resultPixels[idx + 1] = val;
                    resultPixels[idx + 2] = val;
                    resultPixels[idx + 3] = pixels[idx + 3];
                }
            }
            // Recopie de la dernière ligne/colonne
            Array.Copy(pixels, resultPixels, pixels.Length);
            ImageProcessed.Source = CreateBitmapFromPixels(resultPixels, width, height, wb.DpiX, wb.DpiY);
        }

        private void MenuItem_Laplacien_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            double[,] kernel = {
                { 0, -1, 0 },
                { -1, 4, -1 },
                { 0, -1, 0 }
            };
            var result = ApplyConvolutionFilter((BitmapSource)ImageOriginal.Source, kernel);
            ImageProcessed.Source = result;
        }

        private void MenuItem_Canny_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;

            // 1. Conversion en niveaux de gris
            WriteableBitmap grayImage = ConvertToGrayScale((BitmapSource)ImageOriginal.Source);

            // 2. Flou gaussien pour réduire le bruit
            double[,] gaussianKernel = {
                { 2,  4,  5,  4, 2 },
                { 4,  9, 12,  9, 4 },
                { 5, 12, 15, 12, 5 },
                { 4,  9, 12,  9, 4 },
                { 2,  4,  5,  4, 2 }
            };
            double kernelSum = gaussianKernel.Cast<double>().Sum();
            for (int i = 0; i < gaussianKernel.GetLength(0); i++)
                for (int j = 0; j < gaussianKernel.GetLength(1); j++)
                    gaussianKernel[i, j] /= kernelSum;
            WriteableBitmap blurredImage = ApplyConvolutionFilter(grayImage, gaussianKernel);

            // 3. Calcul des gradients avec Sobel
            (WriteableBitmap gradientImage, double[,] gradientDirections) = ComputeGradients(blurredImage);

            // 4. Suppression a maximale
            WriteableBitmap aMaxImage = aMaximumSuppression(gradientImage, gradientDirections);

            // 5. Double seuillage
            WriteableBitmap thresholdedImage = DoubleThreshold(aMaxImage, 30, 90);

            // 6. Suivi de contours par hystérésis
            WriteableBitmap finalImage = EdgeTrackingByHysteresis(thresholdedImage);

            ImageProcessed.Source = finalImage;
        }

        // Méthode pour convertir en niveaux de gris
        private WriteableBitmap ConvertToGrayScale(BitmapSource source)
        {
            WriteableBitmap wb = ConvertToWriteableBitmap(source);
            int width = wb.PixelWidth;
            int height = wb.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte gray = (byte)((pixels[i + 2] * 0.3) + (pixels[i + 1] * 0.59) + (pixels[i] * 0.11));
                pixels[i] = gray;
                pixels[i + 1] = gray;
                pixels[i + 2] = gray;
            }

            WriteableBitmap result = new WriteableBitmap(width, height, wb.DpiX, wb.DpiY, PixelFormats.Bgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return result;
        }

        // Méthode pour calculer les gradients
        private (WriteableBitmap, double[,]) ComputeGradients(WriteableBitmap source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);
            byte[] resultPixels = new byte[pixels.Length];
            double[,] gradientDirections = new double[width, height];

            double[,] gx = {
        { -1, 0, 1 },
        { -2, 0, 2 },
        { -1, 0, 1 }
    };
            double[,] gy = {
        { -1, -2, -1 },
        {  0,  0,  0 },
        {  1,  2,  1 }
    };

            int offset = 1;

            for (int y = offset; y < height - offset; y++)
            {
                for (int x = offset; x < width - offset; x++)
                {
                    double sumX = 0, sumY = 0;
                    for (int ky = -offset; ky <= offset; ky++)
                    {
                        for (int kx = -offset; kx <= offset; kx++)
                        {
                            int px = x + kx;
                            int py = y + ky;
                            int index = (py * stride) + (px * 4);
                            double intensity = pixels[index]; // Image en niveaux de gris
                            sumX += intensity * gx[ky + offset, kx + offset];
                            sumY += intensity * gy[ky + offset, kx + offset];
                        }
                    }
                    double magnitude = Math.Sqrt(sumX * sumX + sumY * sumY);
                    int idx = (y * stride) + (x * 4);
                    byte value = (byte)Clamp(magnitude);
                    resultPixels[idx] = value;
                    resultPixels[idx + 1] = value;
                    resultPixels[idx + 2] = value;
                    gradientDirections[x, y] = Math.Atan2(sumY, sumX) * (180 / Math.PI);
                }
            }

            WriteableBitmap gradientImage = CreateBitmapFromPixels(resultPixels, width, height, source.DpiX, source.DpiY);
            return (gradientImage, gradientDirections);
        }

        // Méthode pour la suppression a maximale
        private WriteableBitmap aMaximumSuppression(WriteableBitmap gradientImage, double[,] angles)
        {
            int width = gradientImage.PixelWidth;
            int height = gradientImage.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            gradientImage.CopyPixels(pixels, stride, 0);
            byte[] resultPixels = new byte[pixels.Length];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int idx = (y * stride) + (x * 4);
                    double angle = angles[x, y];
                    angle = angle < 0 ? angle + 180 : angle;

                    byte currentPixel = pixels[idx];
                    byte neighbor1 = 0;
                    byte neighbor2 = 0;

                    if ((angle >= 0 && angle < 22.5) || (angle >= 157.5 && angle <= 180))
                    {
                        neighbor1 = pixels[idx - 4];
                        neighbor2 = pixels[idx + 4];
                    }
                    else if (angle >= 22.5 && angle < 67.5)
                    {
                        neighbor1 = pixels[idx - stride - 4];
                        neighbor2 = pixels[idx + stride + 4];
                    }
                    else if (angle >= 67.5 && angle < 112.5)
                    {
                        neighbor1 = pixels[idx - stride];
                        neighbor2 = pixels[idx + stride];
                    }
                    else if (angle >= 112.5 && angle < 157.5)
                    {
                        neighbor1 = pixels[idx - stride + 4];
                        neighbor2 = pixels[idx + stride - 4];
                    }

                    if (currentPixel >= neighbor1 && currentPixel >= neighbor2)
                    {
                        resultPixels[idx] = currentPixel;
                        resultPixels[idx + 1] = currentPixel;
                        resultPixels[idx + 2] = currentPixel;
                        resultPixels[idx + 3] = 255;
                    }
                    else
                    {
                        resultPixels[idx] = 0;
                        resultPixels[idx + 1] = 0;
                        resultPixels[idx + 2] = 0;
                        resultPixels[idx + 3] = 255;
                    }
                }
            }

            WriteableBitmap resultImage = CreateBitmapFromPixels(resultPixels, width, height, gradientImage.DpiX, gradientImage.DpiY);
            return resultImage;
        }

        // Méthode pour le double seuillage
        private WriteableBitmap DoubleThreshold(WriteableBitmap aMaxImage, byte lowThreshold, byte highThreshold)
        {
            int width = aMaxImage.PixelWidth;
            int height = aMaxImage.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            aMaxImage.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte value = pixels[i];
                if (value >= highThreshold)
                {
                    pixels[i] = 255;
                    pixels[i + 1] = 255;
                    pixels[i + 2] = 255;
                }
                else if (value >= lowThreshold)
                {
                    pixels[i] = 128;
                    pixels[i + 1] = 128;
                    pixels[i + 2] = 128;
                }
                else
                {
                    pixels[i] = 0;
                    pixels[i + 1] = 0;
                    pixels[i + 2] = 0;
                }
            }

            WriteableBitmap resultImage = CreateBitmapFromPixels(pixels, width, height, aMaxImage.DpiX, aMaxImage.DpiY);
            return resultImage;
        }

        // Méthode pour le suivi de contours par hystérésis
        private WriteableBitmap EdgeTrackingByHysteresis(WriteableBitmap thresholdedImage)
        {
            int width = thresholdedImage.PixelWidth;
            int height = thresholdedImage.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            thresholdedImage.CopyPixels(pixels, stride, 0);

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int idx = (y * stride) + (x * 4);
                    if (pixels[idx] == 128)
                    {
                        bool connectedToStrongEdge = false;
                        for (int dy = -1; dy <= 1 && !connectedToStrongEdge; dy++)
                        {
                            for (int dx = -1; dx <= 1 && !connectedToStrongEdge; dx++)
                            {
                                if (dy == 0 && dx == 0) continue;
                                int neighborIdx = ((y + dy) * stride) + ((x + dx) * 4);
                                if (pixels[neighborIdx] == 255)
                                {
                                    connectedToStrongEdge = true;
                                }
                            }
                        }
                        if (connectedToStrongEdge)
                        {
                            pixels[idx] = 255;
                            pixels[idx + 1] = 255;
                            pixels[idx + 2] = 255;
                        }
                        else
                        {
                            pixels[idx] = 0;
                            pixels[idx + 1] = 0;
                            pixels[idx + 2] = 0;
                        }
                    }
                }
            }

            WriteableBitmap resultImage = CreateBitmapFromPixels(pixels, width, height, thresholdedImage.DpiX, thresholdedImage.DpiY);
            return resultImage;
        }


        private void MenuItem_Kirsch_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;

            double[,] kirschX = {
                { 5,  5,  5 },
                { -3, 0, -3 },
                { -3, -3, -3 }
            };

            double[,] kirschY = {
                { 5, -3, -3 },
                { 5,  0, -3 },
                { 5, -3, -3 }
            };

            WriteableBitmap wb = ConvertToWriteableBitmap((BitmapSource)ImageOriginal.Source);
            int width = wb.PixelWidth, height = wb.PixelHeight, stride = width * 4;
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels, stride, 0);
            byte[] resultPixels = new byte[pixels.Length];
            int offset = 1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double sumX = 0, sumY = 0;
                    for (int ky = -offset; ky <= offset; ky++)
                    {
                        int py = Math.Max(0, Math.Min(height - 1, y + ky));
                        for (int kx = -offset; kx <= offset; kx++)
                        {
                            int px = Math.Max(0, Math.Min(width - 1, x + kx));
                            int index = (py * stride) + (px * 4);
                            double intensity = (pixels[index + 2] + pixels[index + 1] + pixels[index]) / 3.0;
                            sumX += intensity * kirschX[ky + offset, kx + offset];
                            sumY += intensity * kirschY[ky + offset, kx + offset];
                        }
                    }
                    int gradient = Clamp(Math.Sqrt(sumX * sumX + sumY * sumY));
                    int idx = (y * stride) + (x * 4);
                    resultPixels[idx] = (byte)gradient;
                    resultPixels[idx + 1] = (byte)gradient;
                    resultPixels[idx + 2] = (byte)gradient;
                    resultPixels[idx + 3] = pixels[idx + 3];
                }
            }
            ImageProcessed.Source = CreateBitmapFromPixels(resultPixels, width, height, wb.DpiX, wb.DpiY);
        }

        private void MenuItem_MarrHildreth_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;

            // 1. Conversion en niveaux de gris
            WriteableBitmap grayImage = ConvertToGrayScale((BitmapSource)ImageOriginal.Source);

            // 2. Flou gaussien pour réduire le bruit
            double[,] gaussianKernel = {
                { 2,  4,  5,  4, 2 },
                { 4,  9, 12,  9, 4 },
                { 5, 12, 15, 12, 5 },
                { 4,  9, 12,  9, 4 },
                { 2,  4,  5,  4, 2 }
            };
            double kernelSum = gaussianKernel.Cast<double>().Sum();
            for (int i = 0; i < gaussianKernel.GetLength(0); i++)
                for (int j = 0; j < gaussianKernel.GetLength(1); j++)
                    gaussianKernel[i, j] /= kernelSum;
            WriteableBitmap blurredImage = ApplyConvolutionFilter(grayImage, gaussianKernel);

            // 3. Application du filtre Laplacien
            double[,] laplacianKernel = {
                { 0,  1, 0 },
                { 1, -4, 1 },
                { 0,  1, 0 }
            };
            WriteableBitmap laplacianImage = ApplyConvolutionFilter(blurredImage, laplacianKernel);

            // 4. Détection des zéros-crossings
            WriteableBitmap zeroCrossingImage = DetectZeroCrossings(laplacianImage);

            ImageProcessed.Source = zeroCrossingImage;
        }

        private WriteableBitmap DetectZeroCrossings(WriteableBitmap source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);
            byte[] resultPixels = new byte[pixels.Length];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int idx = (y * stride) + (x * 4);
                    double currentPixel = pixels[idx];

                    bool zeroCrossing = false;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dy == 0 && dx == 0) continue;
                            int neighborIdx = ((y + dy) * stride) + ((x + dx) * 4);
                            double neighborPixel = pixels[neighborIdx];
                            if ((currentPixel > 0 && neighborPixel < 0) || (currentPixel < 0 && neighborPixel > 0))
                            {
                                zeroCrossing = true;
                                break;
                            }
                        }
                        if (zeroCrossing) break;
                    }

                    byte value = zeroCrossing ? (byte)255 : (byte)0;
                    resultPixels[idx] = value;
                    resultPixels[idx + 1] = value;
                    resultPixels[idx + 2] = value;
                    resultPixels[idx + 3] = 255;
                }
            }

            WriteableBitmap resultImage = CreateBitmapFromPixels(resultPixels, width, height, source.DpiX, source.DpiY);
            return resultImage;
        }


        #endregion

        #region Filtrage fréquentiel

        private void MenuItem_FiltrePasseBas_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;

            // Convertir l'image en niveaux de gris
            WriteableBitmap grayImage = ConvertToGrayScale((BitmapSource)ImageOriginal.Source);

            // Appliquer la transformation de Fourier
            Complex[,] fourierTransform = FourierTransform(grayImage);

            // Appliquer le filtre passe-bas
            int width = grayImage.PixelWidth;
            int height = grayImage.PixelHeight;
            int radius = Math.Min(width, height) / 4; // Rayon du filtre passe-bas
            for (int u = 0; u < width; u++)
            {
                for (int v = 0; v < height; v++)
                {
                    double distance = Math.Sqrt(Math.Pow(u - width / 2, 2) + Math.Pow(v - height / 2, 2));
                    if (distance > radius)
                    {
                        fourierTransform[u, v] = Complex.Zero;
                    }
                }
            }

            // Appliquer la transformation inverse de Fourier
            WriteableBitmap filteredImage = InverseFourierTransform(fourierTransform, width, height);

            ImageProcessed.Source = filteredImage;
        }

        // Méthode pour appliquer la transformation de Fourier
        private Complex[,] FourierTransform(WriteableBitmap source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);
            Complex[,] transform = new Complex[width, height];

            for (int u = 0; u < width; u++)
            {
                for (int v = 0; v < height; v++)
                {
                    Complex sum = Complex.Zero;
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            double angle = -2 * Math.PI * ((u * x / (double)width) + (v * y / (double)height));
                            sum += new Complex(pixels[(y * stride) + (x * 4)], 0) * Complex.Exp(new Complex(0, angle));
                        }
                    }
                    transform[u, v] = sum;
                }
            }
            return transform;
        }

        // Méthode pour appliquer la transformation inverse de Fourier
        private WriteableBitmap InverseFourierTransform(Complex[,] transform, int width, int height)
        {
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Complex sum = Complex.Zero;
                    for (int u = 0; u < width; u++)
                    {
                        for (int v = 0; v < height; v++)
                        {
                            double angle = 2 * Math.PI * ((u * x / (double)width) + (v * y / (double)height));
                            sum += transform[u, v] * Complex.Exp(new Complex(0, angle));
                        }
                    }
                    byte value = (byte)Clamp(sum.Real / (width * height));
                    int idx = (y * stride) + (x * 4);
                    pixels[idx] = value;
                    pixels[idx + 1] = value;
                    pixels[idx + 2] = value;
                    pixels[idx + 3] = 255;
                }
            }

            WriteableBitmap result = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            result.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return result;
        }

        private void MenuItem_ButterworthBas_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Filtre passe-bas de Butterworth a implémenté.");
        }

        private void MenuItem_FiltrePasseHaut_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Filtre passe-haut en domaine fréquentiel a implémenté.");
        }

        private void MenuItem_ButterworthHaut_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Filtre passe-haut de Butterworth a implémenté.");
        }

        private void MenuItem_Homomorphique_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Filtrage homomorphique a implémenté.");
        }

        private void MenuItem_FiltrePasseBande_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Filtre passe-bande a implémenté.");
        }

        #endregion

        #region Morphologie

        private void MenuItem_Erosion_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            WriteableBitmap wb = ConvertToWriteableBitmap((BitmapSource)ImageOriginal.Source);
            int width = wb.PixelWidth, height = wb.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels, stride, 0);
            byte[] resultPixels = new byte[pixels.Length];

            int offset = 1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte minR = 255, minG = 255, minB = 255;
                    for (int dy = -offset; dy <= offset; dy++)
                    {
                        int ny = Math.Max(0, Math.Min(height - 1, y + dy));
                        for (int dx = -offset; dx <= offset; dx++)
                        {
                            int nx = Math.Max(0, Math.Min(width - 1, x + dx));
                            int index = (ny * stride) + (nx * 4);
                            minB = Math.Min(minB, pixels[index]);
                            minG = Math.Min(minG, pixels[index + 1]);
                            minR = Math.Min(minR, pixels[index + 2]);
                        }
                    }
                    int idx = (y * stride) + (x * 4);
                    resultPixels[idx] = minB;
                    resultPixels[idx + 1] = minG;
                    resultPixels[idx + 2] = minR;
                    resultPixels[idx + 3] = pixels[idx + 3];
                }
            }
            ImageProcessed.Source = CreateBitmapFromPixels(resultPixels, width, height, wb.DpiX, wb.DpiY);
        }

        private void MenuItem_Dilatation_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            WriteableBitmap wb = ConvertToWriteableBitmap((BitmapSource)ImageOriginal.Source);
            int width = wb.PixelWidth, height = wb.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            wb.CopyPixels(pixels, stride, 0);
            byte[] resultPixels = new byte[pixels.Length];

            int offset = 1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte maxR = 0, maxG = 0, maxB = 0;
                    for (int dy = -offset; dy <= offset; dy++)
                    {
                        int ny = Math.Max(0, Math.Min(height - 1, y + dy));
                        for (int dx = -offset; dx <= offset; dx++)
                        {
                            int nx = Math.Max(0, Math.Min(width - 1, x + dx));
                            int index = (ny * stride) + (nx * 4);
                            maxB = Math.Max(maxB, pixels[index]);
                            maxG = Math.Max(maxG, pixels[index + 1]);
                            maxR = Math.Max(maxR, pixels[index + 2]);
                        }
                    }
                    int idx = (y * stride) + (x * 4);
                    resultPixels[idx] = maxB;
                    resultPixels[idx + 1] = maxG;
                    resultPixels[idx + 2] = maxR;
                    resultPixels[idx + 3] = pixels[idx + 3];
                }
            }
            ImageProcessed.Source = CreateBitmapFromPixels(resultPixels, width, height, wb.DpiX, wb.DpiY);
        }

        private void MenuItem_Ouverture_Click(object sender, RoutedEventArgs e)
        {
            // Ouverture = érosion puis dilatation
            MenuItem_Erosion_Click(sender, e);
            BitmapSource eroded = ImageProcessed.Source as BitmapSource;
            if (eroded != null)
            {
                ImageOriginal.Source = eroded;
                MenuItem_Dilatation_Click(sender, e);
            }
        }

        private void MenuItem_Fermeture_Click(object sender, RoutedEventArgs e)
        {
            // Fermeture = dilatation puis érosion
            MenuItem_Dilatation_Click(sender, e);
            BitmapSource? dilated = ImageProcessed.Source as BitmapSource;
            if (dilated != null)
            {
                ImageOriginal.Source = dilated;
                MenuItem_Erosion_Click(sender, e);
            }
        }

        private void MenuItem_GradientInterne_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Gradient interne = Original - Érosion
            WriteableBitmap orig = ConvertToWriteableBitmap((BitmapSource)ImageOriginal.Source);
            MenuItem_Erosion_Click(sender, e);
            WriteableBitmap eroded = ConvertToWriteableBitmap((BitmapSource)ImageProcessed.Source);
            int width = orig.PixelWidth, height = orig.PixelHeight;
            int stride = width * 4;
            byte[] origPixels = new byte[height * stride];
            orig.CopyPixels(origPixels, stride, 0);
            byte[] erodedPixels = new byte[height * stride];
            eroded.CopyPixels(erodedPixels, stride, 0);
            byte[] resultPixels = new byte[origPixels.Length];
            for (int i = 0; i < origPixels.Length; i += 4)
            {
                resultPixels[i] = (byte)Clamp(origPixels[i] - erodedPixels[i]);
                resultPixels[i + 1] = (byte)Clamp(origPixels[i + 1] - erodedPixels[i + 1]);
                resultPixels[i + 2] = (byte)Clamp(origPixels[i + 2] - erodedPixels[i + 2]);
                resultPixels[i + 3] = origPixels[i + 3];
            }
            ImageProcessed.Source = CreateBitmapFromPixels(resultPixels, width, height, orig.DpiX, orig.DpiY);
        }

        private void MenuItem_GradientExterne_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Gradient externe = Dilatation - Original
            WriteableBitmap orig = ConvertToWriteableBitmap((BitmapSource)ImageOriginal.Source);
            MenuItem_Dilatation_Click(sender, e);
            WriteableBitmap dilated = ConvertToWriteableBitmap((BitmapSource)ImageProcessed.Source);
            int width = orig.PixelWidth, height = orig.PixelHeight;
            int stride = width * 4;
            byte[] origPixels = new byte[height * stride];
            orig.CopyPixels(origPixels, stride, 0);
            byte[] dilatedPixels = new byte[height * stride];
            dilated.CopyPixels(dilatedPixels, stride, 0);
            byte[] resultPixels = new byte[origPixels.Length];
            for (int i = 0; i < origPixels.Length; i += 4)
            {
                resultPixels[i] = (byte)Clamp(dilatedPixels[i] - origPixels[i]);
                resultPixels[i + 1] = (byte)Clamp(dilatedPixels[i + 1] - origPixels[i + 1]);
                resultPixels[i + 2] = (byte)Clamp(dilatedPixels[i + 2] - origPixels[i + 2]);
                resultPixels[i + 3] = origPixels[i + 3];
            }
            ImageProcessed.Source = CreateBitmapFromPixels(resultPixels, width, height, orig.DpiX, orig.DpiY);
        }

        private void MenuItem_GradientMorphologique_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Gradient morphologique = Dilatation - Érosion
            MenuItem_Dilatation_Click(sender, e);
            WriteableBitmap dilated = ConvertToWriteableBitmap((BitmapSource)ImageProcessed.Source);
            MenuItem_Erosion_Click(sender, e);
            WriteableBitmap eroded = ConvertToWriteableBitmap((BitmapSource)ImageProcessed.Source);
            int width = dilated.PixelWidth, height = dilated.PixelHeight;
            int stride = width * 4;
            byte[] dilatedPixels = new byte[height * stride];
            dilated.CopyPixels(dilatedPixels, stride, 0);
            byte[] erodedPixels = new byte[height * stride];
            eroded.CopyPixels(erodedPixels, stride, 0);
            byte[] resultPixels = new byte[dilatedPixels.Length];
            for (int i = 0; i < dilatedPixels.Length; i += 4)
            {
                resultPixels[i] = (byte)Clamp(dilatedPixels[i] - erodedPixels[i]);
                resultPixels[i + 1] = (byte)Clamp(dilatedPixels[i + 1] - erodedPixels[i + 1]);
                resultPixels[i + 2] = (byte)Clamp(dilatedPixels[i + 2] - erodedPixels[i + 2]);
                resultPixels[i + 3] = dilatedPixels[i + 3];
            }
            ImageProcessed.Source = CreateBitmapFromPixels(resultPixels, width, height, dilated.DpiX, dilated.DpiY);
        }

        private void MenuItem_ChapeauBlanc_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Chapeau blanc = Original - Ouverture
            WriteableBitmap orig = ConvertToWriteableBitmap((BitmapSource)ImageOriginal.Source);
            MenuItem_Ouverture_Click(sender, e);
            WriteableBitmap opened = ConvertToWriteableBitmap((BitmapSource)ImageProcessed.Source);
            int width = orig.PixelWidth, height = orig.PixelHeight;
            int stride = width * 4;
            byte[] origPixels = new byte[height * stride];
            orig.CopyPixels(origPixels, stride, 0);
            byte[] openedPixels = new byte[height * stride];
            opened.CopyPixels(openedPixels, stride, 0);
            byte[] resultPixels = new byte[origPixels.Length];
            for (int i = 0; i < origPixels.Length; i += 4)
            {
                resultPixels[i] = (byte)Clamp(origPixels[i] - openedPixels[i]);
                resultPixels[i + 1] = (byte)Clamp(origPixels[i + 1] - openedPixels[i + 1]);
                resultPixels[i + 2] = (byte)Clamp(origPixels[i + 2] - openedPixels[i + 2]);
                resultPixels[i + 3] = origPixels[i + 3];
            }
            ImageProcessed.Source = CreateBitmapFromPixels(resultPixels, width, height, orig.DpiX, orig.DpiY);
        }

        private void MenuItem_ChapeauNoir_Click(object sender, RoutedEventArgs e)
        {
            if (ImageOriginal.Source == null) return;
            // Chapeau noir = Fermeture - Original
            WriteableBitmap orig = ConvertToWriteableBitmap((BitmapSource)ImageOriginal.Source);
            MenuItem_Fermeture_Click(sender, e);
            WriteableBitmap closed = ConvertToWriteableBitmap((BitmapSource)ImageProcessed.Source);
            int width = orig.PixelWidth, height = orig.PixelHeight;
            int stride = width * 4;
            byte[] origPixels = new byte[height * stride];
            orig.CopyPixels(origPixels, stride, 0);
            byte[] closedPixels = new byte[height * stride];
            closed.CopyPixels(closedPixels, stride, 0);
            byte[] resultPixels = new byte[origPixels.Length];
            for (int i = 0; i < origPixels.Length; i += 4)
            {
                resultPixels[i] = (byte)Clamp(closedPixels[i] - origPixels[i]);
                resultPixels[i + 1] = (byte)Clamp(closedPixels[i + 1] - origPixels[i + 1]);
                resultPixels[i + 2] = (byte)Clamp(closedPixels[i + 2] - origPixels[i + 2]);
                resultPixels[i + 3] = origPixels[i + 3];
            }
            ImageProcessed.Source = CreateBitmapFromPixels(resultPixels, width, height, orig.DpiX, orig.DpiY);
        }

        #endregion
    }
}
