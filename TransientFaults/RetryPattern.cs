﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace TransientFaults {
    public class RetryPattern : IRetryPattern {
        public class Config {
            public short RetryCount { get; set; } = 1;
            public TimeSpan RetryBackoffTimeSpan { get; set; }
        }

        public T Retry<T>(Func<T> func,Config config = null,Func<T,bool> retryIfTrue = null,
            ICircuitBreaker circuitBreaker = null)
            => RetryAsync(_ => Task.FromResult(func()),new CancellationToken(),config,retryIfTrue,circuitBreaker)
                .GetAwaiter().GetResult();

        public void Retry(Action action,Config config = null,ICircuitBreaker circuitBreaker = null)
            => RetryAsync(action,new CancellationToken(),config,circuitBreaker).GetAwaiter().GetResult();

        public Task RetryAsync(Action action,CancellationToken token,Config config = null,
            ICircuitBreaker circuitBreaker = null)
            => RetryAsync(_ => {
                action();
                return Task.FromResult(0);
            },token,config,null,circuitBreaker);

        public Task<T> RetryAsync<T>(Func<CancellationToken,Task<T>> funcAsync,CancellationToken token,Config config = null,Func<T,bool> retryIfTrue = null,ICircuitBreaker circuitBreaker = null)
            => RetryAsync(funcAsync,token,circuitBreaker,null,config,retryIfTrue);
        public async Task<T> RetryAsync<T>(Func<CancellationToken,Task<T>> funcAsync,CancellationToken token,ICircuitBreaker circuitBreaker,Func<Task<T>> circutOpenValue,Config config = null,Func<T,bool> retryIfTrue = null) {
            var lastException = new Exception("Config has a RetryCount < 0.");
            config = config ?? new Config();
            var retryBackoffTimeSpan = config.RetryBackoffTimeSpan.Milliseconds > 0 ? config.RetryBackoffTimeSpan.Milliseconds : 5;

            for(var count = 0; count <= config.RetryCount; count++) {
                token.ThrowIfCancellationRequested();
                try {
                    T result;
                    if(circuitBreaker != null) {
                        result = await circuitBreaker.ExecuteTaskAsync(ct => funcAsync(token),token,circutOpenValue).ConfigureAwait(false);
                    }
                    else {
                        result = await funcAsync(token).ConfigureAwait(false);
                    }
                    if(result != null && retryIfTrue?.Invoke(result) == false) {
                        throw new PredicateNotMetException<T>($"The {nameof(retryIfTrue)} condition was not met",result);
                    }

                    return result;
                }
                catch(OperationCanceledException) { throw; }
                catch(CircuitBreakerOpenException) { throw; }
                catch(Exception ex) {
                    lastException = ex;
                }
                if(count >= config.RetryCount)
                    continue;
                await Task.Delay(retryBackoffTimeSpan,token).ConfigureAwait(false);
                retryBackoffTimeSpan += retryBackoffTimeSpan;
            }
            throw lastException;
        }
    }
}