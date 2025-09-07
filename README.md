# D365 Marketing Forms API

A modern API for retrieving and rendering Dynamics 365 Marketing Forms in custom web applications.

## Overview

This solution provides a secure and efficient way to access and display marketing forms created in Dynamics 365 Customer Insights - Journeys (formerly Marketing) from any custom web application. The API handles authentication, caching, and data transformation to deliver optimized form content that can be embedded anywhere.

## Architecture

The solution consists of two main parts:

1. **Backend API** (.NET 9 Web API)
   - Secure communication with Dynamics 365 Dataverse
   - JWT-based authentication
   - In-memory caching for performance optimization
   - Form content delivery with slug support

2. **Frontend Demo** (React + Vite)
   - Example implementation
   - Form rendering components
   - Authentication handling

## Technologies Used

### Backend
- .NET 9
- ASP.NET Core Minimal API
- Microsoft.PowerPlatform.Dataverse.Client (v1.2.10)
- JWT Authentication
- In-memory caching
- OpenAPI/Swagger

### Frontend
- React
- Vite
- HTTPS development with auto-generated certificates

## Getting Started

### Prerequisites
- .NET 9 SDK
- Node.js and npm
- Access to a Dynamics 365 environment with marketing forms
- Visual Studio 2022 or later (recommended)

### Configuration

The solution uses `appsettings.json` for configuration:

```json
{
  "Dataverse": {
    "ConnectionString": "YOUR_CONNECTION_STRING",
    "UseConnectionString": true,
    "TimeoutInSeconds": 360
  },
  "Jwt": {
    "Key": "YOUR_SECRET_KEY",
    "Issuer": "ISSUER",
    "Audience": "AUDIENCE"
  },
  "ApiKeys": [
    "YOUR_API_KEY_1",
    "YOUR_API_KEY_2"
  ]
}
```

### Running the Solution

1. Clone the repository
2. Update the connection string in `appsettings.json` to point to your Dynamics 365 environment
3. Open the solution in Visual Studio and run it, or use the command line:

```bash
# Start the backend
cd D365MarketingForms.Server
dotnet run

# Start the frontend (in a separate terminal)
cd d365marketingforms.client
npm install
npm run dev
```

## API Endpoints

### Authentication

```
POST /token
```
Accepts an API key and returns a JWT token for subsequent API calls.

### Marketing Forms

```
GET /marketingforms
```
Returns a list of all available marketing forms.

```
GET /marketingforms/{idOrSlug}
```
Returns a specific marketing form by ID (GUID) or slug (URL-friendly name).

## Features

- **Secure API Access**: JWT-based authentication with API key validation
- **Performance Optimization**: Built-in caching reduces Dataverse API calls
- **Slug Support**: User-friendly URLs for marketing forms
- **OpenAPI Documentation**: Built-in Swagger UI at `/swagger`
- **CORS Support**: Configurable cross-origin resource sharing

## Development

### HTTP Request Examples

The solution includes an HTTP file with examples for all API endpoints. Open `D365MarketingForms.Server.http` to see and test these examples.

### Adding New Features

The project uses a minimal API approach, making it easy to extend:

1. Add new endpoints in `Program.cs`
2. Add corresponding service methods as needed
3. Update the HTTP examples file for testing

## License

[GPL 3.0](LICENSE)

## Acknowledgements

- Microsoft Dynamics 365 Marketing team
- .NET Minimal API documentation