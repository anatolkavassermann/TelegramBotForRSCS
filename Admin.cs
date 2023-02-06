//net 6.0
using System.IO;

//BouncyCastle 1.9.0
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Asn1.Ess;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.X509.Store;
using Org.BouncyCastle.Asn1.CryptoPro;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Asn1.Rosstandart;
using Org.BouncyCastle.Utilities.IO.Pem;
using System.Text;

namespace tb_lab
{
    internal class Admin
    {
        public string pfxFileName = "";
        public X509Certificate x509;
        private char[] pfxPass = "".ToCharArray();
        private SecureRandom secureRandom = new ();
        private AsymmetricKeyEntry prkBag;
        private X509CertificateEntry certBag;
        public Admin() 
        {
            var pfxBytes = File.ReadAllBytes(pfxFileName);
            var builder = new Pkcs12StoreBuilder();
            builder.SetUseDerEncoding(true);
            var store = builder.Build();
            var m = new MemoryStream(pfxBytes);
            store.Load(m, pfxPass);
            m.Close();
            prkBag = store.GetKey("prk");
            certBag = store.GetCertificate("cert");
            x509 = certBag.Certificate;
        }

        public string checkRequest(string request)
        {
            Pkcs10CertificationRequest req;
            try
            {
                StringReader stringReader = new StringReader(request);
                var pemReader = new Org.BouncyCastle.OpenSsl.PemReader(stringReader);
                req = (Pkcs10CertificationRequest)pemReader.ReadObject();
                if (req == null)
                    req = new(Convert.FromBase64String(request));
                stringReader.Close();
            }
            catch
            {
                    return "";
            }
            var cryticalExtensionsOIDs = req.GetRequestedExtensions().GetCriticalExtensionOids();
            if (!cryticalExtensionsOIDs.Contains(new DerObjectIdentifier("1.2.643.1.1.1.1.1.1")) && req.Verify())
            {
                StringWriter stringWriter = new StringWriter();
                var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(stringWriter);
                pemWriter.WriteObject(req.GetPublicKey());
                var toBeSignedBytes = Encoding.ASCII.GetBytes(stringWriter.GetStringBuilder().ToString());
                var publicKey = (ECPublicKeyParameters)certBag.Certificate.GetPublicKey();
                var publicKeyParams = (ECGost3410Parameters)publicKey.Parameters;
                var hashCode = DigestUtilities.CalculateDigest(publicKeyParams.DigestParamSet.Id, toBeSignedBytes);
                var signer = new ECGost3410Signer();
                var paramsWithRandom = new ParametersWithRandom(this.prkBag.Key, this.secureRandom);
                signer.Init(true, paramsWithRandom);
                var signature = signer.GenerateSignature(hashCode);
                List<byte> sig = new List<byte>();
                sig.AddRange(signature[0].ToByteArrayUnsigned());
                sig.AddRange(signature[1].ToByteArrayUnsigned());
                return Convert.ToBase64String(sig.ToArray());
            }
            else
            {   
                return "";
            }
        }
    }
}