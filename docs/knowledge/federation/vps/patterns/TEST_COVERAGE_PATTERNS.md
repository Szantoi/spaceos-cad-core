# Test Coverage Patterns — .NET Backend Implementation

**Created:** 2026-06-22 (based on EHS Module Sprint 2 + Explorer research)

---

## Pattern Overview

**SpaceOS Test Coverage Strategy** = Layered testing with specific coverage targets per architectural layer.

### Coverage Targets

| Layer | Target | Rationale |
|---|---|---|
| **Domain + Application** | ≥90% | Business logic must be thoroughly tested |
| **Infrastructure + API** | ≥40% | Integration tests cover critical paths |
| **Overall** | ≥70% | Balanced coverage without over-testing |

---

## Test Structure Pattern

### 3-Tier Test Architecture

```
Domain Layer Unit Tests (95%+ coverage)
  ├── Aggregate tests (create, validation, business rules)
  ├── Value Object tests (From, FromNullable, validation)
  └── Enum/Type tests (ToApiString, FromApiString, case insensitive)

Application Layer Unit Tests (90%+ coverage)
  ├── Command/Query validator tests (FluentValidation)
  └── Handler tests (Moq dependencies)

Integration Tests (40%+ coverage, Testcontainers)
  ├── Controller tests (HTTP endpoints, status codes)
  ├── Authentication tests (401 paths)
  └── Validation tests (400 paths)
```

---

## Domain Layer Unit Tests

### Aggregate Tests Pattern

```csharp
// Example: EhsEventTests.cs
public class EhsEventTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var eventId = EventId.New();
        var payload = new IncidentPayload { ... };
        var tenantId = Guid.NewGuid();

        // Act
        var @event = EhsEvent.ReportIncident(eventId, payload, null, tenantId);

        // Assert
        @event.Should().NotBeNull();
        @event.Id.Should().Be(eventId);
        @event.Type.Should().Be("incident_reported");
    }

    [Fact]
    public void Create_WithEmptyType_ShouldThrow()
    {
        // Arrange
        var eventId = EventId.New();
        var payload = new IncidentPayload { ... };

        // Act & Assert
        var act = () => new EhsEvent { Type = "" };
        act.Should().Throw<ArgumentException>();
    }
}
```

**Key Points:**
- Test factory methods (static creators like `ReportIncident`)
- Test validation rules (empty/null checks)
- Test business rule enforcement
- Use FluentAssertions for readability

### Value Object Tests Pattern

```csharp
// Example: PhotoS3KeyTests.cs
public class PhotoS3KeyTests
{
    [Fact]
    public void From_WithValidKey_ShouldSucceed()
    {
        // Arrange
        var key = "photos/2026-06/incident-abc123.jpg";

        // Act
        var s3Key = PhotoS3Key.From(key);

        // Assert
        s3Key.Value.Should().Be(key);
    }

    [Fact]
    public void From_WithEmptyKey_ShouldThrow()
    {
        // Act & Assert
        var act = () => PhotoS3Key.From("");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*S3 key cannot be empty*");
    }

    [Fact]
    public void From_WithKeyExceeding500Chars_ShouldThrow()
    {
        // Arrange
        var longKey = new string('a', 501);

        // Act & Assert
        var act = () => PhotoS3Key.From(longKey);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*S3 key cannot exceed 500 characters*");
    }

    [Fact]
    public void FromNullable_WithNullValue_ShouldReturnNull()
    {
        // Act
        var s3Key = PhotoS3Key.FromNullable(null);

        // Assert
        s3Key.Should().BeNull();
    }
}
```

**Key Points:**
- Test both `From` (throws on invalid) and `FromNullable` (returns null)
- Test boundary conditions (empty, max length, null)
- Use descriptive test names (follows SUT_Scenario_ExpectedBehavior convention)

### Enum/Type Tests Pattern

```csharp
// Example: IncidentTypeTests.cs
public class IncidentTypeTests
{
    [Theory]
    [InlineData(IncidentType.NearMiss, "near-miss")]
    [InlineData(IncidentType.Injury, "injury")]
    [InlineData(IncidentType.PropertyDamage, "property")]
    public void ToApiString_ShouldReturnCorrectValue(IncidentType type, string expected)
    {
        // Act
        var apiString = type.ToApiString();

        // Assert
        apiString.Should().Be(expected);
    }

    [Theory]
    [InlineData("near-miss", IncidentType.NearMiss)]
    [InlineData("NEAR-MISS", IncidentType.NearMiss)]  // Case insensitive
    [InlineData("injury", IncidentType.Injury)]
    [InlineData("Injury", IncidentType.Injury)]
    [InlineData("property", IncidentType.PropertyDamage)]
    public void FromApiString_WithValidValue_ShouldSucceed(string apiString, IncidentType expected)
    {
        // Act
        var type = IncidentType.FromApiString(apiString);

        // Assert
        type.Should().Be(expected);
    }

    [Fact]
    public void FromApiString_WithInvalidValue_ShouldThrow()
    {
        // Act & Assert
        var act = () => IncidentType.FromApiString("invalid-type");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unknown incident type*");
    }
}
```

