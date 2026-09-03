# Development Setup

## Prerequisites

- Windows 11
- Visual Studio Community 2026 with the .NET desktop development workload
- .NET 10 SDK

## Build and test

```powershell
dotnet restore
dotnet build NovaSourceG6.sln
dotnet test NovaSourceG6.sln
```

## Run the prototype

Open `NovaSourceG6.sln` in Visual Studio and start `NovaSourceG6Config`, or run:

```powershell
dotnet run --project application/NovaSourceG6Config
```

The prototype does not send data; it only opens and closes the selected port. Do not test a port that is already in use by another application.
