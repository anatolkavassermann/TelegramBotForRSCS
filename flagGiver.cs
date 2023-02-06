using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using tb_lab;

namespace tb_lab
{
    internal class flagGiver
    {
        private CA ca;
        public flagGiver() 
        {
            ca = new();
        }
        public string GimmeResource(string base64Signature, long StudentTID)
        {
            CmsSignedData cmsSignedData;
            try
            {
                cmsSignedData = new CmsSignedData(Convert.FromBase64String(base64Signature));
            }
            catch
            {
                throw new Exception("Error in cms message");
            }
            MemoryStream m = new();
            cmsSignedData.SignedContent.Write(m);
            Newtonsoft.Json.Linq.JObject requestedResource = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(Encoding.ASCII.GetString(m.ToArray()));
            if (requestedResource.Property("resource").Value.ToString() == "flag")
            {
                var certStoreInSig = cmsSignedData.GetCertificates("collection");
                ICollection sgnrs = cmsSignedData.GetSignerInfos().GetSigners();
                var e = sgnrs.GetEnumerator();
                bool flag = false;
                while (e.MoveNext())
                {
                    var sgnr = (SignerInformation)e.Current;
                    var certs = certStoreInSig.GetMatches(sgnr.SignerID);
                    var ee = certs.GetEnumerator();
                    while (ee.MoveNext())
                    {
                        var cert = (Org.BouncyCastle.X509.X509Certificate)ee.Current;
                        try
                        {
                            cert.Verify(ca.x509.GetPublicKey());
                            if (cert.GetCriticalExtensionOids().Contains(new DerObjectIdentifier("1.2.643.1.1.1.1.1.1").Id))
                            {
                                var encodedSignedAttributes = sgnr.GetEncodedSignedAttributes();
                                var sig = sgnr.GetSignature();
                                var publicKey = (ECPublicKeyParameters)cert.GetPublicKey();
                                var publicKeyParams = (ECGost3410Parameters)publicKey.Parameters;
                                var encodedSignedAttributesHash = DigestUtilities.CalculateDigest(publicKeyParams.DigestParamSet.Id, encodedSignedAttributes);
                                var r = new Org.BouncyCastle.Math.BigInteger(1, sig, 32, 32);
                                var s = new Org.BouncyCastle.Math.BigInteger(1, sig, 0, 32);
                                var gostVerifier = new ECGost3410Signer();
                                gostVerifier.Init(false, publicKey);
                                if (gostVerifier.VerifySignature(encodedSignedAttributesHash, r, s))
                                    flag = true;
                            }
                        }
                        catch
                        {

                        }
                    }
                }
                if (flag)
                {
                    return "OK";
                }
                else
                {
                    throw new Exception("You are not authorized to do that");
                }
            }
            else
            {
                throw new Exception("Only resource named \"flag\" is available");
            }
        }
    }
}