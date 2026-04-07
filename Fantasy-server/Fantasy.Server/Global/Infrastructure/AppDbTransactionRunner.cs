using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Fantasy.Server.Global.Infrastructure;

public class AppDbTransactionRunner : IAppDbTransactionRunner
{
    private readonly AppDbContext _dbContext;

    public AppDbTransactionRunner(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteAsync(Func<Task> action, IsolationLevel? isolationLevel = null)
    {
        await ExecuteAsync<object?>(async () =>
        {
            await action();
            return null;
        }, isolationLevel);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, IsolationLevel? isolationLevel = null)
    {
        if (_dbContext.Database.CurrentTransaction != null)
            return await action();

        var transaction = isolationLevel.HasValue
            ? await _dbContext.Database.BeginTransactionAsync(isolationLevel.Value)
            : await _dbContext.Database.BeginTransactionAsync();

        await using (transaction)
        {
            try
            {
                var result = await action();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
