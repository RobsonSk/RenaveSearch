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
    public partial class SettingsWindow : Window
    {
        private string pfxPassword = string.Empty;

        public event EventHandler CertificateSelected;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private async void OnSelectCertificateClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
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
                string pfxPassword = await passwordDialog.ShowDialog<string>(this); // Pass 'this' to show the dialog on top of SettingsWindow

                if (!string.IsNullOrEmpty(pfxPassword))
                {
                    CertificateManager.StoreCertificateAndPassword(pfxFilePath, pfxPassword);
                    Console.WriteLine("Certificate path and password saved successfully.");

                    // Raise the CertificateSelected event
                    CertificateSelected?.Invoke(this, EventArgs.Empty);

                }
            }
        }
    }
}