using Fantasy.Server.Domain.Player.Entity;

namespace Fantasy.Server.Domain.Player.Repository.Interface;

public interface IRewardTransactionRepository
{
    Task SaveRangeAsync(List<RewardTransaction> transactions);
}
