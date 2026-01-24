# Clean Architecture Startup Template

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=.net)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14-239120?style=flat-square&logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)

---

## 📋 Overview

A **.NET 10 backend** implementing Clean Architecture principles with CQRS, Domain-Driven Design, and comprehensive error handling. This template serves as a modern foundation for building scalable, maintainable, and testable backend services using C# 14's latest features.

Built with strict separation of concerns, explicit error handling via the Result pattern, and async-first design.

---

## ✨ Key Features

### Clean Architecture Foundation
- **Layered Architecture:** Domain → Application → Infrastructure → Web.Api with strict one-way dependencies
- **Domain-Driven Design:** Rich domain entities with factory methods and domain events
- **CQRS Pattern:** Segregated command (mutations) and query (reads) pipelines via MediatR-like abstractions
- **Event-Driven Architecture:** Domain events dispatched through `IDomainEventsDispatcher` for decoupled side effects

### Modern .NET & C# 14
- **Framework:** .NET 10 LTS with implicit usings and nullable reference types enabled
- **Language Features:** Primary constructors, collection expressions (`[]`), required properties, file-scoped namespaces
- **Type Safety:** Strict null checking and compile-time guarantees across the entire codebase
- **Async-First:** All I/O operations are async by default; blocking calls are forbidden

### Explicit Error Handling
- **Result Pattern:** All operations return `Result<TValue>` or `Result` for predictable, composable error handling
- **No Silent Failures:** Every business operation explicitly communicates success or failure with detailed error codes
- **Validation Layer:** FluentValidation decorators enforce input validation before handler execution
- **Structured Logging:** Serilog integration with contextual request logging via decorators

### Production-Ready Infrastructure
- **Database:** Entity Framework Core with PostgreSQL and automatic migrations
- **Authentication & Authorization:** JWT token support with custom permission-based authorization (`HasPermissionAttribute`)
- **Distributed Caching:** Redis integration via `StackExchange.Redis` with automatic query caching decorator
- **Rate Limiting:** Built-in ASP.NET Core rate limiting with configurable policies (5 req/min for auth, 1000 req/hour for users)
- **API Versioning:** URL-based versioning (`/v1/...`) with backward compatibility support
- **Health Checks:** Built-in health check endpoints with UI client integration
- **API Documentation:** Swagger/OpenAPI endpoints with auto-generated documentation

---

## 🏗️ Architecture Layers

### Domain Layer (`src/Domain/`)
Core business logic and rules isolated from external concerns.

- **Entities:** `User`, `TodoItem` with rich behavior and factory methods
- **Value Objects:** `Priority` enum, error definitions
- **Domain Events:** `UserRegisteredDomainEvent`, `TodoItemCreatedDomainEvent`, `TodoItemCompletedDomainEvent`
- **Error Catalog:** Centralized error definitions via `UserErrors`, `TodoItemErrors` static classes

**No external dependencies**—depends only on `SharedKernel`.

### Application Layer (`src/Application/`)
Use cases, orchestration, and cross-cutting concerns.

- **Command Handlers:** Process mutations (Create, Update, Delete) returning results
- **Query Handlers:** Retrieve data via queries, return DTOs
- **Validators:** FluentValidation rules enforcing input constraints
- **Decorators:** Logging, validation, and transaction management via pipeline behaviors
- **DTOs:** `TodoResponse`, `UserResponse` for safe API data transfer

### Infrastructure Layer (`src/Infrastructure/`)
External services, persistence, and implementation details.

- **Database Context:** `ApplicationDbContext` with EF Core configuration
- **Repositories:** Data access abstractions and implementations
- **Authentication:** Token providers, password hashers, JWT claims extraction
- **Authorization:** Permission-based authorization policies
- **Domain Event Dispatcher:** Publishes domain events to registered handlers
- **Time Provider:** Abstracted `IDateTimeProvider` for testable datetime operations

### Web.Api Layer (`src/Web.Api/`)
HTTP endpoints, middleware, and request/response transformation.

