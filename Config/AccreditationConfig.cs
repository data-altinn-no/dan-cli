﻿using dan_client_dotnet.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace dan_client_dotnet.Config
{
    // This will be populated by the .NET runtime. See appsettings.json to specify the values to be used.
    class AccreditationConfig
    {
        public string accreditationId { get; set; }
        public string requestor { get; set; }
        public string subject { get; set; }
        public List<EvidenceRequest> evidenceRequests { get; set;}
        public string consentReference { get; set; }
        public string externalReference { get; set; }
        public string languageCode { get; set; }
    }
}