**Key Points:**
- Use `[Theory]` + `[InlineData]` for parameterized tests
- Test case insensitivity (API convention)
- Test all enum variants
- Test invalid inputs

---

## Application Layer Unit Tests

### FluentValidation Validator Tests

```csharp
// Example: ReportIncidentCommandValidatorTests.cs
public class ReportIncidentCommandValidatorTests
{
    private readonly ReportIncidentCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new ReportIncidentCommand
        {
            EventId = Guid.NewGuid(),
            Type = "incident_reported",
            Payload = new IncidentPayload
            {
                IncidentType = "injury",
                ReporterId = Guid.NewGuid().ToString(),
                Description = "Test description",
                Timestamp = DateTime.UtcNow
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyEventId_ShouldFail()
    {
        // Arrange
        var command = new ReportIncidentCommand { EventId = Guid.Empty };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EventId");
    }

    [Fact]
    public void Validate_WithDescriptionExceeding2000Chars_ShouldFail()
    {
        // Arrange
        var command = new ReportIncidentCommand
        {
            Payload = new IncidentPayload
            {
                Description = new string('a', 2001)
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Payload.Description" &&
            e.ErrorMessage.Contains("2000")
        );
    }
}
```

**Key Points:**
- Test happy path (valid command)
- Test each validation rule individually
- Check `ErrorMessage` content for clarity
- Test boundary conditions (empty, max length)

### Handler Tests with Moq

```csharp
// Example: ReportIncidentCommandHandlerTests.cs (simplified)
public class ReportIncidentCommandHandlerTests
{
    private readonly Mock<IEhsEventRepository> _repositoryMock = new();
    private readonly Mock<ICurrentUserContext> _userContextMock = new();
    private readonly ReportIncidentCommandHandler _handler;

    public ReportIncidentCommandHandlerTests()
    {
        _handler = new ReportIncidentCommandHandler(_repositoryMock.Object, _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAddEventToRepository()
    {
        // Arrange
        var command = new ReportIncidentCommand { ... };
        _userContextMock.Setup(x => x.TenantId).Returns(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(x => x.AddAsync(It.Is<EhsEvent>(e =>
            e.Type == "incident_reported" &&
            e.Id.Value == command.EventId
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateEventId_ShouldReturnExistingEvent()
    {
        // Arrange
        var existingEvent = new EhsEvent { ... };
        _repositoryMock.Setup(x => x.GetByEventIdAsync(It.IsAny<EventId>()))
            .ReturnsAsync(existingEvent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(existingEvent.Id);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<EhsEvent>()), Times.Never);
    }
}
```

**Key Points:**
- Mock dependencies (repositories, services)
- Verify method calls with `Times.Once` / `Times.Never`
- Use `It.Is<T>()` for argument matching
- Test idempotency logic (duplicate handling)

---

## Integration Tests (Testcontainers)

### Testcontainers Setup Pattern

```csharp
// EhsApiTestBase.cs (shared base class for all integration tests)
public class EhsApiTestBase : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    protected readonly HttpClient Client;

    public EhsApiTestBase(WebApplicationFactory<Program> factory)
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("ehs_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove production DbContext
                    var descriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<EhsDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // Add test DbContext
                    services.AddDbContext<EhsDbContext>(options =>
                        options.UseNpgsql(_postgresContainer.GetConnectionString()));

                    // Mock external dependencies
                    services.AddScoped<IS3Service, MockS3Service>();
                    services.AddScoped<ICurrentUserContext, MockUserContext>();

                    // Use test authentication handler
                    services.AddAuthentication(TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            TestAuthHandler.SchemeName, _ => { });
                });
            });

        Client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // Run migrations
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EhsDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }
}
```

**Key Points:**
- `IClassFixture<WebApplicationFactory<Program>>` — Shared test fixture per test class
- `IAsyncLifetime` — Async setup/teardown (container start/stop)
- PostgreSQL 16 Alpine container (lightweight)
- Auto-migration on startup (`MigrateAsync`)
- Mock external dependencies (S3, UserContext)
- Test authentication handler (bypasses JWT)

### Program.cs Requirement

```csharp
// Ehs.Api/Program.cs
// REQUIRED: Make Program class accessible to WebApplicationFactory
public partial class Program { }
```

