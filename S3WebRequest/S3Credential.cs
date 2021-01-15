using System.Security;

namespace System.Net.S3
{
    internal class S3Credential : NetworkCredential, ICredentials
    {
        public S3Credential(NetworkCredential networkCredential) : base(networkCredential.UserName, networkCredential.SecurePassword, networkCredential.Domain)
        {
        }

        public S3Credential(string keyId, SecureString secretKey) : base(keyId, secretKey)
        {
        }

    }
}