using System;
using System.Threading;
using System.Threading.Tasks;

namespace TransientFaults
{
    public interface IRetryPatteran
    {
        void Retry(Action action,RetryPatteran.Config config = null,ICircuitBreaker circuitBreaker = null);
        T Retry<T>(Func<T> func,RetryPatteran.Config config = null,Func<T,bool> retryIfTrue = null,ICircuitBreaker circuitBreaker = null);
        Task RetryAsync(Action action,CancellationToken token,RetryPatteran.Config config = null,ICircuitBreaker circuitBreaker = null);
        Task<T> RetryAsync<T>(Func<CancellationToken,Task<T>> funcAsync,CancellationToken token,RetryPatteran.Config config = null,Func<T,bool> retryIfTrue = null,ICircuitBreaker circuitBreaker = null);
    }
}