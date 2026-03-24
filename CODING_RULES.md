# Coding Rules

Rules for implementing code in this repository. Follow these conventions when writing or modifying code.

## Project Structure

- **Models project**: `[SolutionName].Models` - contains EF Core DbContext and entities
- **Services project**: `*.Services` - all business logic goes here
- **Controllers**: Only in the main API project, inherit from base controllers

## Services

### Structure
- Each service in its own folder: `Services/[Name]/[Name]Service.cs`
- Interface in same folder: `Services/[Name]/I[Name]Service.cs`
- Related DTOs, enums, and helpers in the same folder (not in separate folders)

### Implementation
- Services implement all business logic
- Controllers only validate input and call services
- Services must implement an interface for DI
- Services must NOT use ASP.NET-specific classes (HttpContext, Session, etc.)

### Dependency Injection
```csharp
public class MyService : IMyService
{
    private readonly ILogService log;
    private readonly TCDBContext db;

    public MyService(ILogService log, TCDBContext db)
    {
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        this.db = db ?? throw new ArgumentNullException(nameof(db));
    }
}
```
- Fields: `private readonly`
- Constructor: null check with `?? throw new ArgumentNullException(nameof(param))`

## Controllers

### Hierarchy
```
AbstractController                    → Base with logging, correlation token
├── CustomerAbstractController        → Route: api/v{version}/customers/{customerId}/[controller]
└── GenericAbstractController         → Route: api/v{version}/[controller]
```

### Rules
- Customer-specific endpoints: inherit from `CustomerAbstractController`
- Non-customer endpoints: inherit from `GenericAbstractController`
- Controllers contain NO business logic - only call services
- Include `[FromQuery] string correlationToken` as last parameter (except internal/trigger endpoints)

### Endpoint Naming
Route name = Endpoint name = Method name:
```csharp
[HttpPost("prepare", Name = nameof(Prepare))]
public async Task<PreparedOutDTO> Prepare(...)
```

## DTOs

### Naming
- Input: `*InDTO`
- Output: `*OutDTO`
- Both: `*InOutDTO`
- Never use "Model" in DTO names

### Implementation
```csharp
public class MyOutDTO
{
    public MyOutDTO(string name, int count)
    {
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
        this.Count = count;
    }

    public string Name { get; }   // Getter only, no setter
    public int Count { get; }     // Getter only, no setter
}
```
- Must have constructor
- Validate constructor arguments
- Properties: getter only, no setters
- Never use EF entities as DTOs

## REST API

### HTTP Methods
- **GET**: Loading data (simple queries)
- **POST**: Actions, changes, or complex queries with body payload
- No PUT, PATCH, DELETE

### POST Requests
- No query parameters (except `correlationToken` and `customerId` in route)
- All data in request body

### Security
- CustomerID comes from route, never from DTO body
- Never return CustomerID in responses

## Entity Framework

### General
- Database-first approach
- Do NOT use navigation properties (use separate queries)
- Navigation properties are `internal` (not removed, but hidden)

### Enum Naming
For enum columns: `[TableName][ColumnName]`
```csharp
// For db.AuditRun.Status column:
public enum AuditRunStatus { ... }
```

### Connection String
Include app name and environment:
```
Application Name=TC.Audit.Microservice-{env};
```

## C# Conventions

### Async
- Async methods must have `Async` suffix
- Use async in WebAPI projects
- Do NOT use async in console apps

### Parameters
Order from higher to lower level:
```csharp
public void Method(long customerId, long accountId, long auditId, string ruleKey)
```

### Collections
- Method parameters and returns: use arrays `[]`, not `List<>`
- Internal variables: `List<>` is acceptable
```csharp
// Good
public string[] GetNames(Account[] accounts)

// Bad
public List<string> GetNames(List<Account> accounts)
```

### Nullables
- Use `.HasValue` and `.Value` for nullable types
- Mark nullable references with `?`
- Document what `null` means for nullable parameters

### Percentages
Represent as 0..1, not 0..100

### Immutability
- Never mutate input arguments
- Return changes through return value
```csharp
// Good
public long GetCost(Account account) => db.Cost.Where(...).Single();

// Bad
public void GetCost(Account account) { account.Cost = ...; }
```

### Enum Parsing
Use `ToEnum<T>()` from TC.Shared, not `Enum.Parse<T>()`

## Exception Handling

### Exception Types
- `ContractApplicationException`: Expected errors, part of API contract → 400, logged as Info
- `ApplicationException`: Code-handled errors → 400, logged as Error
- Other exceptions: Bugs/infrastructure issues → 500, logged as Error

## Logging

### Format
Use `key=value` format for Logz.io parsing:
```csharp
log.Info($"e=actionName customerId={customerId} result={result}");
```

### Log4net
- Pattern: `%date l=%level app=%property{app} env=%property{env} %message%newline`
- Daily rolling files

## Adding New Audit Rule Changes

Implement `IChange` interface:
```csharp
public class MyRuleChange : IChange
{
    public AuditRuleKey RuleKey => AuditRuleKey.MyRule;

    public async Task<OutputTableFormattedAuditingOutDTO> PrepareAsync(
        long customerId, long[] loginCustomerIds, long clientCustomerId,
        string refreshToken, SelectedForChangesExtendedInDTO selectedForChanges)
    { ... }

    public async Task<SubmitChangesResultOutDTO> SubmitAsync(
        long customerId, long[] loginCustomerIds, long clientCustomerId,
        string refreshToken, SubmitChangesExtendedInDTO submitChanges)
    { ... }
}
```

Place in `Services/MakeChanges/[RuleName]/[RuleName]Change.cs`

Register in `Startup.cs`:
```csharp
services.AddTransient<IChange, MyRuleChange>();
```

## JSON Serialization

- Use System.Text.Json (STJ), NOT Newtonsoft.Json
- Use `JsonOptions.Default` from TC.Shared
- Constructor-based deserialization for validation
