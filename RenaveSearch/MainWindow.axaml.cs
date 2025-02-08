using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Org.BouncyCastle.OpenSsl;
using Avalonia.Platform.Storage;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace RenaveSearch
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
                string? password = null;
                if (appLifetime?.MainWindow != null)
                {
                    password = await passwordDialog.ShowDialog<string>(appLifetime.MainWindow);
                }

                if (!string.IsNullOrEmpty(password))
                {
                    // Add the certificate using BouncyCastle
                    await AddCertificate(pfxFilePath, password);
                }
            }
        }

        private async Task AddCertificate(string pfxFilePath, string password)
        {
            await Task.Run(() =>
            {
                try
                {
                    // Load the PFX file
                    Pkcs12Store pkcs12Store = new Pkcs12Store(new FileStream(pfxFilePath, FileMode.Open, FileAccess.Read), password.ToCharArray());

                    // Extract the certificate and private key
                    string alias = null;
                    foreach (string a in pkcs12Store.Aliases)
                    {
                        if (pkcs12Store.IsKeyEntry(a))
                        {
                            alias = a;
                            break;
                        }
                    }

                    if (alias == null)
                    {
                        throw new Exception("No private key found in the PFX file.");
                    }

                    var keyEntry = pkcs12Store.GetKey(alias);
                    var certificateChain = pkcs12Store.GetCertificateChain(alias);

                    // Convert the certificate and private key to PEM format
                    using (var certWriter = new StreamWriter("cert.pem"))
                    {
                        var pemWriter = new PemWriter(certWriter);
                        foreach (var cert in certificateChain)
                        {
                            pemWriter.WriteObject(cert.Certificate);
                        }
                        pemWriter.WriteObject(keyEntry.Key);
                    }

                    // Certificate added successfully
                    Console.WriteLine("Certificate added successfully.");
                }
                catch (Exception ex)
                {
                    // Handle error
                    Console.WriteLine($"Error: {ex.Message}");
                }
            });
        }

        private async void OnConductApiSearchClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            await ConductApiSearch();
        }

        private async Task ConductApiSearch()
        {
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.ClientCertificates.Add(new X509Certificate2("cert.pem"));

                    using (var client = new HttpClient(handler))
                    {
                        // Implement the API search logic here using the added certificate
                        var response = await client.GetAsync("https://api.example.com/search");
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(content);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error
                Console.WriteLine($"Error: {ex.Message}");
            }
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