using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Newtonsoft.Json.Linq;
using Avalonia.Markup.Xaml;

namespace RenaveSearch
{
    public partial class DataWindow : Window
    {
        private JObject jsonResponse;
        public DataWindow()
        {
            InitializeComponent();
            AtpvPanel.IsVisible = false;
            EntregaPanel.IsVisible = false;
        }

        private async void OnSearchDataClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await AtpvSearch();
            await EntregaSearch();
        }
        private async Task AtpvSearch()
        {
            try
            {
                var (certificateBytes, password) = CertificateManager.LoadCertificateAndPassword();
                // Load the certificate and private key from the PFX file
                var cert = new X509Certificate2(certificateBytes, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(cert);

                var loggingHandler = new LoggingHandler(handler);

                using (var client = new HttpClient(loggingHandler))
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "HttpClientFactory-Sample");

                    var chassi = ChassiTextBox.Text;
                    // Implement the API search logic here using the added certificate
                    Console.WriteLine($"Searching for data for chassi: {chassi}");
                    var response = await client.GetAsync($"https://renave.estaleiro.serpro.gov.br/renave-ws/api/pdf-atpv?chassi={chassi}");
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        StatusIndicator.Fill = Brushes.Green;
                        DownloadAtpvButton.IsEnabled = true;
                        AtpvPanel.IsVisible = true;
                        jsonResponse = JObject.Parse(content);
                    }
                    else
                    {
                        AtpvPanel.IsVisible = false;

                    }

                }
            }
            catch (Exception ex)
            {
                // Handle error
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }

        }
        private async Task EntregaSearch()
        {
            try
            {
                var (certificateBytes, password) = CertificateManager.LoadCertificateAndPassword();
                // Load the certificate and private key from the PFX file
                var cert = new X509Certificate2(certificateBytes, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(cert);

                var loggingHandler = new LoggingHandler(handler);

                using (var client = new HttpClient(loggingHandler))
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "HttpClientFactory-Sample");

                    var chassi = ChassiTextBox.Text;
                    // Implement the API search logic here using the added certificate
                    Console.WriteLine($"Searching for data for chassi: {chassi}");
                    var response = await client.GetAsync($"https://renave.estaleiro.serpro.gov.br/renave-ws/api/entregas-veiculo-zero-km?chassi={chassi}");
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(content);
                    if (response.IsSuccessStatusCode && (content != "[]"))
                    {
                        StatusIndicator2.Fill = Brushes.Green;
                        EntregaButton.IsEnabled = true;
                        EntregaPanel.IsVisible = true;
                    }
                    else
                    {
                        EntregaPanel.IsVisible = false;

                    }

                }
            }
            catch (Exception ex)
            {
                // Handle error
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }

        }
        private async void OnDownloadAtpvClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await DownloadAtpv();
        }
        private async Task DownloadAtpv()
        {
            if (jsonResponse != null)
            {
                // Use the jsonResponse to perform the download logic
                var pdfBase64 = jsonResponse["pdfAtpvBase64"]?.ToString();

                if (!string.IsNullOrEmpty(pdfBase64))
                {
                    var pdfBytes = Convert.FromBase64String(pdfBase64);

                    var options = new FilePickerSaveOptions
                    {
                        DefaultExtension = "pdf",
                        FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("PDF Files") { Patterns = new[] { "*.pdf" } }
                },
                        SuggestedFileName = $"{ChassiTextBox.Text}.pdf"
                    };

                    var result = await StorageProvider.SaveFilePickerAsync(options);
                    if (result != null)
                    {
                        await using (var stream = await result.OpenWriteAsync())
                        {
                            await stream.WriteAsync(pdfBytes);
                        }
                        Console.WriteLine($"PDF saved to {result.Path.LocalPath}");
                    }
                    else
                    {
                        Console.WriteLine("Save operation was cancelled.");
                    }
                }
                else
                {
                    Console.WriteLine("No PDF data found in the response.");
                }

            }
            else
            {
                Console.WriteLine("No data available to download.");
            }
        }
        public class TextInputDialog : Window
        {
            private TextBox _inputTextBox;
            private Button _okButton;
            private Button _cancelButton;

            public TextInputDialog(string title, string message)
            {
                Title = title;
                Width = 400;
                Height = 150;

                var stackPanel = new StackPanel { Margin = new Thickness(10) };

                var messageTextBlock = new TextBlock { Text = message, Margin = new Thickness(0, 0, 0, 10) };
                stackPanel.Children.Add(messageTextBlock);

                _inputTextBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
                stackPanel.Children.Add(_inputTextBox);

                var buttonPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };

                _okButton = new Button { Content = "OK", IsDefault = true };
                _okButton.Click += OkButton_Click;
                buttonPanel.Children.Add(_okButton);

                _cancelButton = new Button { Content = "Cancel", IsCancel = true, Margin = new Thickness(10, 0, 0, 0) };
                _cancelButton.Click += CancelButton_Click;
                buttonPanel.Children.Add(_cancelButton);

                stackPanel.Children.Add(buttonPanel);

                Content = stackPanel;
            }

            private void OkButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            {
                Close(_inputTextBox.Text);
            }

            private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            {
                Close(null);
            }
        }
    }
}