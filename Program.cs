using dan_client_dotnet.Config;
using dan_client_dotnet.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace dan_client_dotnet
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            HttpClientConfig clientConfig = new HttpClientConfig();
            config.GetSection("HttpClientConfig").Bind(clientConfig);

            CertificateConfig certificateConfig = new CertificateConfig();
            config.GetSection("CertificateConfig").Bind(certificateConfig);

            MaskinportenConfig maskinportenConfig = new MaskinportenConfig();
            config.GetSection("MaskinportenConfig").Bind(maskinportenConfig);

            StoreName storeName = (StoreName)Enum.Parse(typeof(StoreName), certificateConfig.StoreName);
            StoreLocation storeLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), certificateConfig.StoreLocation);

            var host = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<RequestConfig>(config.GetSection("RequestConfig"));
                    services.Configure<AccreditationConfig>(config.GetSection("Accreditation"));

                    services.AddTransient<RequestService>();
                    services.AddTransient<AccreditationService>();

                    services.AddHttpClient("", c =>
                    {
                        c.BaseAddress = new Uri(clientConfig.BaseAddress);
                        c.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", clientConfig.SubscriptionKey);
                        if (!string.IsNullOrEmpty(maskinportenConfig.token)) {
                            c.DefaultRequestHeaders.Add("X-NADOBE-AUTHORIZATION", maskinportenConfig.token);
                        }
                    }).ConfigurePrimaryHttpMessageHandler(() =>
                    {
                        var handler = new HttpClientHandler();
                        handler.ClientCertificates.Add(GetCertificate(storeName, storeLocation, certificateConfig.Thumbprint));
                        return handler;
                    });

                }).Build();

        
            RequestService requestHandler = host.Services.GetRequiredService<RequestService>();
            Console.WriteLine(await requestHandler.Demo());

            await host.RunAsync();
        }

        static private X509Certificate2 GetCertificate(StoreName storeName, StoreLocation storeLocation, string thumbprint)
        {
            X509Certificate2 certificate = null;

            X509Store certStore = new X509Store(storeName, storeLocation);
            certStore.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certCollection = certStore.Certificates.Find(
                X509FindType.FindByThumbprint, thumbprint, false);
            if (certCollection.Count > 0)
            {
                certificate = certCollection[0];
            }

            certStore.Close();

            return certificate;
        }

    }
}
