using System;
using System.Threading;
using System.Threading.Tasks;
using static TransientFaults.CircuitBreaker;

namespace TransientFaults
{
    public partial class RetryPatteran : IRetryPatteran
    {
        public class Config
        {
            public short RetryCount { get; set; } = 1;
            public TimeSpan RetryBackoffTimeSpan { get; set; }
        }

        public T Retry<T>(Func<T> func,Config config = null,Func<T,bool> retryIfTrue = null,ICircuitBreaker circuitBreaker = null)
        => RetryAsync((_) => Task.FromResult(func()),new CancellationToken(),config,retryIfTrue,circuitBreaker).Result;


        public void Retry(Action action,Config config = null,ICircuitBreaker circuitBreaker = null)
        => RetryAsync(() => action(),new CancellationToken(),config,circuitBreaker).Wait();


        public Task RetryAsync(Action action,CancellationToken token,Config config = null,ICircuitBreaker circuitBreaker = null)
        => RetryAsync<int>((ct) => { action(); return Task.FromResult(0); },token,config,null,circuitBreaker);


        public async Task<T> RetryAsync<T>(Func<CancellationToken,Task<T>> funcAsync,CancellationToken token,Config config = null,Func<T,bool> retryIfTrue = null,ICircuitBreaker circuitBreaker = null)
        {
            var lastException = new Exception("Config has a RetryCount < 0.");
            config = config ?? new Config();
            var retryBackoffTimeSpan = config.RetryBackoffTimeSpan.Milliseconds > 0 ? config.RetryBackoffTimeSpan.Milliseconds : 5;

            for(var count = 0; count <= config.RetryCount; count++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    T result;
                    if(circuitBreaker != null)
                    {
                        result = await circuitBreaker.ExecuteTaskAsync(async (ct) => await funcAsync(token).ConfigureAwait(false),token).ConfigureAwait(false);
                    }
                    else
                    {
                        result = await funcAsync(token).ConfigureAwait(false);
                    }
                    if(result != null && retryIfTrue?.Invoke(result) == true)
                    {
                        throw new PredicateNotMetException($"The {nameof(retryIfTrue)} condition was not meet");
                    }

                    return result;
                }
                catch(OperationCanceledException) { throw; }
                catch(CircuitBreakerOpenException) { throw; }
                catch(Exception ex)
                {
                    lastException = ex;
                }
                if(count < config.RetryCount)
                {
                    await Task.Delay(retryBackoffTimeSpan,token).ConfigureAwait(false);
                    retryBackoffTimeSpan += retryBackoffTimeSpan;
                }
            }
            throw lastException;
        }
    }
}