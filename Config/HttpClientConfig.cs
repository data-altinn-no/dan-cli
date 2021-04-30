using System;
using System.Collections.Generic;
using System.Text;

namespace dan_client_dotnet.Config
{
    // This will be populated by the .NET runtime. See appsettings.json to specify the values to be used.
    class HttpClientConfig
    {
        public string BaseAddress { get; set; }
        public string SubscriptionKey { get; set; }
    }
}
