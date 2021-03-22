using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace dan_client_dotnet.Services
{
    class AccreditationService
    {
        public string GetAccreditationId(string accreditation)
        {
            var acc = JsonConvert.DeserializeObject<dynamic>(accreditation);
            return acc.id;
        }
    }
}
