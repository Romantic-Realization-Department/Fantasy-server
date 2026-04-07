# Player Domain Refactoring Spec

## Summary

Normalize the Player table and split the single `PATCH /v1/player/change` endpoint into domain-specific controllers and services.

---

## 1. DB Schema Changes

### 1-1. Player table (modified)

Remove `MaxStage`, `LastWeaponId`, `ActiveSkills` — keep `Level` and `Exp` (move Exp here from PlayerResource).

```
player.player
├── id             BIGSERIAL PK
├── account_id     BIGINT NOT NULL
├── job_type       VARCHAR NOT NULL
├── level          BIGINT NOT NULL DEFAULT 1
├── exp            BIGINT NOT NULL DEFAULT 0       ← moved from PlayerResource
├── created_at     TIMESTAMPTZ NOT NULL
└── updated_at     TIMESTAMPTZ NOT NULL

UNIQUE INDEX: (account_id, job_type)
```

### 1-2. PlayerResource table (modified)

Remove `Gold`, `Exp` — Gold moves to PlayerResource for now? 

Wait: Gold and Exp are updated together in EndPlayerSession. User said Exp moves to Player. Gold stays in PlayerResource.

```
player.player_resource
├── id                  BIGSERIAL PK
├── player_id           BIGINT NOT NULL UNIQUE (FK → player.player)
├── gold                BIGINT NOT NULL DEFAULT 0
├── enhancement_scroll  BIGINT NOT NULL DEFAULT 0
├── mithril             BIGINT NOT NULL DEFAULT 0
├── sp                  BIGINT NOT NULL DEFAULT 0
└── updated_at          TIMESTAMPTZ NOT NULL

Note: Exp removed (moved to player table)
```

### 1-3. PlayerStage table (new)

```
player.player_stage
├── id          BIGSERIAL PK
├── player_id   BIGINT NOT NULL UNIQUE (FK → player.player, CASCADE DELETE)
└── max_stage   BIGINT NOT NULL DEFAULT 1
```

### 1-4. PlayerSession table (new)

```
player.player_session
├── id              BIGSERIAL PK
├── player_id       BIGINT NOT NULL UNIQUE (FK → player.player, CASCADE DELETE)
├── last_weapon_id  INT NULL
├── active_skills   INT[] NOT NULL DEFAULT '{}'
└── updated_at      TIMESTAMPTZ NOT NULL
```

---

## 2. Entity Changes

### 2-1. Player.cs (modified)

**Add:** `Exp` property  
**Remove:** `MaxStage`, `LastWeaponId`, `ActiveSkills`  
**Modify:** `Create()`, `UpdateSessionEnd()` removed, add `UpdateExp()`

