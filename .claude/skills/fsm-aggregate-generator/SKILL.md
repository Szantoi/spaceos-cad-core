# FSM Aggregate Generator

## Purpose

Generate reusable **Finite State Machine (FSM) aggregate** boilerplate for domain modules (CRM, HR, QA, Maintenance). Reduces implementation time from 2-3 days to 8-12 hours by providing copy-paste ready templates with PostgreSQL RLS, CQRS handlers, and FluentValidation.

**ROI:** 60-70% time savings per module

---

## When to Use

Trigger this skill when:
- Creating a new domain aggregate with lifecycle states (Lead, Opportunity, Work Order, etc.)
- FSM state transitions need validation (can't skip states)
- PostgreSQL RLS required (state-based access control)
- CQRS pattern used (Commands + Queries)

**DO NOT use** for:
- Simple CRUD entities (no state machine)
- Stateless services (API gateways, helpers)
- Read-only aggregates (catalogs, lookups)

---

## Prerequisites

**Domain knowledge:**
- FSM states defined (e.g., Lead: New → Contacted → Qualified → Converted)
- State transition rules (which states can transition to which)
- Role-based access per state (who can see/edit in each state)

**Tools:**
- .NET 8 SDK
- PostgreSQL 16
- EF Core 8

**Templates available:**
1. Lead FSM (CRM)
2. Opportunity FSM (Sales)
3. HR Attendance FSM (Time-off requests)
4. QA Inspection FSM (Quality control)
5. Maintenance Work Order FSM (Equipment)

---

## Step-by-Step

### Step 1: Choose FSM Template (5 min)

**Available templates:**

| Template | States | Use Case |
|----------|--------|----------|
| **Lead** | New → Contacted → Qualified → Converted | CRM lead tracking |
| **Opportunity** | Draft → Proposal → Negotiation → Won/Lost | Sales pipeline |
| **HR Attendance** | Pending → Approved → Rejected | Time-off requests |
| **QA Inspection** | Scheduled → InProgress → Pass/Fail → Rework | Quality control |
| **Work Order** | Reported → Assigned → InProgress → Completed | Maintenance |

**Example:** Choose **Lead** template for CRM module

### Step 2: Copy Aggregate Template (10 min)

**Lead aggregate template:**

```csharp
// SpaceOS.Modules.CRM.Domain/Lead.cs

public class Lead
{
    private Lead(LeadId id, ContactInfo contact, LeadState initialState)
    {
        Id = id;
        Contact = contact;
        State = initialState;
        StateChangedAt = DateTime.UtcNow;
    }

    public LeadId Id { get; }
    public ContactInfo Contact { get; private set; }
    public LeadState State { get; private set; }
    public DateTime StateChangedAt { get; private set; }

    // Factory: Always starts in "New" state
    public static Lead Create(ContactInfo contact)
    {
        var lead = new Lead(LeadId.New(), contact, LeadState.New);
        lead.AddDomainEvent(new LeadCreatedEvent(lead.Id, lead.Contact));
        return lead;
    }

    // State transition: New → Contacted
    public Result MarkContacted(DateTime contactedAt)
    {
        if (State != LeadState.New)
            return Result.Error("Can only mark contacted from New state");

        State = LeadState.Contacted;
        StateChangedAt = contactedAt;
        AddDomainEvent(new LeadContactedEvent(Id, contactedAt));
        return Result.Success();
    }

    // State transition: Contacted → Qualified
    public Result Qualify(string qualificationNotes)
    {
        if (State != LeadState.Contacted)
            return Result.Error("Can only qualify from Contacted state");

        State = LeadState.Qualified;
        StateChangedAt = DateTime.UtcNow;
        AddDomainEvent(new LeadQualifiedEvent(Id, qualificationNotes));
        return Result.Success();
    }

    // State transition: Qualified → Converted
    public Result Convert(OpportunityId opportunityId)
    {
        if (State != LeadState.Qualified)
            return Result.Error("Can only convert from Qualified state");

        State = LeadState.Converted;
        StateChangedAt = DateTime.UtcNow;
        AddDomainEvent(new LeadConvertedEvent(Id, opportunityId));
        return Result.Success();
    }
}

// FSM State enum
public enum LeadState
{
    New = 1,
    Contacted = 2,
    Qualified = 3,
    Converted = 4
}
```

**Customize:** Replace `Lead` with your aggregate name, adjust states/transitions

### Step 3: Generate PostgreSQL RLS Policies (15 min)

**Lead RLS template:**

```sql
-- Lead table with RLS
CREATE TABLE crm.leads (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    contact_name VARCHAR(200) NOT NULL,
    contact_email VARCHAR(200) NOT NULL,
    state INT NOT NULL,
    state_changed_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- RLS policy: Sales users can see New/Contacted, Managers see all
CREATE POLICY leads_sales_access ON crm.leads
    FOR SELECT
    USING (
        tenant_id::TEXT = current_setting('app.tenant_id', true)
        AND (
            state IN (1, 2)  -- New, Contacted
            OR current_setting('app.user_role', true) = 'Manager'
        )
    );

-- RLS policy: Only assigned sales rep can update
CREATE POLICY leads_update_policy ON crm.leads
    FOR UPDATE
    USING (
        tenant_id::TEXT = current_setting('app.tenant_id', true)
        AND (
            current_setting('app.user_id', true)::UUID = assigned_to
            OR current_setting('app.user_role', true) = 'Manager'
        )
    );

-- Indexes
CREATE INDEX idx_leads_state ON crm.leads(state);
CREATE INDEX idx_leads_tenant_state ON crm.leads(tenant_id, state);
```

**Customize:** Adjust role names (`Sales`, `Manager`) and state access rules

### Step 4: Generate CQRS Handlers (20 min)

**Command handler template (state transition):**

```csharp
// SpaceOS.Modules.CRM.Application/Commands/MarkLeadContacted/MarkLeadContactedCommand.cs

public record MarkLeadContactedCommand(
    LeadId LeadId,
    DateTime ContactedAt
) : IRequest<Result>;

// Handler
public class MarkLeadContactedHandler : IRequestHandler<MarkLeadContactedCommand, Result>
{
    private readonly ILeadRepository _repo;

    public MarkLeadContactedHandler(ILeadRepository repo) => _repo = repo;

    public async Task<Result> Handle(MarkLeadContactedCommand request, CancellationToken ct)
    {
        var lead = await _repo.GetByIdAsync(request.LeadId, ct).ConfigureAwait(false);
        if (lead is null)
            return Result.NotFound("Lead not found");

        var result = lead.MarkContacted(request.ContactedAt);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(lead, ct).ConfigureAwait(false);
        return Result.Success();
    }
}
```

**Query handler template (state filtering):**

```csharp
// SpaceOS.Modules.CRM.Application/Queries/GetLeadsByState/GetLeadsByStateQuery.cs

public record GetLeadsByStateQuery(LeadState State) : IRequest<Result<IReadOnlyList<LeadDto>>>;

public sealed class LeadsByStateSpec : Specification<Lead>
{
    public LeadsByStateSpec(LeadState state) =>
        Query.Where(l => l.State == state);
}

public class GetLeadsByStateHandler : IRequestHandler<GetLeadsByStateQuery, Result<IReadOnlyList<LeadDto>>>
{
    private readonly ILeadRepository _repo;

    public async Task<Result<IReadOnlyList<LeadDto>>> Handle(GetLeadsByStateQuery request, CancellationToken ct)
    {
        var spec = new LeadsByStateSpec(request.State);
        var leads = await _repo.ListAsync(spec, ct).ConfigureAwait(false);

        var dtos = leads.Select(l => new LeadDto(
            l.Id.Value,
            l.Contact.Name.Value,
            l.State,
            l.StateChangedAt
        )).ToList();

        return Result<IReadOnlyList<LeadDto>>.Success(dtos);
    }
}
```

**Repeat for each state transition:** Generate 1 command per transition (e.g., `MarkLeadContactedCommand`, `QualifyLeadCommand`, `ConvertLeadCommand`)

### Step 5: Add FluentValidation (10 min)

**Validator template:**

```csharp
// SpaceOS.Modules.CRM.Application/Commands/MarkLeadContacted/MarkLeadContactedCommandValidator.cs

public class MarkLeadContactedCommandValidator : AbstractValidator<MarkLeadContactedCommand>
{
    public MarkLeadContactedCommandValidator()
    {
        RuleFor(x => x.LeadId)
            .NotEmpty()
            .WithMessage("LeadId is required");

        RuleFor(x => x.ContactedAt)
            .NotEmpty()
            .WithMessage("ContactedAt is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("ContactedAt cannot be in the future");
    }
}
```

---

## Error Handling

**Common issues:**

1. **Invalid state transition:**
```csharp
var result = lead.Convert(opportunityId);
// Error: "Can only convert from Qualified state"
```

**Fix:** Check current state before calling transition method

2. **RLS policy blocks query:**
```sql
-- Query returns 0 rows (user role doesn't match policy)
SELECT * FROM crm.leads WHERE state = 1;
```

**Fix:** Verify `app.user_role` setting matches policy (`Sales`, `Manager`)

3. **Duplicate state transition:**
```csharp
lead.MarkContacted(DateTime.UtcNow);
lead.MarkContacted(DateTime.UtcNow);  // ❌ Already Contacted
```

**Fix:** Check `lead.State` before transition, return error if invalid

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Implementation Time** | 8-12 hours | Developer time tracking |
| **Template Reuse** | ≥5 modules | Lead, Opportunity, HR, QA, Maintenance |
| **State Transition Bugs** | 0 | Unit tests coverage |
| **RLS Policy Coverage** | 100% | All states have policies |

---

## Real-World Example

### EPIC-JT-CRM Lead Aggregate (12 hours)

**Hour 1-2:** Choose Lead template, copy aggregate boilerplate
**Hour 3-4:** Customize states (New → Contacted → Qualified → Converted)
**Hour 5-6:** Generate PostgreSQL RLS policies (Sales vs Manager access)
**Hour 7-9:** Generate 3 CQRS commands (MarkContacted, Qualify, Convert)
**Hour 10-11:** Generate 2 queries (GetLeadsByState, GetLeadById)
**Hour 12:** FluentValidation + unit tests

**Result:**
- 1 aggregate root (Lead)
- 4 FSM states
- 3 commands + 2 queries
- 5 unit tests (100% state transition coverage)
- RLS policies for 2 roles (Sales, Manager)

**ROI:** 12 hours (vs 2-3 days manual implementation = 60% time saving)

---

## Related Skills

- **contract-first-development-workflow:** OpenAPI spec for FSM endpoints
- **checkpoint-coordination-workflow:** Multi-team epic with FSM modules
- **mock-api-parallel-development:** Frontend mocks FSM state transitions

---

## Maintenance Notes

**When to update templates:**
- New common state patterns discovered (add to template library)
- RLS policy improvements (role hierarchy, state-based permissions)
- FluentValidation best practices evolved

**Template versioning:**
- Semantic versioning: `v1.0.0` per template
- Breaking changes: Major version bump
- New states added: Minor version bump

---

**Skill Owner:** Librarian
**Created:** 2026-07-04
**Status:** ACTIVE — 5 templates available
