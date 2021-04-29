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
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            HttpClientConfig clientConfig = new HttpClientConfig();
            config.GetSection("HttpClientConfig").Bind(clientConfig);

            CertificateConfig certificateConfig = new CertificateConfig();
            config.GetSection("CertificateConfig").Bind(certificateConfig);

            MaskinportenConfig maskinportenConfig = new MaskinportenConfig();
            config.GetSection("MaskinportenConfig").Bind(maskinportenConfig);

            var host = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<RequestConfig>(config.GetSection("RequestConfig"));
                    services.Configure<AccreditationConfig>(config.GetSection("Accreditation"));
                    services.Configure<MaskinportenConfig>(config.GetSection("MaskinportenConfig"));
                    services.Configure<CertificateConfig>(config.GetSection("CertificateConfig"));

                    services.AddTransient<RequestService>();
                    services.AddTransient<AccreditationService>();
                    services.AddTransient<MaskinportenService>();

                    services.AddHttpClient("", c =>
                    {
                        c.BaseAddress = new Uri(clientConfig.BaseAddress);
                        c.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", clientConfig.SubscriptionKey);
                    });

                }).Build();
        
            RequestService requestHandler = host.Services.GetRequiredService<RequestService>();
            Console.WriteLine(await requestHandler.Demo());

            await host.RunAsync();
        }
    }
}