```csharp
public class Player
{
    public long Id { get; private set; }
    public long AccountId { get; private set; }
    public JobType JobType { get; private set; }
    public long Level { get; private set; }
    public long Exp { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Player Create(long accountId, JobType jobType) => new()
    {
        AccountId = accountId,
        JobType = jobType,
        Level = 1,
        Exp = 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public void UpdateLevel(long level)
    {
        Level = level;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateExpAndGold(long exp)  // called from EndPlayerSession
    {
        Exp = exp;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 2-2. PlayerResource.cs (modified)

**Remove:** `Gold`, `Exp` properties and related methods  
**Keep:** `EnhancementScroll`, `Mithril`, `Sp`  
**Add:** `Gold` stays (only Exp removed)

```csharp
public class PlayerResource
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public long Gold { get; private set; }
    public long EnhancementScroll { get; private set; }
    public long Mithril { get; private set; }
    public long Sp { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static PlayerResource Create(long playerId) => new()
    {
        PlayerId = playerId,
        Gold = 0,
        EnhancementScroll = 0,
        Mithril = 0,
        Sp = 0,
        UpdatedAt = DateTime.UtcNow
    };

    public void UpdateGold(long gold)
    {
        Gold = gold;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateChangeData(long? enhancementScroll, long? mithril, long? sp)
    {
        if (enhancementScroll.HasValue) EnhancementScroll = enhancementScroll.Value;
        if (mithril.HasValue) Mithril = mithril.Value;
        if (sp.HasValue) Sp = sp.Value;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### 2-3. PlayerStage.cs (new)

```csharp
// Domain/Player/Entity/PlayerStage.cs
public class PlayerStage
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public long MaxStage { get; private set; }

    public static PlayerStage Create(long playerId) => new()
    {
        PlayerId = playerId,
        MaxStage = 1
    };

    public void Update(long maxStage)
    {
        MaxStage = maxStage;
    }
}
```

### 2-4. PlayerSession.cs (new)

```csharp
// Domain/Player/Entity/PlayerSession.cs
public class PlayerSession
{
    public long Id { get; private set; }
    public long PlayerId { get; private set; }
    public int? LastWeaponId { get; private set; }
    public int[] ActiveSkills { get; private set; } = [];
    public DateTime UpdatedAt { get; private set; }

    public static PlayerSession Create(long playerId) => new()
    {
        PlayerId = playerId,
        LastWeaponId = null,
        ActiveSkills = [],
        UpdatedAt = DateTime.UtcNow
    };

    public void Update(int lastWeaponId, int[] activeSkills)
    {
        LastWeaponId = lastWeaponId;
        ActiveSkills = activeSkills;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

---

## 3. EF Core Config Changes

### 3-1. PlayerConfig.cs (modified)

Remove: `MaxStage`, `LastWeaponId`, `ActiveSkills` properties  
Add: `Exp` property

### 3-2. PlayerResourceConfig.cs (modified)

Remove: `Exp` property

### 3-3. PlayerStageConfig.cs (new)

```csharp
// Domain/Player/Entity/Config/PlayerStageConfig.cs
public class PlayerStageConfig : IEntityTypeConfiguration<PlayerStage>
{
    public void Configure(EntityTypeBuilder<PlayerStage> builder)
    {
        builder.ToTable("player_stage", "player");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.PlayerId).IsRequired();
        builder.Property(s => s.MaxStage).IsRequired().HasDefaultValue(1L);
        builder.HasIndex(s => s.PlayerId).IsUnique();
        builder.HasOne<Player>()
            .WithOne()
            .HasForeignKey<PlayerStage>(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### 3-4. PlayerSessionConfig.cs (new)

```csharp
// Domain/Player/Entity/Config/PlayerSessionConfig.cs
public class PlayerSessionConfig : IEntityTypeConfiguration<PlayerSession>
{
    public void Configure(EntityTypeBuilder<PlayerSession> builder)
    {
        builder.ToTable("player_session", "player");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.PlayerId).IsRequired();
        builder.Property(s => s.LastWeaponId);
        builder.Property(s => s.ActiveSkills)
            .IsRequired()
            .HasDefaultValueSql("ARRAY[]::integer[]");
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.HasIndex(s => s.PlayerId).IsUnique();
        builder.HasOne<Player>()
            .WithOne()
            .HasForeignKey<PlayerSession>(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## 4. AppDbContext Changes

Add `DbSet` for new entities:

```csharp
public DbSet<PlayerStage> PlayerStages { get; set; }
public DbSet<PlayerSession> PlayerSessions { get; set; }
```

---

## 5. Repository Changes

### New Interfaces & Implementations

| Interface | Implementation | Responsibility |
|---|---|---|
| `IPlayerStageRepository` | `PlayerStageRepository` | PlayerStage CRUD |
| `IPlayerSessionRepository` | `PlayerSessionRepository` | PlayerSession CRUD |

### IPlayerStageRepository

```csharp
public interface IPlayerStageRepository
{
    Task<PlayerStage?> FindByPlayerIdAsync(long playerId);
    Task<PlayerStage> SaveAsync(PlayerStage stage);
    Task UpdateAsync(PlayerStage stage);
}
```

### IPlayerSessionRepository

```csharp
public interface IPlayerSessionRepository
{
    Task<PlayerSession?> FindByPlayerIdAsync(long playerId);
    Task<PlayerSession> SaveAsync(PlayerSession session);
    Task UpdateAsync(PlayerSession session);
}
```

### IPlayerRepository (modified)

No interface change needed — `FindByAccountAndJobAsync`, `SaveAsync`, `UpdateAsync` remain.

### IPlayerResourceRepository (unchanged)

---

## 6. DTO Changes

### Removed

- `UpdatePlayerChangeRequest` — deleted entirely (split into separate requests)

### New Request DTOs

```csharp
// Dto/Request/UpdatePlayerLevelRequest.cs
public record UpdatePlayerLevelRequest(
    [Required] JobType JobType,
    [Range(1, long.MaxValue)] long Level
);

// Dto/Request/UpdatePlayerStageRequest.cs
public record UpdatePlayerStageRequest(
    [Required] JobType JobType,
    [Range(1, long.MaxValue)] long MaxStage
);

// Dto/Request/UpdatePlayerResourceRequest.cs
public record UpdatePlayerResourceRequest(
    [Required] JobType JobType,
    [Range(0, long.MaxValue)] long? EnhancementScroll,
    [Range(0, long.MaxValue)] long? Mithril,
    [Range(0, long.MaxValue)] long? Sp
);

// Dto/Request/UpdatePlayerWeaponRequest.cs
public record UpdatePlayerWeaponRequest(
    [Required] JobType JobType,
    [Required] List<WeaponChangeItem> Weapons
);

// Dto/Request/UpdatePlayerSkillRequest.cs
public record UpdatePlayerSkillRequest(
    [Required] JobType JobType,
    [Required] List<SkillChangeItem> Skills
);
```

### Modified Request DTOs

```csharp
// EndPlayerSessionRequest.cs (modified — remove Gold field, keep Exp)
public record EndPlayerSessionRequest(
    [Required] JobType JobType,
    [Required] int LastWeaponId,
    [Required] int[] ActiveSkills,
    [Range(0, long.MaxValue)] long? Gold,
    [Range(0, long.MaxValue)] long? Exp
);
```

### Modified Response DTOs

```csharp
// PlayerDataResponse.cs (modified — Exp field stays, sourced from Player now)
// Structure unchanged from client perspective — only data source changes internally
```

---

## 7. Service Changes

### Removed

- `IUpdatePlayerChangeService` / `UpdatePlayerChangeService` — deleted

### New Services

| Interface | Implementation | Endpoint |
|---|---|---|
| `IUpdatePlayerLevelService` | `UpdatePlayerLevelService` | PATCH /v1/player/level |
| `IUpdatePlayerStageService` | `UpdatePlayerStageService` | PATCH /v1/player/stage |
| `IUpdatePlayerResourceService` | `UpdatePlayerResourceService` | PATCH /v1/player/resource |
| `IUpdatePlayerWeaponService` | `UpdatePlayerWeaponService` | PATCH /v1/player/weapon |
| `IUpdatePlayerSkillService` | `UpdatePlayerSkillService` | PATCH /v1/player/skill |

### Modified Services

**InitPlayerService** — add creation of `PlayerStage` and `PlayerSession` records on new player, read from both for response building.

**EndPlayerSessionService** — write `LastWeaponId`/`ActiveSkills` to `PlayerSession` (not `Player`), write `Exp` to `Player`, write `Gold` to `PlayerResource`.

---

## 8. Controller Changes

### PlayerController (modified)

```
POST  /v1/player/init   → IInitPlayerService
PATCH /v1/player/level  → IUpdatePlayerLevelService
```

### PlayerResourceController (new)

```
PATCH /v1/player/resource  → IUpdatePlayerResourceService
```

### PlayerStageController (new)

```
PATCH /v1/player/stage  → IUpdatePlayerStageService
```

### PlayerWeaponController (new)

```
PATCH /v1/player/weapon  → IUpdatePlayerWeaponService
```

### PlayerSkillController (new)

```
PATCH /v1/player/skill  → IUpdatePlayerSkillService
```

### PlayerSessionController (new)

```
PATCH /v1/player/session/end  → IEndPlayerSessionService
```

All controllers: `[ApiController]`, `[Route("v1/player/...")]`, `[Authorize]`

---

## 9. Redis Cache Strategy

**Key format unchanged:** `player:{accountId}:{jobType}`

**Rules:**
- All 5 PATCH endpoints → `DEL player:{accountId}:{jobType}` after DB write
- `POST /v1/player/init` → GET on cache miss → query DB → SET cache → return
- `PlayerDataResponse` structure unchanged from client perspective (Exp now sourced from Player table, but response field stays the same)

---

## 10. DI Registration (PlayerServiceConfig.cs)

Add new service and repository registrations:

```csharp
// Repositories
services.AddScoped<IPlayerStageRepository, PlayerStageRepository>();
services.AddScoped<IPlayerSessionRepository, PlayerSessionRepository>();

// Services
services.AddScoped<IUpdatePlayerLevelService, UpdatePlayerLevelService>();
services.AddScoped<IUpdatePlayerStageService, UpdatePlayerStageService>();
services.AddScoped<IUpdatePlayerResourceService, UpdatePlayerResourceService>();
services.AddScoped<IUpdatePlayerWeaponService, UpdatePlayerWeaponService>();
services.AddScoped<IUpdatePlayerSkillService, UpdatePlayerSkillService>();

// Remove
// services.AddScoped<IUpdatePlayerChangeService, UpdatePlayerChangeService>();
```

---

## 11. Migration Strategy

1. Add EF Core migration: `NormalizePlayerTables`
   - Adds `player_stage`, `player_session` tables
   - Adds `exp` column to `player`
   - Removes `max_stage`, `last_weapon_id`, `active_skills` from `player`
   - Removes `exp` from `player_resource`
   - Data migration SQL in `Up()`:
     ```sql
     -- Copy existing data before dropping columns
     INSERT INTO player.player_stage (player_id, max_stage)
     SELECT id, max_stage FROM player.player;

     INSERT INTO player.player_session (player_id, last_weapon_id, active_skills, updated_at)
     SELECT id, last_weapon_id, active_skills, updated_at FROM player.player;

     UPDATE player.player p
     SET exp = pr.exp
     FROM player.player_resource pr
     WHERE pr.player_id = p.id;
     ```

2. Run `/db-migrate update` to apply

---

## 12. Implementation Order

```
Phase 1 — Entities & EF Config
  1. Modify Player.cs (add Exp, remove MaxStage/LastWeaponId/ActiveSkills)
  2. Modify PlayerResource.cs (remove Exp)
  3. Create PlayerStage.cs
  4. Create PlayerSession.cs
  5. Modify PlayerConfig.cs
  6. Modify PlayerResourceConfig.cs
  7. Create PlayerStageConfig.cs
  8. Create PlayerSessionConfig.cs
  9. Update AppDbContext (add DbSets)

Phase 2 — Migration
  10. /db-migrate add NormalizePlayerTables (with data migration SQL)
  11. /db-migrate update

Phase 3 — Repositories
  12. Create IPlayerStageRepository + PlayerStageRepository
  13. Create IPlayerSessionRepository + PlayerSessionRepository

Phase 4 — DTOs
  14. Delete UpdatePlayerChangeRequest.cs
  15. Create UpdatePlayerLevelRequest, UpdatePlayerStageRequest,
      UpdatePlayerResourceRequest, UpdatePlayerWeaponRequest, UpdatePlayerSkillRequest
  16. Modify EndPlayerSessionRequest (remove nothing — Gold/Exp stay)
  17. Modify PlayerDataResponse (Exp source changes internally only)

Phase 5 — Services
  18. Modify InitPlayerService (create PlayerStage + PlayerSession, read from them)
  19. Modify EndPlayerSessionService (write to PlayerSession, Player.Exp, PlayerResource.Gold)
  20. Delete UpdatePlayerChangeService
  21. Create UpdatePlayerLevelService
  22. Create UpdatePlayerStageService
  23. Create UpdatePlayerResourceService
  24. Create UpdatePlayerWeaponService
  25. Create UpdatePlayerSkillService

Phase 6 — Controllers
  26. Modify PlayerController (add PATCH / for level)
  27. Create PlayerResourceController
  28. Create PlayerStageController
  29. Create PlayerWeaponController
  30. Create PlayerSkillController
  31. Create PlayerSessionController

Phase 7 — DI & Verify
  32. Update PlayerServiceConfig.cs
  33. /test (build + all tests)
```

---

## 13. Test Coverage

For each new service, test:

| Service | Cases |
|---|---|
| `UpdatePlayerLevelService` | happy path, player not found |
| `UpdatePlayerStageService` | happy path, stage not found |
| `UpdatePlayerResourceService` | happy path, partial update (nulls), player not found |
| `UpdatePlayerWeaponService` | happy path, player not found |
| `UpdatePlayerSkillService` | happy path, player not found |
| `InitPlayerService` | new player (creates stage+session), existing player (cache hit, cache miss) |
| `EndPlayerSessionService` | happy path, player not found |

Existing tests for `UpdatePlayerChangeService` → delete.
