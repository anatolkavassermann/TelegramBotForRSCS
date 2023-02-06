using Org.BouncyCastle.Asn1.CryptoPro;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.Rosstandart;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;

namespace tb_lab
{
    internal class CreateCA
    {
        static public void CreateCAandCerts()
        {
            //Create CA keys
            var secureRandom = new SecureRandom();
            var caCurve = ECGost3410NamedCurves.GetByNameX9("Tc26-Gost-3410-12-256-paramSetA");
            var caDomainParams = new ECDomainParameters(caCurve.Curve, caCurve.G, caCurve.N, caCurve.H, caCurve.GetSeed());
            var caECGost3410Parameters = new ECGost3410Parameters(
                new ECNamedDomainParameters(new DerObjectIdentifier("1.2.643.7.1.2.1.1.1"), caDomainParams),
                new DerObjectIdentifier("1.2.643.7.1.2.1.1.1"),
                new DerObjectIdentifier("1.2.643.7.1.1.2.2"),
                null
            );
            var caECKeyGenerationParameters = new ECKeyGenerationParameters(caECGost3410Parameters, secureRandom);
            var caKeyGenerator = new ECKeyPairGenerator();
            caKeyGenerator.Init(caECKeyGenerationParameters);
            var caKeyPair = caKeyGenerator.GenerateKeyPair();

            //Create CA selfsigned Cert
            Org.BouncyCastle.Math.BigInteger caSerial = new Org.BouncyCastle.Math.BigInteger(160, secureRandom);
            var caCertGen = new X509V3CertificateGenerator();
            caCertGen.SetSerialNumber(caSerial);
            caCertGen.SetIssuerDN(new X509Name("CN=LabBotMIIGAiK CA"));
            caCertGen.SetNotBefore(DateTime.UtcNow);
            caCertGen.SetNotAfter(DateTime.UtcNow.AddYears(15));
            caCertGen.SetPublicKey(caKeyPair.Public);
            caCertGen.SetSubjectDN(new X509Name("CN=LabBotMIIGAiK CA"));
            var caSubjectPbkInfo = new SubjectPublicKeyInfo(new AlgorithmIdentifier("1.2.643.7.1.2.1.1.1"), ((ECPublicKeyParameters)(caKeyPair.Public)).Q.GetEncoded());
            var caSubjectKeyID = new SubjectKeyIdentifier(caSubjectPbkInfo);
            caCertGen.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(511));
            caCertGen.AddExtension(new DerObjectIdentifier("2.5.29.37"), false, new DerSequence(new DerObjectIdentifier("1.3.6.1.5.5.7.3.2")));
            caCertGen.AddExtension(new DerObjectIdentifier("2.5.29.14"), false, new DerOctetString(caSubjectKeyID.GetKeyIdentifier()));
            var caPbkParams = (ECGost3410Parameters)((ECPublicKeyParameters)(caKeyPair.Public)).Parameters;
            ISignatureFactory signatureFactory = new Asn1SignatureFactory(RosstandartObjectIdentifiers.id_tc26_signwithdigest_gost_3410_12_256.Id, (AsymmetricKeyParameter)caKeyPair.Private);
            var caX509 = caCertGen.Generate(signatureFactory);
            WritePemObject(caX509, "");

            //Export CA PFX
            var caPkcs12Builder = new Pkcs12StoreBuilder();
            caPkcs12Builder.SetUseDerEncoding(true);
            var caStore = caPkcs12Builder.Build();
            caStore.SetKeyEntry("prk", new AsymmetricKeyEntry((AsymmetricKeyParameter)caKeyPair.Private), new X509CertificateEntry[] { new X509CertificateEntry(caX509) });
            caStore.SetCertificateEntry("cert", new X509CertificateEntry(caX509));
            var m = new MemoryStream();
            caStore.Save(m, "".ToCharArray(), secureRandom);
            var caData = m.ToArray();
            var caPkcs12Bytes = Pkcs12Utilities.ConvertToDefiniteLength(caData);
            File.WriteAllBytes("", caPkcs12Bytes);

