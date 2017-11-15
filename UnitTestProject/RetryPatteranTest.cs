using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using TransientFaults;
using static TransientFaults.RetryPattern;

namespace UnitTestProject
{
    [TestClass]
    public class RetryPatteranTest
    {
        [TestMethod]
        public void Retry_Count_Match()
        {
            var retry = new RetryPattern();
            var retryCount = 0;
            var firstAtemptOffset = 1;
            short timeToRetry = 4;
            var config = new Config
            {
                RetryCount = timeToRetry
            };
            try
            {
                retry.Retry(() =>
                            {
                                retryCount++;
                                throw new Exception("");
                            },config);

            }
            catch { }
            finally
            {
                Assert.AreEqual(timeToRetry,retryCount - firstAtemptOffset);
            }
            try
            {
                retryCount = 0;
                var item = retry.Retry(() =>
                {
                    retryCount++;
                    throw new Exception("");
                    return 0;
                },config);
            }
            catch { }
            finally
            {
                Assert.AreEqual(timeToRetry,retryCount - firstAtemptOffset);
            }

        }
        [TestMethod]
        [ExpectedException(exceptionType: typeof(PredicateNotMetException<int>))]
        public async Task Retry_Expecting_OperationCanceledException()
        {
            var retry = new RetryPattern();
            var cancelTimeSpan = new System.Threading.CancellationTokenSource();
            var config = new Config
            {
                RetryCount = 1
            };
            var item =
                  await retry.RetryAsync((token) => Task.FromResult(0)
                    ,cancelTimeSpan.Token,config,x => x != 0);

        }
        [TestMethod]
        public async Task Retry_ReturnsResult()
        {
            var retry = new RetryPattern();
            var cancelTimeSpan = new System.Threading.CancellationTokenSource();
            var config = new Config
            {
                RetryCount = 1
            };
            var item =
                  await retry.RetryAsync((token) => Task.FromResult(0)
                    ,cancelTimeSpan.Token,config,x => x == 0);

            Assert.IsInstanceOfType(item,typeof(int));
        }
        [TestMethod]
        public async Task Retry_ReturnsResultThroughCircuitBreaker()
        {
            var retry = new RetryPattern();
            var cancelTimeSpan = new System.Threading.CancellationTokenSource();
            var cb = A.Fake<ICircuitBreaker>();
            A.CallTo(cb).WithReturnType<Task<int>>()                        
            .WithAnyArguments().Returns(Task.FromResult(0));

            var config = new Config
            {
                RetryCount = 1
            };
            var item =  await retry.RetryAsync((token) => Task.FromResult(0)
                    ,cancelTimeSpan.Token,config,x => x == 0,cb);

            Assert.IsInstanceOfType(item,typeof(int));
        }
        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task Retry_OperationCanceledException()
        {
            var retry = new RetryPattern();
            var cancelTimeSpan = new System.Threading.CancellationTokenSource();
            var config = new Config
            {
                RetryCount = 1
            };
            var item =
                  await retry.RetryAsync((token) =>{
                      throw new OperationCanceledException();
                      return Task.FromResult(0); }
                    ,cancelTimeSpan.Token);
            
        }
        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerOpenException))]
        public async Task Retry_CircuitBreakerOpenException()
        {
            var retry = new RetryPattern();
            var cancelTimeSpan = new System.Threading.CancellationTokenSource();
            var config = new Config
            {
                RetryCount = 1
            };
            var item =
                  await retry.RetryAsync((token) => {
                      throw new CircuitBreakerOpenException("");
                      return Task.FromResult(0);
                  }
                    ,cancelTimeSpan.Token);
            
        }
    }
}