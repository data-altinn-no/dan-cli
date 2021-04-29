using dan_client_dotnet.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace dan_client_dotnet.Services
{
    class RequestService
    {
        private HttpClient httpClient { get; set; }
        private AccreditationService _accreditationService { get; set; }
        private readonly MaskinportenService _maskinportenService;
        private readonly AccreditationConfig _accreditation;
        private readonly RequestConfig _evidenceRequest;


        public RequestService(IHttpClientFactory httpClientFactory, IOptions<AccreditationConfig> accreditation, IOptions<RequestConfig> evidenceRequest, AccreditationService accreditationService, MaskinportenService maskinportenService)
        {
            httpClient = httpClientFactory.CreateClient();
            _maskinportenService = maskinportenService;            
            _accreditationService = accreditationService;
            _accreditation = accreditation.Value;
            _evidenceRequest = evidenceRequest.Value;
        }

        public async Task<string> sendRequest()
        {
            var token = await _maskinportenService.GetAccessToken();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var request = GetRequestInfo(_evidenceRequest.requestType);

            if(request.action == "GET")
            {
                return await GetRequest(request.endpoint);
            }

            if(request.action == "POST")
            {
                AccreditationConfig payload = _accreditation;
                return await PostRequest(request.endpoint, payload);
            }

            if (request.action == "DELETE")
            {
                return await DeleteRequest(request.endpoint);
            }

            return string.Empty;
        }


        public async Task<string> GetRequest(string endpoint)
        {
            var result = await httpClient.GetAsync(endpoint);
            var response = await result.Content.ReadAsStringAsync();
            return response;
        }

        public async Task<string> PostRequest(string endpoint, AccreditationConfig payload)
        {
            var accred = JsonConvert.SerializeObject(payload);

            HttpContent content = new StringContent(accred, Encoding.UTF8, "application/json");
            var result = await httpClient.PostAsync(endpoint, content);


            if (!result.IsSuccessStatusCode)
            {
                Console.Error.WriteLine(result.ReasonPhrase);
                return string.Empty;
            }
            else
            {
                var response = await result.Content.ReadAsStringAsync();
                return response;
            }

        }

        public async Task<string> DeleteRequest(string endpoint)
        {
            var result = await httpClient.DeleteAsync(endpoint);
            var response = await result.Content.ReadAsStringAsync();
            return result.StatusCode == System.Net.HttpStatusCode.NoContent ? "204 received, deletion was successful" : response;
        }

        public async Task<string> Demo()
        {
            WriteHeader("Demonstration of a call chain to data.altinn.no for retrieving the evidencecode 'UnitBasicInformation'\n");
            Console.WriteLine("Initial call for authentication and creating an accreditation:");
            Console.WriteLine("Sending POST request to " + httpClient.BaseAddress + "authorization");
            Console.WriteLine("Request body:");
            // The accreditation request stored in _accreditation is read from configuration. See the AccreditationConfig section in application.json
            Console.WriteLine(JsonConvert.SerializeObject(_accreditation, Formatting.Indented));
            _evidenceRequest.requestType = "Authorize";
            var acc = await sendRequest();
            PrettyPrint(acc);

            Console.WriteLine("Pausing ...");
            Thread.Sleep(3000);

            // Store the accreditation id for use in subsequent calls. When using this client to run individual calls agains data.altinn.no the
            // accreditation id can be stored in apsettings.json by adding a value for accreditationId in the AccreditationConfig section
            _accreditation.accreditationId = _accreditationService.GetAccreditationId(acc);
            WriteHeader("\n\nCall to verify that the accreditation created in the previous step is valid and requests the correct data:");
            Console.WriteLine("Sending GET request to " + httpClient.BaseAddress + "evidence/" + _accreditation.accreditationId);
            _evidenceRequest.requestType = "GetAccreditation";
            PrettyPrint(await sendRequest());

            Console.WriteLine("Pausing ...");
            Thread.Sleep(5000);

            WriteHeader("\n\nCall to retrieve the data specified by the accreditation:");
            Console.WriteLine("Sending GET request to " + httpClient.BaseAddress + "evidence/" + _accreditation.accreditationId + "/UnitBasicInformation\n");
            _evidenceRequest.requestType = "GetEvidence";
            PrettyPrint(await sendRequest());

            Console.WriteLine("Pausing ...");
            Thread.Sleep(5000);

            WriteHeader("\n\nClean up by making call to delete the accreditation after the data has been retrieved:");
            Console.WriteLine("Sending DELETE request to " + httpClient.BaseAddress + "accreditations/" + _accreditation.accreditationId);
            _evidenceRequest.requestType = "DeleteAccreditation";
            Console.WriteLine(await sendRequest());

            Console.WriteLine("");
            Console.WriteLine("Demo complete, hit CTRL-C to exit");
            return "";
        }

        private (string endpoint, string action) GetRequestInfo(string requestType)
        {
            var result = ("", "");
            switch (requestType)
            {
                case "GetEvidence":
                    result =  ("evidence/" + _accreditation.accreditationId + "/" + _accreditation.evidenceRequests[0].EvidenceCodeName, "GET");
                    break;
                case "GetAccreditation":
                    result = ("evidence/" + _accreditation.accreditationId, "GET");
                    break;
                case "Authorize":
                    result = ("authorization", "POST");
                    break;
                case "DeleteAccreditation":
                    result = ("accreditations/" + _accreditation.accreditationId, "DELETE");
                    break;
                case "GetServicecontexts":
                    result = ("public/metadata/servicecontexts", "GET");
                    break;
                default:
                    result = ("public/metadata/evidencecodes", "GET");
                    break;
            }
            return result;
        }

        static private void PrettyPrint(string json)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(json), Formatting.Indented));
            Console.ResetColor();
        }

        static void WriteHeader(string text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}
