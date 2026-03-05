# ClientRecords
Minimal ASP.NET Core Web API for storing and querying client records in a CSV-backed store.

## Short Description

ClientRecords exposes REST endpoints to create client records, query by client ID, and query by country code. Data is read from and persisted to a local CSV file for a lightweight, dependency-free setup.

## Build, Test, Run

### Build

`dotnet build ClientRecords.slnx`

### Test

`dotnet test .\ClientRecords.Tests\ClientRecords.Tests.csproj -v minimal`

### Run

- HTTP profile:

	`dotnet run --launch-profile http`

- HTTPS profile:

	`dotnet run --launch-profile https`

- Windows helper script (opens Swagger, then runs HTTPS profile):

	`.\launch-api.cmd`

## OpenAPI

When the app runs, Swagger UI is available at `/swagger` and the generated OpenAPI spec is at `/swagger/v1/swagger.json`.

## Functionality and Design Decisions

- API functionality:
	`POST /api/clients` creates a client record and assigns `ClientId`.
	`GET /api/clients/{id}` returns a single record by numeric ID.
	`GET /api/clients/{countryCode}` returns records filtered by country code.
- Storage design:
	Uses a CSV file (`ClientRecords:CsvPath`, default `clients.csv`) instead of a database to keep the sample small and easy to run.
- Concurrency design:
	`ClientService` uses `ReaderWriterLockSlim` so multiple reads can run concurrently while writes remain exclusive.
- Code organization:
	CSV parsing/format helpers were moved to `Shared/Utilities.cs` as extension methods (`ToClientRecord`, `ToCsvLine`) to keep service logic focused on behavior.
- Performance considerations:
	CSV file is re-read and parsed every time a GET or POST is invoked. This is done to allow
	dynamic loading of data without a need for restart. As data gets larger or reading data
	becomes more complex, this behavior should change to perform loading at the start and whenever file changes (might also need paginated loads if holding all data in memory becomes an issue).