            //Create Admin keys
            var adminCurve = ECGost3410NamedCurves.GetByNameX9("Tc26-Gost-3410-12-256-paramSetA");
            var adminDomainParams = new ECDomainParameters(adminCurve.Curve, adminCurve.G, adminCurve.N, adminCurve.H, adminCurve.GetSeed());
            var adminECGost3410Parameters = new ECGost3410Parameters(
                new ECNamedDomainParameters(new DerObjectIdentifier("1.2.643.7.1.2.1.1.1"), adminDomainParams),
                new DerObjectIdentifier("1.2.643.7.1.2.1.1.1"),
                new DerObjectIdentifier("1.2.643.7.1.1.2.2"),
                null
            );
            var adminECKeyGenerationParameters = new ECKeyGenerationParameters(adminECGost3410Parameters, secureRandom);
            var adminKeyGenerator = new ECKeyPairGenerator();
            adminKeyGenerator.Init(adminECKeyGenerationParameters);
            var adminKeyPair = adminKeyGenerator.GenerateKeyPair();

            //Create admin Cert
            Org.BouncyCastle.Math.BigInteger adminSerial = new Org.BouncyCastle.Math.BigInteger(160, secureRandom);
            var adminCertGen = new X509V3CertificateGenerator();
            adminCertGen.SetSerialNumber(adminSerial);
            adminCertGen.SetIssuerDN(new X509Name("CN=LabBotMIIGAiK CA"));
            adminCertGen.SetNotBefore(DateTime.UtcNow);
            adminCertGen.SetNotAfter(DateTime.UtcNow.AddYears(1));
            adminCertGen.SetPublicKey(adminKeyPair.Public);
            adminCertGen.SetSubjectDN(new X509Name("CN=LabBotMIIGAiK Admin"));
            var adminSubjectPbkInfo = new SubjectPublicKeyInfo(new AlgorithmIdentifier("1.2.643.7.1.2.1.1.1"), ((ECPublicKeyParameters)(adminKeyPair.Public)).Q.GetEncoded());
            var adminSubjectKeyID = new SubjectKeyIdentifier(adminSubjectPbkInfo);
            adminCertGen.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(511));
            adminCertGen.AddExtension(new DerObjectIdentifier("1.2.643.1.1.1.1.1.1"), true, new DerOctetString(Encoding.ASCII.GetBytes("Is admin")));
            adminCertGen.AddExtension(new DerObjectIdentifier("2.5.29.37"), false, new DerSequence(new DerObjectIdentifier("1.3.6.1.5.5.7.3.2")));
            adminCertGen.AddExtension(new DerObjectIdentifier("2.5.29.14"), false, new DerOctetString(adminSubjectKeyID.GetKeyIdentifier()));
            signatureFactory = new Asn1SignatureFactory(RosstandartObjectIdentifiers.id_tc26_signwithdigest_gost_3410_12_256.Id, (AsymmetricKeyParameter)caKeyPair.Private);
            var adminX509 = adminCertGen.Generate(signatureFactory);
            WritePemObject(adminX509, "");

            //Create admin PFX
            var adminPkcs12Builder = new Pkcs12StoreBuilder();
            adminPkcs12Builder.SetUseDerEncoding(true);
            var adminStore = adminPkcs12Builder.Build();
            adminStore.SetKeyEntry("prk", new AsymmetricKeyEntry((AsymmetricKeyParameter)adminKeyPair.Private), new X509CertificateEntry[] { new X509CertificateEntry(adminX509) });
            adminStore.SetCertificateEntry("cert", new X509CertificateEntry(adminX509));
            m = new MemoryStream();
            adminStore.Save(m, "".ToCharArray(), secureRandom);
            var adminData = m.ToArray();
            var adminPkcs12Bytes = Pkcs12Utilities.ConvertToDefiniteLength(caData);
            File.WriteAllBytes("", adminPkcs12Bytes);
        }

        private static void WritePemObject(Object _object, String _fileName)
        {
            TextWriter TextWriter = File.CreateText($".\\{_fileName}");
            var PemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(TextWriter);
            PemWriter.WriteObject(_object);
            TextWriter.Flush();
            TextWriter.Close();
            TextWriter.Dispose();
        }

        private static System.Object ReadPemObject(String _fileName)
        {
            TextReader TextReader = File.OpenText($".\\{_fileName}");
            var PemReader = new Org.BouncyCastle.OpenSsl.PemReader(TextReader);
            var _object = PemReader.ReadObject();
            TextReader.Close();
            TextReader.Dispose();
            return _object;
        }
    }
}
