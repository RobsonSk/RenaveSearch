using System;
using System.IO;

public static class CertificateManager
{
    public static void StoreCertificateAndPassword(string certificatePath, string password)
    {
        if (string.IsNullOrEmpty(certificatePath))
        {
            throw new ArgumentException("Certificate path cannot be null or empty.", nameof(certificatePath));
        }

        var certificateBytes = File.ReadAllBytes(certificatePath);
        var encryptedCertificate = EncryptionHelper.Encrypt(Convert.ToBase64String(certificateBytes));
        var encryptedPassword = EncryptionHelper.Encrypt(password);

        string storagePath = GetStoragePath();
        File.WriteAllText(Path.Combine(storagePath, "certificate.enc"), encryptedCertificate);
        File.WriteAllText(Path.Combine(storagePath, "password.enc"), encryptedPassword);

        // Console.WriteLine("Encrypted certificate: " + encryptedCertificate);
        // Console.WriteLine("Encrypted password: " + encryptedPassword);
    }

    public static (byte[] certificate, string password) LoadCertificateAndPassword()
    {
        string storagePath = GetStoragePath();
        string certificateFilePath = Path.Combine(storagePath, "certificate.enc");
        string passwordFilePath = Path.Combine(storagePath, "password.enc");

        if (!File.Exists(certificateFilePath) || !File.Exists(passwordFilePath))
        {
            throw new FileNotFoundException("Encrypted certificate or password file not found.");
        }

        var encryptedCertificate = File.ReadAllText(certificateFilePath);
        var encryptedPassword = File.ReadAllText(passwordFilePath);

        // Console.WriteLine("Encrypted certificate from file: " + encryptedCertificate);
        // Console.WriteLine("Encrypted password from file: " + encryptedPassword);

        var certificateBase64 = EncryptionHelper.Decrypt(encryptedCertificate);
        // Console.WriteLine("Decrypted certificate from file: " + certificateBase64);
        var certificate = Convert.FromBase64String(certificateBase64);
        var password = EncryptionHelper.Decrypt(encryptedPassword);

        // Console.WriteLine("Decrypted certificate: " + certificateBase64);
        // Console.WriteLine("Decrypted password: " + password);

        return (certificate, password);
    }

    public static bool AreEncryptedFilesPresent()
    {
        string storagePath = GetStoragePath();
        string certificateFilePath = Path.Combine(storagePath, "certificate.enc");
        string passwordFilePath = Path.Combine(storagePath, "password.enc");

        return File.Exists(certificateFilePath) && File.Exists(passwordFilePath);
    }

    private static string GetStoragePath()
    {
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string storageDirectory = Path.Combine(homeDirectory, ".renave_search");

        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }

        return storageDirectory;
    }
}