- **Endpoints:** REPR pattern (Request → Endpoint → Response) using minimal APIs
- **Middleware:** Request context logging, exception handling, CORS
- **Exception Handler:** Global handler converting exceptions to structured error responses
- **Result Extensions:** Converting `Result<T>` to HTTP responses (200, 400, 404, 500, etc.)

### SharedKernel (`src/SharedKernel/`)
Foundational abstractions shared across all layers.

- **Base Types:** `Entity`, `Result<TValue>`, `Error`, `ValidationError`
- **Interfaces:** `IDomainEvent`, `IDomainEventHandler<T>`, `IDateTimeProvider`
- **No Business Logic**—purely foundational.

---

## 🚀 Quick Start

### Prerequisites
- **.NET 10 SDK** ([Download](https://dotnet.microsoft.com/download))
- **PostgreSQL** (local or Docker)

### Installation

```bash
# Clone the repository
git clone https://github.com/veysoff/clean-architecture-startup-template.git
cd clean-architecture-startup-template

# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update --project src/Infrastructure

# Run the application
dotnet run --project src/Web.Api
```

### Docker Setup

```bash
# Build and run with Docker Compose
docker-compose up --build

# Application will be available at http://localhost:5000
```

---

## 🔑 Core Concepts

### The Result Pattern

All operations return `Result<T>` for explicit error handling:

```csharp
public sealed class LoginUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider) : ICommandHandler<LoginUserCommand, LoginUserResponse>
{
    public async Task<Result<LoginUserResponse>> Handle(
        LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, cancellationToken);

        if (user is null)
            return Result.Failure<LoginUserResponse>(UserErrors.InvalidCredentials);

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
            return Result.Failure<LoginUserResponse>(UserErrors.InvalidCredentials);

        var token = tokenProvider.GenerateToken(user);
        return Result.Success(new LoginUserResponse { Token = token });
    }
}
```

**Key benefits:**
- No exceptions for expected business failures
- Composable and chainable operations
- Explicit success/failure paths in code
- Testable without mocking or exception handling

### Domain Events

Domain entities raise events to communicate business-significant changes:

```csharp
public static Result<TodoItem> Create(
    Guid userId,
    string description,
    DateTime? dueDate,
    Priority priority)
{
    if (string.IsNullOrWhiteSpace(description))
        return Result.Failure<TodoItem>(TodoItemErrors.DescriptionRequired);

    var todo = new TodoItem
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Description = description,
        DueDate = dueDate,
        Priority = priority,
        CreatedAt = DateTime.UtcNow
    };

    // Domain event: captured and dispatched by infrastructure
    todo.RaiseDomainEvent(new TodoItemCreatedDomainEvent(todo.Id));

    return Result.Success(todo);
}
```

Event handlers process side effects (logging, notifications, etc.) without coupling the domain to infrastructure:

```csharp
public sealed class UserRegisteredDomainEventHandler(
    IEmailService emailService,
    ILogger<UserRegisteredDomainEventHandler> logger) : IDomainEventHandler<UserRegisteredDomainEvent>
{
    public async Task Handle(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await emailService.SendWelcomeEmailAsync(domainEvent.UserId, cancellationToken);
        logger.LogInformation("Welcome email sent for user {UserId}", domainEvent.UserId);
    }
}
```

### Async-First Design

All I/O operations are async; blocking operations are forbidden:

```csharp
// ✅ Correct: async/await throughout
public async Task<Result<TodoResponse>> Handle(
    GetTodoByIdQuery query,
    CancellationToken cancellationToken)
{
    var todo = await _todoRepository.GetByIdAsync(query.Id, cancellationToken);

    if (todo is null)
        return Result.Failure<TodoResponse>(TodoItemErrors.NotFound);

    return Result.Success(_mapper.Map<TodoResponse>(todo));
}

// ❌ Forbidden: blocking calls
var todo = _todoRepository.GetByIdAsync(query.Id, cancellationToken).Result; // ❌ NEVER!
```

### Validation Decorator Pattern

FluentValidation rules are applied automatically via pipeline decorators:

```csharp
public sealed class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("User ID is required");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Invalid priority level");
    }
}
```

Validators are automatically invoked by the `ValidationDecorator` before handler execution—no manual validation needed in handlers.

---

## 📊 Project Structure

```
clean-architecture-startup-template/
├── src/
│   ├── Domain/                          # Core business entities and rules
│   │
│   ├── Application/                     # Use cases, handlers, validators, DTOs
│   │
│   ├── Infrastructure/                  # Database, repositories, external services
│   │
│   ├── Web.Api/                         # HTTP endpoints, middleware, configuration
│   │  
│   └── SharedKernel/                    # Shared types and abstractions
│       
├── tests/
│   ├── ArchitectureTests/               # Architectural rule enforcement
│   │   └── *.cs
│   │
│   └── Application.UnitTests/           # Unit tests for handlers, validators, decorators
│       ├── Behaviors/
│       │   └── QueryCachingDecoratorTests.cs
│       ├── Todos/
│       │   └── Create/
│       │       ├── CreateTodoCommandHandlerTests.cs
│       │       └── CreateTodoCommandValidatorTests.cs
│       └── Users/
│           ├── Login/
│           │   └── LoginUserCommandHandlerTests.cs
│           └── Register/
│               └── RegisterUserCommandValidatorTests.cs
│
├── .github/
│   └── workflows/                       # CI/CD pipelines
│       ├── build.yml
│       └── tests.yml
│
├── docker-compose.yml
├── Dockerfile
├── .gitignore
├── LICENSE
├── README.md
└── CLAUDE.md                            # Detailed architectural guidelines
```

---

## 🛠️ Tech Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Framework** | .NET 10 LTS | Modern, high-performance runtime |
| **Language** | C# 14 | Type-safe, feature-rich language with primary constructors |
| **Database** | PostgreSQL | Reliable, open-source relational database |
| **ORM** | Entity Framework Core | Type-safe database access and migrations |
| **API** | ASP.NET Core Minimal APIs | Lightweight, high-performance HTTP layer |
| **Validation** | FluentValidation | Composable, fluent validation rules |
| **Logging** | Serilog | Structured, sink-agnostic logging |
| **Auth** | JWT Bearer | Stateless, token-based authentication |
| **Health Checks** | AspNetCore.HealthChecks | Service health monitoring |
| **API Docs** | Swagger/OpenAPI | Auto-generated endpoint documentation |
| **DI Container** | Microsoft.Extensions.DependencyInjection | Built-in, lightweight DI |
| **Scrutor** | Convention-based service registration | Auto-registration of handlers and validators |
| **Caching** | StackExchange.Redis | Distributed caching with automatic query caching |
| **Rate Limiting** | ASP.NET Core Rate Limiting | Built-in rate limiting with multiple policies |
| **API Versioning** | Asp.Versioning.Http | URL-based API versioning support |

---

## 📝 Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Database=clean_architecture;Username=postgres;Password=password",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars-required",
    "Issuer": "YourIssuer",
    "Audience": "YourAudience",
    "ExpirationMinutes": 60
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "Console",
        "Args": { "theme": "Ansi" }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341"
        }
      }
    ]
  }
}
```

---

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/ArchitectureTests

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover

# Run in watch mode (auto-rerun on changes)
dotnet watch test
```

### Test Structure

Unit tests follow the **AAA (Arrange-Act-Assert)** pattern with naming convention: `Handle_Should<Expected>_When<Condition>`:

```csharp
[Fact]
public async Task Handle_ShouldReturnFailure_WhenDescriptionIsEmpty()
{
    // Arrange
    var command = new CreateTodoCommand { Description = "", UserId = Guid.NewGuid() };

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be(TodoItemErrors.DescriptionRequired.Code);
}
```

---

## 📖 Resources

- **[CLAUDE.md](CLAUDE.md)** — Comprehensive architectural guidelines and code standards
- **[Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)** — Uncle Bob's foundational article
- **[.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)** — Official .NET resources
- **[C# 14 Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)** — Latest language features
- **[Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)** — ORM documentation
- **[xUnit Testing](https://xunit.net/)** — Unit testing framework
- **[FluentValidation](https://fluentvalidation.net/)** — Validation library
- **[Serilog](https://serilog.net/)** — Structured logging

---

## 📜 License

This project is licensed under the **MIT License**—see [LICENSE](LICENSE) for details.
