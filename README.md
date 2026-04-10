# HNG Internship First Task
# Gender Classifier API (.NET)

A minimal ASP.NET Core Web API that calls the Genderize API and returns a processed response.

## Endpoint

`GET /api/classify?name={name}`

### Success
```json
{
  "status": "success",
  "data": {
    "name": "john",
    "gender": "male",
    "probability": 0.99,
    "sample_size": 1234,
    "is_confident": true,
    "processed_at": "2026-04-01T12:00:00Z"
  }
}
```

### Errors
```json
{ "status": "error", "message": "Missing or empty name parameter" }
```

```json
{ "status": "error", "message": "name is not a string" }
```

```json
{ "status": "error", "message": "No prediction available for the provided name" }
```

## Rules implemented

- `gender`, `probability`, and `count` are read from Genderize
- `count` is returned as `sample_size`
- `is_confident = probability >= 0.7 && sample_size >= 100`
- `processed_at` is generated per request in UTC ISO 8601 format
- CORS allows all origins and also explicitly returns `Access-Control-Allow-Origin: *`

## Run locally

```bash
dotnet restore
dotnet run --project src/GenderClassifierApi
```

## Test

```bash
dotnet test
```

## Example calls

```bash
curl "https://your-domain/api/classify?name=john"
curl "https://your-domain/api/classify"
curl "https://your-domain/api/classify?name=123"
```

## Deployment notes

- AWS Platform
- The deployed URL is public and HTTPS
- API response includes `Access-Control-Allow-Origin: *`

## Submission checklist

- API base URL
- GitHub repo link: https://github.com/temmy-t/HNG_Task0_Genderize_API.git
- full name: Temitope Olawole
- email: temitopeolawole@gmail.com
- stack: .NET (C#)
