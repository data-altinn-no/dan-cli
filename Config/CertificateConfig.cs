using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace dan_client_dotnet.Config
{
    public class CertificateConfig
    {
        public string Thumbprint { get; set; }
        public string StoreName { get; set; }
        public string StoreLocation { get; set; }
        public string Pkcs12FilePath { get; set; }
        public string Pkcs12FileSecret { get; set; }
    }
}
