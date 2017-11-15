using System;
using System.Threading;
using System.Threading.Tasks;

namespace TransientFaults
{
    public interface IRetryPattern
    {
        void Retry(Action action,RetryPattern.Config config = null,ICircuitBreaker circuitBreaker = null);
        T Retry<T>(Func<T> func,RetryPattern.Config config = null,Func<T,bool> retryIfTrue = null,ICircuitBreaker circuitBreaker = null);
        Task RetryAsync(Action action,CancellationToken token,RetryPattern.Config config = null,ICircuitBreaker circuitBreaker = null);
        Task<T> RetryAsync<T>(Func<CancellationToken,Task<T>> funcAsync,CancellationToken token,RetryPattern.Config config = null,Func<T,bool> retryIfTrue = null,ICircuitBreaker circuitBreaker = null);
    }
}