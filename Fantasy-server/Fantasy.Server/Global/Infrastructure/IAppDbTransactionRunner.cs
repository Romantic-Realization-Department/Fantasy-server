using System.Data;

namespace Fantasy.Server.Global.Infrastructure;

public interface IAppDbTransactionRunner
{
    Task ExecuteAsync(Func<Task> action, IsolationLevel? isolationLevel = null);
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, IsolationLevel? isolationLevel = null);
}
