# Gender Classifier API

A production-ready ASP.NET Core Web API that predicts gender from a given name using the Genderize.io API.

## Live API

Base URL:
http://genderize-api-env.eba-auswcq5k.eu-west-1.elasticbeanstalk.com

Example endpoint:
GET /api/classify?name=john

Full example:
http://genderize-api-env.eba-auswcq5k.eu-west-1.elasticbeanstalk.com/api/classify?name=john

## Features

- Gender prediction using the Genderize.io API
- Clean controller/service design
- Robust error handling
- Input validation
- Standardized response format
- Confidence scoring logic
- In-memory caching for performance
- Deployed on AWS Elastic Beanstalk

## How It Works

1. The client sends a name query parameter.
2. The API validates the input.
3. The API calls Genderize.io.
4. The response is processed.
5. The confidence rule is applied.
6. A structured JSON response is returned.

## Request

### Endpoint
GET /api/classify

### Query Parameter

| Parameter | Type | Required | Description |
| --- | --- | --- | --- |
| name | string | Yes | Name to classify |

## Success Response

### 200 OK

```json
{
  "status": "success",
  "data": {
    "name": "john",
    "gender": "male",
    "probability": 0.99,
    "sample_size": 1234,
    "is_confident": true,
    "processed_at": "2026-04-13T06:30:00Z"
  }
}
```

## Error Responses

### 400 Bad Request

```json
{
  "status": "error",
  "message": "Missing or empty name parameter"
}
```

### 422 Unprocessable Entity

```json
{
  "status": "error",
  "message": "No prediction available for the provided name"
}
```

### 502 Bad Gateway

```json
{
  "status": "error",
  "message": "Upstream service returned an error"
}
```

## Confidence Logic

A result is marked confident when both conditions are true:

- probability >= 0.7
- sample_size >= 100

## Caching

The API uses in-memory caching to reduce repeated Genderize API calls.

- Cache key format: `genderize:{name}`
- Cache duration: 10 minutes
- Only valid predictions are cached

## Tech Stack

- ASP.NET Core 8 Web API
- C#
- HttpClientFactory
- IMemoryCache
- AWS Elastic Beanstalk

## Project Structure

```text
src/
 └── GenderClassifierApi/
     ├── Controllers/
     ├── Models/
     ├── Services/
     └── Program.cs

tests/
 └── GenderClassifierApi.Tests/
```

## Running Locally

### Prerequisites

- .NET 8 SDK
- Visual Studio or VS Code

### Commands

```bash
git clone https://github.com/temmy-t/HNG_Task0_Genderize_API.git
cd HNG_Task0_Genderize_API
dotnet restore
dotnet run --project src/GenderClassifierApi
```

Then open:

```text
http://localhost:5000/api/classify?name=john
```

## Testing

Run the test suite with:

```bash
dotnet test
```

## Deployment to AWS Elastic Beanstalk

Typical deployment flow:

```bash
dotnet publish src/GenderClassifierApi/GenderClassifierApi.csproj -c Release -o publish
cd publish
eb deploy
```

## Notes

- The API returns standardized JSON for success and error cases.
- `processed_at` is generated dynamically for every request in UTC.
- CORS is enabled with `Access-Control-Allow-Origin: *`.

## Author

Temitope Olawole