**Why?** WebApplicationFactory needs to reference the `Program` class to bootstrap the app.

### Controller Integration Tests

```csharp
// EventsControllerTests.cs
public class EventsControllerTests : EhsApiTestBase
{
    public EventsControllerTests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task PostEvent_WithValidPayload_ShouldReturn201Created()
    {
        // Arrange
        var request = new
        {
            eventId = Guid.NewGuid(),
            type = "incident_reported",
            payload = new
            {
                incidentType = "injury",
                reporterId = Guid.NewGuid().ToString(),
                description = "Test incident",
                timestamp = DateTime.UtcNow
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/ehs/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<EventResponse>();
        content.EventId.Should().Be(request.eventId);
    }

    [Fact]
    public async Task PostEvent_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Arrange
        var client = factory.CreateClient();  // No auth header

        // Act
        var response = await client.PostAsJsonAsync("/api/ehs/events", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostEvent_WithEmptyDescription_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new { payload = new { description = "" } };

        // Act
        var response = await Client.PostAsJsonAsync("/api/ehs/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

**Key Points:**
- Test happy path (201 Created)
- Test authentication (401 Unauthorized)
- Test validation (400 Bad Request)
- Use `HttpClient.PostAsJsonAsync` for JSON requests
- Assert status codes + response content

---

## Dependencies Pattern

### NuGet Packages (Per Test Project)

```xml
<ItemGroup>
  <!-- Testing framework -->
  <PackageReference Include="xUnit" Version="2.4.2" />
  <PackageReference Include="xUnit.runner.visualstudio" Version="2.4.5" />

  <!-- Testcontainers -->
  <PackageReference Include="Testcontainers" Version="3.5.0" />
  <PackageReference Include="Testcontainers.PostgreSql" Version="3.5.0" />

  <!-- ASP.NET Core testing -->
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />

  <!-- Mocking -->
  <PackageReference Include="Moq" Version="4.18.4" />

  <!-- Assertions -->
  <PackageReference Include="FluentAssertions" Version="6.12.0" />

  <!-- Project references -->
  <ProjectReference Include="..\Ehs.Domain\Ehs.Domain.csproj" />
  <ProjectReference Include="..\Ehs.Application\Ehs.Application.csproj" />
  <ProjectReference Include="..\Ehs.Infrastructure\Ehs.Infrastructure.csproj" />
  <ProjectReference Include="..\Ehs.Api\Ehs.Api.csproj" />
</ItemGroup>
```

---

## Acceptance Criteria (reviewer.sh)

### Automated Review Thresholds

```bash
# reviewer.sh checks (dual Haiku agents)
1. Build: 0 errors ✅
2. Tests: >90% pass rate ✅
3. Coverage:
   - Domain + Application: ≥90% ✅
   - Overall: ≥70% ✅
4. No critical warnings (nullable, unused code)
```

**APPROVE if:**
- All thresholds met
- Test failures have documented reasons (e.g., "validation middleware not wired in test setup")

**REJECT if:**
- Build fails
- Test pass rate <90%
- Coverage <70%
- Critical warnings unresolved

---

## Common Pitfalls

### 1. FluentValidation middleware not wired in WebApplicationFactory
**Symptom:** Integration tests fail with 200 OK instead of 400 Bad Request
**Fix:** Add FluentValidation behavior to test setup:
```csharp
services.AddControllers()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<ReportIncidentCommandValidator>());
```

### 2. Testcontainers not disposed
**Symptom:** PostgreSQL containers accumulate, port conflicts
**Fix:** Implement `IAsyncLifetime.DisposeAsync()`

### 3. Test authentication not configured
**Symptom:** All requests return 401 Unauthorized
**Fix:** Add `TestAuthHandler` to services

### 4. Migrations not applied
**Symptom:** Integration tests fail with "relation does not exist"
**Fix:** Call `dbContext.Database.MigrateAsync()` in `InitializeAsync()`

---

## Performance Considerations

- **Testcontainers startup:** ~5-10s per test run (cached image)
- **Test isolation:** Each test class gets fresh DB (no shared state)
- **Parallel execution:** xUnit supports parallel test execution (default)
- **Coverage reporting:** Use `dotnet test --collect:"XPlat Code Coverage"`

---

## References

- Implementation: `backend/spaceos-modules-ehs/Ehs.Tests/` (35 tests, 25 unit + 10 integration)
- DONE message: `terminals/backend/outbox/2026-06-22_025_ehs-unit-integration-tests-done.md`
- Explorer research: `terminals/explorer/outbox/2026-06-22_002_deep-dive-patterns-research-done.md`
- Coverage achieved: Domain+App 95%, Overall 75%
