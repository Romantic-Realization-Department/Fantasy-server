## Request Flow

All API endpoints follow this layer order:

```
Client → Controller → Service → Repository → PostgreSQL / Redis
```

### Layer Responsibilities

| Layer | Responsibility |
|---|---|
| Controller | Receive request, call service, return `CommonApiResponse` |
| Service | Business logic, exception handling (`NotFoundException`, `ConflictException`, etc.) |
| Repository | Exclusive DB / Redis access (`AppDbContext`, `IConnectionMultiplexer`) |

### Authenticated Endpoints

Controllers or actions annotated with `[Authorize]` pass through `JwtAuthenticationFilter` on every request.
When the service needs the current user, it extracts claims via `ICurrentUserProvider`.

```
Client → JwtAuthenticationFilter → Controller → Service → ICurrentUserProvider (claims) → Repository
```

### Redis Cache Pattern

Always check Redis first on reads; only query the DB on a cache miss, then populate the cache.
After any write (update / delete), invalidate the relevant key.

```
Read : Redis hit → return / miss → query DB → Redis SET → return
Write: update DB → Redis DEL
```
