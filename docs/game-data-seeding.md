# Game Data Seeding

## Overview

Game reference data is managed through **embedded JSON + `GameDataSeeder`**, not hardcoded entities or SQL scripts.

Target tables (schema `game_data`):

| Table | Entity | Key |
|---|---|---|
| `job_base_stat` | `JobBaseStat` | `JobType` |
| `level_table` | `LevelTable` | `Level` |
| `stage_data` | `StageData` | `Stage` |
| `skill_data` | `SkillData` | `SkillId` |
| `weapon_data` | `WeaponData` | `WeaponId` |

On server startup (`Program.cs`):

1. `db.Database.MigrateAsync()` runs pending EF Core migrations.
2. `GameDataSeeder.SeedAsync(db, logger)` runs.
3. Seed is applied **only to empty tables** (see [Seeder behavior](#seeder-behavior)).

Both steps run inside a single `await using` async scope, so the seeder shares the migrated `AppDbContext`.

## JSON location and embedding

```
Fantasy.Server/Domain/GameData/Seed/Data/
├── jobBaseStats.json
├── levels.json
├── stages.json
├── skills.json
└── weapons.json
```

All five files are included as `EmbeddedResource` in `Fantasy.Server.csproj`:

```xml
<ItemGroup>
    <EmbeddedResource Include="Domain\GameData\Seed\Data\*.json" />
</ItemGroup>
```

They are compiled **into the published DLL**, so the deployment image needs no separate data files — the seeder reads them from the assembly manifest stream.

## JSON schemas

Property names are matched case-insensitively. **Enum values must be written in PascalCase** (the C# enum member name), e.g. `"Warrior"`, `"AtkPercent"`, `"S"`. A typo or wrong casing fails `LoadAllSeeds_ShouldParseSuccessfully`.

### jobBaseStats.json
| Field | Type | Notes |
|---|---|---|
| `jobType` | enum `JobType` | `Warrior` \| `Archer` \| `Mage` |
| `baseHp` | long | |
| `baseAtk` | long | |
| `critRate` | double | 0.0–1.0 |
| `critDmgMultiplier` | double | |
| `hpPerLevel` | double | |
| `atkPerLevel` | double | |

### levels.json
| Field | Type | Notes |
|---|---|---|
| `level` | long | key |
| `requiredExp` | long | cumulative exp to reach this level |
| `rewardSp` | long | SP granted on reaching this level |

### stages.json
| Field | Type | Notes |
|---|---|---|
| `stage` | long | key |
| `monsterHp` | long | |
| `monsterAtk` | long | |
| `xpPerSecond` | long | idle reward rate |
| `goldPerSecond` | long | idle reward rate |
| `isBossStage` | bool | |

### skills.json
| Field | Type | Notes |
|---|---|---|
| `skillId` | int | key, unique |
| `jobType` | enum `JobType` | |
| `isActive` | bool | `true` = active skill, `false` = passive |
| `spCost` | long | |
| `prereqSkillId` | int? | `null` for root skills |
| `effectType` | enum `SkillEffectType` | `AtkFlat` \| `AtkPercent` \| `HpFlat` \| `HpPercent` \| `CritRate` \| `CritDmg` \| `CooldownReduce` \| `ElementalBoost` |
| `effectValue` | double | |

### weapons.json
| Field | Type | Notes |
|---|---|---|
| `weaponId` | int | key |
| `name` | string | max 50 chars |
| `grade` | enum `WeaponGrade` | `C` \| `B` \| `A` \| `S` |
| `jobType` | enum `JobType` | |
| `baseAtk` | long | |
| `atkPerEnhancement` | long | |

## Seeder behavior

The seeder is **idempotent per table** and intentionally conservative — game reference data is not modified during normal operation, so it never overwrites or deletes existing rows.

For each table it compares the existing row count against the seed count:

| Existing rows | Action |
|---|---|
| `0` | Insert all seed rows |
| `== seed count` | Already seeded — skip |
| `0 < count < seed count`, or `> seed count` | **Log a warning and skip.** No automatic repair. |

The mismatch case is treated as a possible data-integrity problem (partial corruption, or a real production dataset larger than the placeholder seed). Auto-fixing here is dangerous — e.g. a live table with 300 balanced rows must not be wiped by a 5-row placeholder seed. **An operator must inspect and fix the table manually.**

All inserts are flushed with a single `SaveChangesAsync()` after every table has been processed.

## How to change data

Balance changes follow:

1. Edit the relevant JSON file.
2. Run the tests (`/test`) — validates parsing and skill-tree constraints.
3. Deploy.

The `GameDataSeeder` code does **not** change for data edits — only the JSON does.

> **Important:** The seeder only seeds *empty* tables. If a table already contains rows, redeploying will **not** update it (it will hit the "already seeded" or "mismatch → skip" branch). To apply new values to an already-populated table, an operator must clear/adjust the table manually and let the seeder repopulate it.

## SkillData constraints

Seed skill data must satisfy (enforced by `SkillSeedDataTests`):

- `SkillId` is unique.
- `PrereqSkillId` references an existing `SkillId`.
- A skill and its prerequisite share the same `JobType` (skill trees connect only within a job).
- No cycles in the prerequisite chain.
- A skill cannot reference itself as a prerequisite.

The skill tree allows only a **single prerequisite** per skill; branches are expressed by multiple skills pointing at the same `PrereqSkillId`.

## Placeholder data

The current seed values are **minimal placeholders** for functional verification (the server cannot run idle-reward settlement or skill unlock against empty tables). They are not balanced game values. When real balance data is finalized, replace the JSON contents — no `GameDataSeeder` structure change is required.
