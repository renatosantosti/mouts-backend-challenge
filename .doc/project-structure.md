[Back to README](../README.md)

## Project Structure

The repository root holds documentation, Docker assets, and a **`backend`** folder that contains the .NET solution and all source code. The logical split is still **source projects** vs **test projects**, nested under `backend/`:

```
repository root
├── README.md
├── .doc/
├── backend/
│   ├── Ambev.DeveloperEvaluation.sln
│   ├── src/
│   │   ├── Ambev.DeveloperEvaluation.Domain
│   │   ├── Ambev.DeveloperEvaluation.Application
│   │   ├── Ambev.DeveloperEvaluation.Common
│   │   ├── Ambev.DeveloperEvaluation.ORM
│   │   ├── Ambev.DeveloperEvaluation.IoC
│   │   └── Ambev.DeveloperEvaluation.WebApi
│   └── tests/
│       ├── Ambev.DeveloperEvaluation.Unit
│       ├── Ambev.DeveloperEvaluation.Integration
│       └── Ambev.DeveloperEvaluation.Functional
├── docker-compose.yml
└── Dockerfile
```

Each name under `backend/src` and `backend/tests` is a **project folder** (matching its `.csproj`). Open the solution file `backend/Ambev.DeveloperEvaluation.sln` in your IDE to work across layers.
