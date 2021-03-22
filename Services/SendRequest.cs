using dan_client_dotnet.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace dan_client_dotnet.Services
{
    class SendRequest
    {
        private HttpClient httpClient { get; set; }
        private readonly AccreditationConfig _accreditation;
        private readonly RequestConfig _evidenceRequest;

        public SendRequest(IHttpClientFactory httpClientFactory, IOptions<AccreditationConfig> accreditation, IOptions<RequestConfig> evidenceRequest)
        {
            httpClient = httpClientFactory.CreateClient();
            _accreditation = accreditation.Value;
            _evidenceRequest = evidenceRequest.Value;
        }

        public async Task buildRequest()
        {
            if(_evidenceRequest.requestType == "GetEvidence")
            {
                string endpoint = "evidence/" + _accreditation.accreditationId + "/" + _evidenceRequest.evidenceCode;
                await GetRequest(endpoint);
                Console.WriteLine("called get evidence - " + _evidenceRequest.evidenceCode);
            }

            if(_evidenceRequest.requestType == "GetAccreditation")
            {
                string endpoint = "evidence/" + _accreditation.accreditationId;
                await GetRequest(endpoint);
                Console.WriteLine("called get accreditation");
            }

            if(_evidenceRequest.requestType == "Authorize")
            {
                string endpoint = "authorization";
                AccreditationConfig payload = _accreditation;
                await PostRequest(endpoint, payload);
            }

            if(_evidenceRequest.requestType == "DeleteAccreditation")
            {
                string endpoint = "accreditations/" + _accreditation.accreditationId;
                await DeleteRequest(endpoint);
            }

            if (_evidenceRequest.requestType == "GetServicecontexts")
            {
                string endpoint = "public/metadata/servicecontexts";
                await GetRequest(endpoint);
            }
        }

        public async Task GetRequest(string endpoint)
        {
            var result = await httpClient.GetAsync(endpoint);
            var response = await result.Content.ReadAsStringAsync();
            Console.WriteLine(httpClient.BaseAddress + endpoint + "\n");
            Console.WriteLine(response);
        }

        public async Task PostRequest(string endpoint, AccreditationConfig payload)
        {
            var accred = JsonConvert.SerializeObject(payload);

            HttpContent content = new StringContent(accred, Encoding.UTF8, "application/json");
            var result = await httpClient.PostAsync(endpoint, content);

            Console.WriteLine(httpClient.BaseAddress + endpoint + "\n");

            if (!result.IsSuccessStatusCode)
            {
                Console.Error.WriteLine(result.ReasonPhrase);
            }
            else
            {
                var response = await result.Content.ReadAsStringAsync();
                Console.WriteLine(response);
            }
        }

        public async Task DeleteRequest(string endpoint)
        {
            var result = await httpClient.DeleteAsync(endpoint);
            var response = await result.Content.ReadAsStringAsync();
            Console.WriteLine(response);
        }
    }
}
