# Introduction 
This is a sample client for sending requests to data.altinn.no [information in Norwegian](https://www.altinndigital.no/produkter/data.altinn/). developed in .NET Core. The purpose of the sample
client is twofold: To demonstrate how to effectively utilize the various functions available through data.altinn.no and to provide sample implementation for anyone wanting to write their own data.altinn.no client.

# Requirements
To use data.altinn.no and this client you will need the following
1. A subscription key for the data.altinn.no API. A subscription key can be obtained at [data.altinn.no/](https://data.altinn.no/). For access to be granted you must describe how you intend to use the data obtained from data.altinn.no and which data-sets you request access too. 
2. An enterprise certificate or a JWT token from Maskinporten. 
    - An Enterprise Certificate is a certificate that includes the organization number for your organization. Your organization must be registered in the Norwegian Entity Registry (Enhetsregisteret). A valid         enterprise certificate can be ordered from Buypass or Comfides

    - Maskinporten is a solution that guarantees the identity of entities exchanging data. It is the preferred method for machine to machine authentication [Information in Norwegian](https://samarbeid.digdir.no/maskinporten/maskinporten/25).   
3. Net Core 3.1 (or later) SDK or Visual Studio 2019


# Getting Started
The client is implemented as a console app. To get the app clone this repo and build the solution.

# Build
There are several different ways to build the solution. 

## Visual Studio 2019
Open the solution in Visual Studio 2019 and build it using the build tools (Build -> Build Solution)

## Command line
Open a command prompt window and navigate to the folder where you have cloned this repo. Then run the following command

``` shell
dotnet build
```

# Run

## Visual Studio 2019
Select 'Release' from the Solution Configurations menu and 'Any CPU' from the Solution Platforms menu in Visual Studio. Next click the button labeled dan-client-dotnet.

## Command line
Open a command prompt window and navigate to the folder where you have cloned this repo. Then run the following command

``` shell
dotnet run
```

## Windows Explorer
In a new Windows Explorer window navigate to the folder where you have cloned this repo. 
Navigate to the executable file by going to bin -> Release -> netcoreapp3.1 
Double click the file named 'dan-client-dotnet.exe'

# Usage
This client is set up to demonstrate a small set of the functionality available in data.altinn.no. The supported endpoints have been selected to most clearly demonstrate how to use
the service to request access to, and consume responses from an evidence source. Retrieving data from data.altinn.no is a process that requires certain steps to be performed, in it's 
simplest form this process can be broken down into two steps:

1. Authorization and request for access to a dataset
2. Retrieve data once access has been granted

This client can be configured to perform these steps. Configuration is read from appsettings.json which contains several sections that must be updated before the application can 
succesfully send requests to data.altinn.no

## HttpClientConfig
This section sets up the basic usage of the http client that will handle requests to and responses from data.altinn.no. This section must specify the enviroment 

``` json
"HttpClientConfig": {
    "SubscriptionKey": "xxxxxxxx", // Your subscription key
    "BaseAddress": "https://apim-nadobe-dev.azure-api.net/v1/" // URL to the environment the requests will be sent to
                                                               // Dev    : 'https://apim-nadobe-dev.azure-api.net/v1/'
                                                               // QA     : 'https://apim-nadobe-qa.azure-api.net'
                                                               // Staging: 'https://apim-nadobe-staging.azure-api.net'
                                                               // Prod   : 'https://api.data.altinn.no'

  }
```

## CertificateConfig
This section defines where the enterprise certificate is installed on your local machine, and which certificate the client should use to authenticate itself to data.altinn.no

``` json
 "CertificateConfig": {
    "Thumbprint": "xxxxxxxx",           // The Thumbprint of your certificate
    "StoreLocation": "LocalMachine",    // The location of the certificate store where the certificate is installed. Allowed values are "LocalMachine" and "CurrentUser"
    "StoreName": "My"                   // The name of the certificate store where the certificate is installed. Allowed values are "My", "Root" and "CertificateAuthority".
                                        // Note that in vast majority of cases the certificate should be installed in the certificate store called "Personal" and the StoreName should be set to "My"
  }
```

## MaskinportenConfig
``` json
    "CertificateConfig": {
        "token": "xxxxxxxxxxxxxxxxx"    // JWT token received from Maskinporten    
    }
```

## RequestConfig
This section is used to decide what type of request that will be sent to data.altinn.no

``` json
"RequestConfig": {
    "requestType": "Authorize"
  }
```

### Supported requestTypes:
Any type of request requires the client to be set up with a valid Subscription Key and either an Enterprise Certificate or a Maskinporten token. The client currently supports these request types:

### Authorize
Submit a request for access to a dataset. 

Please note that requests of this type requires a valid Accreditation request ((see below))[#accreditation] to be configured

### GetEvidence
Submits a request to retrieve data from data.altinn.no. To succesfully retrieve data using this request a valid accreditation must first have been created and the id of the accreditation
passed to the app from the Accreditation configuration [(see below)](#accreditation)

### GetAccreditation
Submits a request to get information about a valid accreditation. To succesfully retrieve data using this request a valid accreditation must first have been created and the id of the accreditation
passed to the app from the Accreditation configuration [(see below)](#accreditation)

### DeleteAccreditation
Submits a rquest to delete an accreditation. To complete a request of this type the id of a valid accreditation must be supplied in the Accreditation configuration ((see below))[]

### GetServicecontexts
Submits a request to retrieve all service contexts in data.altinn.no

## Accreditation
This section defines a request for access to a dataset in data.altinn.no. 

The most basic request is for publically available information and can be configured like this:

``` json
{
  "Accreditation": {
    "requestor": "974760673",   // Organization number of the entity making the request (must match the org. number the Enterprise Certificate is issued to)
    "subject": "998997801",     // Organization number of the entity a dataset is requested for
    "evidenceRequests": [
      {
        "evidenceCodeName": "UnitBasicInformation", // Name of the dataset being requested
        "requestConsent": false                     // Specify if the request for data should generate a consent request in Altinn for the subject of the request
                                                    // Note that some datasets requires consent from the subject before they can be provided by data.altinn.no
      }
    ]
  }
```

To request access to a dataset that requires the subject of the request to give consent to the data being delivered:

``` json
{
  "Accreditation": {
    "requestor": "974760673",
    "subject": "998997801",
    "evidenceRequests": [
      {
        "evidenceCodeName": "UnitBasicInformation",
        "requestConsent": true
      }
    ],
    "consentReference": "test",         // A string that will be visible in the consent request the subject receives in Altinn
    "externalReference": "dan-client",  // A value that can be used to trace consent requests (any string up to 64 characters)
    "languageCode": "no-nb"             // The language to use in the conesent request when it is shown in Altinn
  }
```

Please note that this client does not store the id of a created accreditation. If you intend to use the accreditation you create in additional requests to data.altinn.no you must copy the id
from the response and store it yourself, for example as a value in the appsettings.json configuration file.

When a request for accreditation has been processed, altinn.data.no returns a full accreditation object. This accreditation object will have a unique id that can be used to make subsequent requests 
for the dataset described by the accreditation object.

To pass this id to the client, configure the Accreditation section like this:
``` json
{
  "Accreditation": {
      accreditation_id: "xxxx-xxxxx-xxxxx-xxxxx" // The id of a valid accreditation
  }
}
```