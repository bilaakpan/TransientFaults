using System;
using System.Threading;
using System.Threading.Tasks;

namespace TransientFaults
{
    public interface ICircuitBreaker
    {
        string State { get; }
        Task<TResult> ExecuteTaskAsync<TResult>(Func<CancellationToken,Task<TResult>> action, CancellationToken token);
        void HardOpen();
        void Reset();
    }
}