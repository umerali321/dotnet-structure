using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SkillsetsBackend.Infrastructure.Skillsoft;


internal static class DevSelfSignedCertificate
{
    private static readonly Lazy<X509Certificate2> Instance = new(Create);

    public static X509Certificate2 GetOrCreate() => Instance.Value;

    private static X509Certificate2 Create()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=skillsetsbackend-dev-saml-signing (NOT TRUSTED BY SKILLSOFT)",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(2));
        return X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pfx),
            password: null,
            X509KeyStorageFlags.EphemeralKeySet);
    }
}
