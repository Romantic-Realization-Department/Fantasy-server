---
paths:
  - "**/*.cs"
---

# No Fully Qualified Names

Do not use fully qualified type names inline unless there is a real ambiguity that cannot be resolved cleanly. Prefer adding a `using` directive and using the short type name.

```csharp
// wrong
private readonly Microsoft.Extensions.Logging.ILogger<AccountService> _logger;

// correct
using Microsoft.Extensions.Logging;

private readonly ILogger<AccountService> _logger;
```
