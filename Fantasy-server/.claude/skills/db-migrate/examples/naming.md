# Migration Naming Guide — Fantasy Server

## Format

**PascalCase**, no spaces, clearly describes the schema operation being performed.

---

## Naming patterns by operation

### New table
```
Create{TableName}Table
```
e.g. `CreateAccountTable`, `CreateGameRoomTable`, `CreatePlayerTable`

### Initial schema (multiple tables at once)
```
CreateTables
InitialSchema
```
e.g. `CreateTables` ← the actual name of the first migration in this project

### Add column to existing table
```
Add{ColumnName}To{TableName}
```
e.g. `AddNicknameToAccount`, `AddStatusToGameRoom`, `AddRefreshTokenToAuth`

### Remove column
```
Remove{ColumnName}From{TableName}
```
e.g. `RemoveDeprecatedFieldFromAccount`

### Fix column type, constraint, or misconfiguration
```
Fix{ColumnName}{Issue}
Change{ColumnName}In{TableName}
```
e.g.
- `FixUpdatedAtValueGeneration` ← actual fix migration in this project
- `ChangeEmailMaxLengthInAccount`

### Add index
```
Add{Description}IndexTo{TableName}
```
e.g. `AddEmailUniqueIndexToAccount`, `AddCreatedAtIndexToGameRoom`

### Add foreign key
```
Add{Relation}ForeignKeyTo{TableName}
```
e.g. `AddAccountForeignKeyToGamePlayer`

---

## Anti-patterns (avoid)

| Bad | Good |
|---|---|
| `Migration1` | `CreateAccountTable` |
| `FixAccount` | `FixUpdatedAtValueGeneration` |
| `UpdateSchema` | `AddNicknameToAccount` |
| `Temp` | (describe what actually changed) |
| `Fix` | `FixEmailConstraintInAccount` |

---

## Migration history in this project

| Migration name | What it does |
|---|---|
| _(no migrations yet)_ | First migration should be named `CreateTables` |
