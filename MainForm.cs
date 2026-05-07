using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MediComOCR
{
    public class MainForm : Form
    {
        private readonly Panel _headerPanel;
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly string _ocrFilePath;
        private readonly Label _fileLabel;
        private readonly RichTextBox _outputText;
        private readonly Label _statusLabel;

        public MainForm(string ocrFilePath)
        {
            _ocrFilePath = ocrFilePath;
            Text = "MediCom OCR";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 620);
            Size = new Size(1024, 700);
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 10f);

            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(34, 73, 255)
            };

            _titleLabel = new Label
            {
                Text = "MediCom OCR",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 22f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(28, 24)
            };

            _subtitleLabel = new Label
            {
                Text = "Prevod fotografií na text pomocou Windows OCR API",
                ForeColor = Color.FromArgb(230, 235, 255),
                Font = new Font("Segoe UI", 11f),
                AutoSize = true,
                Location = new Point(31, 72)
            };

            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_subtitleLabel);

            _fileLabel = new Label
            {
                Text = string.IsNullOrWhiteSpace(_ocrFilePath)
                    ? "OCR súbor nebol zadaný ako parameter."
                    : $"Súbor: {Path.GetFileName(_ocrFilePath)}",
                ForeColor = Color.FromArgb(90, 95, 110),
                AutoSize = true,
                Location = new Point(30, 156)
            };

            _outputText = new RichTextBox
            {
                Location = new Point(30, 190),
                Width = 944,
                Height = 420,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 33, 40),
                Font = new Font("Segoe UI", 11f),
                ReadOnly = true,
                DetectUrls = false
            };

            _statusLabel = new Label
            {
                Text = "Pripravené.",
                ForeColor = Color.FromArgb(90, 95, 110),
                AutoSize = true,
                Location = new Point(30, 635),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };

            Controls.Add(_headerPanel);
            Controls.Add(_fileLabel);
            Controls.Add(_outputText);
            Controls.Add(_statusLabel);

            Resize += (_, __) => _statusLabel.Location = new Point(30, ClientSize.Height - 28);
            Shown += async (_, __) => await LoadOcrTextAsync();
        }

        public Task<string> GetTransformedTextAsync()
        {
            if (string.IsNullOrWhiteSpace(_ocrFilePath))
            {
                throw new InvalidOperationException("OCR súbor nebol zadaný ako parameter hlavného okna.");
            }

            return ExtractTextFromImageAsync(_ocrFilePath);
        }

        private async Task LoadOcrTextAsync()
        {
            if (string.IsNullOrWhiteSpace(_ocrFilePath))
            {
                _statusLabel.Text = "Chýba parameter súboru.";
                _outputText.Text = "Zadajte cestu k obrázku ako parameter aplikácie.";
                return;
            }

            if (!File.Exists(_ocrFilePath))
            {
                _statusLabel.Text = "Súbor neexistuje.";
                _outputText.Text = $"Zadaný OCR súbor neexistuje:\n{_ocrFilePath}";
                return;
            }

            _statusLabel.Text = "Spracovanie obrázka...";
            _outputText.Text = string.Empty;

            try
            {
                var text = await GetTransformedTextAsync();
                _outputText.Text = string.IsNullOrWhiteSpace(text)
                    ? "OCR nenašiel žiadny text."
                    : text;
                _statusLabel.Text = "Hotovo.";
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Chyba pri OCR.";
                _outputText.Text = $"OCR sa nepodarilo vykonať.\n\nDetail chyby:\n{ex.Message}";
            }
        }

        private static OcrEngine CreatePreferredOcrEngine()
        {
            var preferredTags = new[] { "sk-SK", "cs-CZ" };

            foreach (var tag in preferredTags)
            {
                var language = new Language(tag);
                if (OcrEngine.IsLanguageSupported(language))
                {
                    var preferredEngine = OcrEngine.TryCreateFromLanguage(language);
                    if (preferredEngine != null)
                    {
                        return preferredEngine;
                    }
                }
            }

            return OcrEngine.TryCreateFromUserProfileLanguages();
        }

        private static async Task<string> ExtractTextFromImageAsync(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            using (IRandomAccessStream stream = await file.OpenReadAsync())
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Ignore);

                OcrEngine ocrEngine = CreatePreferredOcrEngine();
                if (ocrEngine == null)
                {
                    throw new InvalidOperationException("Nie je dostupný OCR engine. Nainštalujte jazykový balík sk-SK alebo cs-CZ vo Windows.");
                }

                OcrResult result = await ocrEngine.RecognizeAsync(softwareBitmap);
                return result?.Text ?? string.Empty;
            }
        }
    }
}
