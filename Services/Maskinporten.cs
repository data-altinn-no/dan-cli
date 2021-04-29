using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using dan_client_dotnet.Config;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace dan_client_dotnet.Services
{
    public class MaskinportenService
    {

        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _scopes;
        private readonly string _tokenEndpoint;
        private readonly X509Certificate2 _signingCertificate;
        private readonly MaskinportenConfig _maskinportenConfig;
        private readonly CertificateConfig _certificateConfig;
        private string _cachedToken = null;
        private DateTime _cachedTokenExpires = DateTime.UtcNow;

        public string LastTokenRequest { get; private set; }
        public Exception LastException { get; private set; }
        public string CurlDebugCommand { get; private set; }

        public MaskinportenService(IOptions<MaskinportenConfig> maskinportenConfig, IOptions<CertificateConfig> certificateConfig)
        {
            _maskinportenConfig = maskinportenConfig.Value;
            _certificateConfig = certificateConfig.Value;

            switch (_maskinportenConfig.Environment.ToLower())
            {
                case "ver2":
                    _tokenEndpoint = "https://ver2.maskinporten.no/token";
                    _audience = "https://ver2.maskinporten.no/";
                    break;
                case "prod":
                    _tokenEndpoint = "https://maskinporten.no/token";
                    _audience = "https://maskinporten.no/";
                    break;
                default:
                    throw new ArgumentException("Invalid Maskinporten environment specified, must be either 'ver2' or 'prod'");
            }

            _scopes = maskinportenConfig.Value.Scopes;
            _issuer = maskinportenConfig.Value.ClientId;

            // Thumbprint takes precendence if supplied
            if (!string.IsNullOrEmpty(_certificateConfig.Thumbprint))
            {
                _signingCertificate = GetCertificateFromKeyStore();
            }
            else
            {
                if (!File.Exists(_certificateConfig.Pkcs12FilePath))
                {
                    throw new ArgumentException("Unable to find PKCS#12 certificate at " + _certificateConfig.Pkcs12FilePath);
                }

                _signingCertificate = new X509Certificate2();
                _signingCertificate.Import(File.ReadAllBytes(_certificateConfig.Pkcs12FilePath), _certificateConfig.Pkcs12FileSecret, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }
        }

        public async Task<string> GetAccessToken(bool useCache = true)
        {
            if (useCache && _cachedToken != null && _cachedTokenExpires > DateTime.UtcNow)
            {
                WriteInfo("Using cached access token from Maskinporten");
                return _cachedToken;
            }

            WriteInfo("Getting access token from Maskinporten");

            var assertion = GetJwtAssertion();
            var (isError, token) = await GetTokenFromJwtBearerGrant(assertion);

            if (isError)
            {
                WriteError("Failed getting token: " + token);
                WriteError("Call made (formatted as curl command, also placed in clipboard):");
                WriteError(CurlDebugCommand);

                return string.Empty;
            }
            else
            {
                var tokenObject = JsonConvert.DeserializeObject<JObject>(token);

                _cachedToken = tokenObject.GetValue("access_token").ToString();
                _cachedTokenExpires = DateTime.UtcNow;
                
                if (int.TryParse(tokenObject.GetValue("expires_in").ToString(), out int expiresIn))
                {
                    _cachedTokenExpires = _cachedTokenExpires.AddSeconds(expiresIn);
                }
                
                WriteInfo("Received access token from Maskinporten, expires " + _cachedTokenExpires.ToLocalTime());

                return _cachedToken;
            }
        }

        private string GetJwtAssertion()
        {
            var dateTimeOffset = new DateTimeOffset(DateTime.UtcNow);

            var securityKey = new X509SecurityKey(_signingCertificate);
            var header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256))
            {
                {"x5c", new List<string>() {Convert.ToBase64String(_signingCertificate.GetRawCertData())}}
            };

            header.Remove("typ");
            header.Remove("kid");

            var payload = new JwtPayload
            {
                { "aud", _audience },
                { "scope", _scopes },
                { "iss", _issuer },
                { "exp", dateTimeOffset.ToUnixTimeSeconds() + 60 },
                { "iat", dateTimeOffset.ToUnixTimeSeconds() },
                { "jti", Guid.NewGuid().ToString() },
            };

            var securityToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(securityToken);
        }


        private async Task<(bool, string)> GetTokenFromJwtBearerGrant(string assertion)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var formContent = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new KeyValuePair<string, string>("assertion", assertion),
            });

            LastTokenRequest = await formContent.ReadAsStringAsync();
            return await SendTokenRequest(formContent);
        }

        private async Task<(bool, string)> SendTokenRequest(FormUrlEncodedContent formContent)
        {
            var client = new HttpClient();
            string responseString;
            CurlDebugCommand = "curl -v -X POST -d '" + formContent.ReadAsStringAsync().Result + "' " + _tokenEndpoint;
            try
            {
                var response = client.PostAsync(_tokenEndpoint, formContent).Result;
                responseString = await response.Content.ReadAsStringAsync();
                return (!response.IsSuccessStatusCode, responseString);
            }
            catch (Exception e)
            {
                LastException = e;
                PrettyPrintException(e);
                return (true, null);
            }
        }

        private X509Certificate2 GetCertificateFromKeyStore()
        {
            StoreLocation sl;
            if (_certificateConfig.StoreLocation.ToLowerInvariant() == "localmachine")
            {
                sl = StoreLocation.LocalMachine;
            }
            else if (_certificateConfig.StoreLocation.ToLowerInvariant() == "currentuser")
            {
                sl = StoreLocation.CurrentUser;
            }
            else
            {
                throw new ArgumentException("Invalid store location, valid values are 'LocalMachine' and 'CurrentUser'");
            }


            var store = new X509Store(_certificateConfig.StoreName, sl);

            store.Open(OpenFlags.ReadOnly);
            var certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, _certificateConfig.Thumbprint, false);
            var enumerator = certCollection.GetEnumerator();
            X509Certificate2 cert = null;
            while (enumerator.MoveNext())
            {
                cert = enumerator.Current;
            }

            if (cert == null)
            {
                throw new ArgumentException("Unable to find certificate in store with thumbprint: " + _certificateConfig.Thumbprint + ". Check your config, and make sure the certificate is installed in the \"" + _certificateConfig.StoreLocation + "\\" + _certificateConfig.StoreName + "\" store.");
            }

            return cert;
        }

        private static void PrettyPrintException(Exception e)
        {
            Console.WriteLine("############");
            Console.WriteLine("Failed request to token endpoint, Exception thrown: " + e.GetType().FullName);
            Console.WriteLine("Message:" + e.Message);
            Console.WriteLine("Stack trace:");
            Console.WriteLine(e.StackTrace);
            while (e.InnerException != null)
            {
                Console.WriteLine("Inner Exception:" + e.InnerException.GetType().FullName);
                Console.WriteLine("Message:" + e.InnerException.Message);
                Console.WriteLine("Stack trace:");
                Console.WriteLine(e.InnerException.StackTrace);
                e = e.InnerException;
            }

            Console.WriteLine("############");
        }

        private static void WriteInfo(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        private static void WriteError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}
