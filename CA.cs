using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.Rosstandart;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using tb_lab;

namespace tb_lab
{
    internal class CA
    {
        public string pfxFileName = "";
        public X509Certificate x509;
        private char[] pfxPass = "".ToCharArray();
        private SecureRandom secureRandom = new();
        private AsymmetricKeyEntry prkBag;
        private X509CertificateEntry certBag;
        private Admin admin;
        public CA ()
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
            admin = new();
        }
        public string validateCertReq(string request, string? adminCertification)
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
            if ((adminCertification == null) && req.Verify())
            {
                adminCertification = admin.checkRequest(request);
            }
            if (adminCertification == "")
            {
                return "";
            }
            StringWriter stringWriter = new StringWriter();
            var pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(stringWriter);
            pemWriter.WriteObject(req.GetPublicKey());
            var toBeSignedBytes = Encoding.ASCII.GetBytes(stringWriter.GetStringBuilder().ToString());
            var adminPublicKey = (ECPublicKeyParameters)admin.x509.GetPublicKey();
            var adminPublicKeyParams = (ECGost3410Parameters)adminPublicKey.Parameters;
            var hashCode = DigestUtilities.CalculateDigest(adminPublicKeyParams.DigestParamSet.Id, toBeSignedBytes);
            var signer = new ECGost3410Signer();
            signer.Init(false, (AsymmetricKeyParameter)admin.x509.GetPublicKey());
            byte[] adminCertificationSignatureBytes = null!;
            try
            {
                adminCertificationSignatureBytes = Convert.FromBase64String(adminCertification!);
            }
            catch
            {
                return "";
            }
            Org.BouncyCastle.Math.BigInteger r = new Org.BouncyCastle.Math.BigInteger(adminCertificationSignatureBytes.Take(32).ToArray());
            Org.BouncyCastle.Math.BigInteger s = new Org.BouncyCastle.Math.BigInteger(adminCertificationSignatureBytes.Skip(32).Take(32).ToArray());
            if (signer.VerifySignature(hashCode, r, s))
            {
                Org.BouncyCastle.Math.BigInteger serial = new Org.BouncyCastle.Math.BigInteger(160, secureRandom);
                var certGen = new X509V3CertificateGenerator();
                certGen.SetSerialNumber(serial);
                certGen.SetIssuerDN(certBag.Certificate.IssuerDN);
                certGen.SetNotBefore(DateTime.UtcNow);
                certGen.SetNotAfter(DateTime.UtcNow.AddYears(1));
                certGen.SetPublicKey(req.GetPublicKey());
                certGen.SetSubjectDN(req.GetCertificationRequestInfo().Subject);
                var nonCriticalReqExts = req.GetRequestedExtensions().GetNonCriticalExtensionOids();
                var criticalReqExts = req.GetRequestedExtensions().GetCriticalExtensionOids();
                var reqExts = req.GetRequestedExtensions();
                var nc = nonCriticalReqExts.GetEnumerator();
                var cr = criticalReqExts.GetEnumerator();
                while (nc.MoveNext())
                {
                    var ext = reqExts.GetExtension((DerObjectIdentifier)nc.Current);
                    certGen.AddExtension((DerObjectIdentifier)nc.Current, false, ext.GetParsedValue());
                }
                while (cr.MoveNext())
                {
                    var ext = reqExts.GetExtension((DerObjectIdentifier)cr.Current);
                    certGen.AddExtension((DerObjectIdentifier)cr.Current, true, ext.GetParsedValue());
                }
                ISignatureFactory signatureFactory = new Asn1SignatureFactory(RosstandartObjectIdentifiers.id_tc26_signwithdigest_gost_3410_12_256.Id, (AsymmetricKeyParameter)prkBag.Key);
                var x509 = certGen.Generate(signatureFactory);
                bool flag;
                try
                {
                    x509.Verify(certBag.Certificate.GetPublicKey());
                    flag = true;
                }
                catch
                {
                    flag = false;
                }
                if (flag)
                {
                    stringWriter = new();
                    pemWriter = new Org.BouncyCastle.OpenSsl.PemWriter(stringWriter);
                    pemWriter.WriteObject(x509);
                    return stringWriter.GetStringBuilder().ToString();
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
        }
    }
}
