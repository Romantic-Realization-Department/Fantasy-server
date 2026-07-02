using Fantasy.Server.Domain.Player.Entity;
using Fantasy.Server.Domain.Player.Repository.Interface;
using Fantasy.Server.Global.Infrastructure;

namespace Fantasy.Server.Domain.Player.Repository;

public class RewardTransactionRepository : IRewardTransactionRepository
{
    private readonly AppDbContext _db;

    public RewardTransactionRepository(AppDbContext db) => _db = db;

    public async Task SaveRangeAsync(List<RewardTransaction> transactions)
    {
        if (transactions.Count == 0)
            return;

        _db.RewardTransactions.AddRange(transactions);
        await _db.SaveChangesAsync();
    }
}
