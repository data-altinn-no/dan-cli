using System;
using System.Collections.Generic;
using System.Text;

namespace dan_client_dotnet.Config
{
    // This will be populated by the .NET runtime. See appsettings.json to specify the values to be used.
    public class MaskinportenConfig
    {
        public string ClientId { get; set; }
        public string Scopes { get; set; }
        public string Environment { get; set; }
    }
}
