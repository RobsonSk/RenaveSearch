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

namespace RenaveSearch
{
    public partial class MainWindow : Window
    {
        private string pfxPassword = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            InitializeMainWindow();
        }

        private async Task InitializeMainWindow()
        {
            try
            {
                if (CertificateManager.AreEncryptedFilesPresent())
                {
                    var (certificate, password) = CertificateManager.LoadCertificateAndPassword();
                    Console.WriteLine("Certificate and password loaded successfully.");
                    await ValidateCertificate();
                }
                else
                {
                    Console.WriteLine("Encrypted files not found. Please add the certificate and password.");
                    CertificateStatusIndicator.Fill = Brushes.Red;
                    ApiSearchButton.IsEnabled = false;
                    // Optionally, prompt the user to add the certificate and password here
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading certificate and password: {ex.Message}");
                CertificateStatusIndicator.Fill = Brushes.Red;
                ApiSearchButton.IsEnabled = false;
            }
        }

        private async void OnAddCertificateClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select PFX Certificate",
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("PFX Files") { Patterns = new[] { "*.pfx" } }
                }
            });

            if (result != null && result.Count > 0)
            {
                string pfxFilePath = result[0].Path.LocalPath;

                // Prompt for the password
                var passwordDialog = new TextInputDialog("Enter Password", "Please enter the password for the PFX certificate:");
                var appLifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                if (appLifetime?.MainWindow != null)
                {
                    pfxPassword = await passwordDialog.ShowDialog<string>(appLifetime.MainWindow);
                }

                if (!string.IsNullOrEmpty(pfxPassword))
                {
                    CertificateManager.StoreCertificateAndPassword(pfxFilePath, pfxPassword);
                    // Console.WriteLine("Certificate path and password saved successfully.");
                }
            }
        }

        private void OnOpenDataClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var dataWindow = new DataWindow();
            dataWindow.Show();
        }

        private async Task ValidateCertificate()
        {
            try
            {
                var (certificateBytes, password) = CertificateManager.LoadCertificateAndPassword();

                // Console.WriteLine("Certificate bytes length: " + certificateBytes.Length);
                // Console.WriteLine("Password: " + password);

                // Load the certificate and private key from the PFX file
                var cert = new X509Certificate2(certificateBytes, password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                // Console.WriteLine($"Certificate valid from {cert.NotBefore} to {cert.NotAfter}");
                // foreach (var extension in cert.Extensions)
                // {
                //     Console.WriteLine($"{extension.Oid.FriendlyName}: {extension.Format(true)}");
                // }

                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(cert);

                // Add the client certificates to the request properties for logging
                var loggingHandler = new LoggingHandler(handler);

                using (var client = new HttpClient(loggingHandler))
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "HttpClientFactory-Sample");

                    // Implement the API search logic here using the added certificate
                    var response = await client.GetAsync("https://renave.estaleiro.serpro.gov.br/renave-ws/api/cliente-autenticado");
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response status code: {response.StatusCode}");
                    Console.WriteLine($"Response content: {content}");
                     if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = JObject.Parse(content);
                        string name = jsonResponse["nome"]?.ToString();
                        string cnpj = jsonResponse["cnpj"]?.ToString();

                        NameTextBox.Text = $"{name}";
                        CnpjTextBox.Text = $"{cnpj}";

                        CertificateStatusIndicator.Fill = Brushes.Green;
                        ApiSearchButton.IsEnabled = true;
                    }
                    else
                    {
                        CertificateStatusIndicator.Fill = Brushes.Red;
 
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
                CertificateStatusIndicator.Fill = Brushes.Red;
                ApiSearchButton.IsEnabled = false;
            }
        }
        

        private void OnOpenSettingsClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.CertificateSelected += OnCertificateSelected;
            settingsWindow.Show();
        }

        private async void OnCertificateSelected(object sender, EventArgs e)
        {
            await ValidateCertificate();
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