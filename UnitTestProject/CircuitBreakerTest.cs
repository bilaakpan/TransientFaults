using FakeItEasy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using TransientFaults;

namespace UnitTestProject
{
    [TestClass]
    public class CircuitBreakerTest
    {
        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerOpenException),"The Circuit Breaker is in a Hard Open state")]
        public async Task HardOpenThrowsCircuitBreakerOpenException() {
            var config = A.Fake<ICircuitBreakerConfig>();
            config.FailureCount = 2;
            config.HealPeriod = TimeSpan.FromMilliseconds(100);

            var cb = new CircuitBreaker(config);
            cb.HardOpen();

            await cb.ExecuteTaskAsync(() => { });
        }
        [TestMethod]
        public async Task ReturnsExpectedResult()
        {
            var config = A.Fake<ICircuitBreakerConfig>();
            config.FailureCount = 2;
            config.HealPeriod = TimeSpan.FromMilliseconds(100);

            var cb = new CircuitBreaker(config);
            var actReturn = 1;
           var result= await cb.ExecuteTaskAsync((ct) =>  Task.FromResult(actReturn),new System.Threading.CancellationTokenSource().Token);
            Assert.AreEqual(actReturn,result);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task TipBraker()
        {
            var config = A.Fake<ICircuitBreakerConfig>();
            config.FailureCount = 1;
            config.HealPeriod = TimeSpan.FromMilliseconds(1);

            var cb = new CircuitBreaker(config);
            await cb.ExecuteTaskAsync((ct) => { throw new Exception("");return Task.FromResult(0); },new System.Threading.CancellationTokenSource().Token);
           
        }
        [TestMethod]
        public async Task CircuitBreakerHeals()
        {
            var config = A.Fake<ICircuitBreakerConfig>();
            config.FailureCount = 3;
            config.HealPeriod = TimeSpan.FromMilliseconds(1);

            var cb = new CircuitBreaker(config);
            for(var x = 0; x <= config.FailureCount+1; x++)
            {
                try
                {
                    await cb.ExecuteTaskAsync((ct) => { throw new Exception(""); return Task.FromResult(0); },new System.Threading.CancellationTokenSource().Token);
                }
                catch
                {
                }
            }
                await Task.Delay(1);
                var actReturn = 1;
                var result =await cb.ExecuteTask(() =>Task.FromResult(actReturn));
                Assert.AreEqual(actReturn,result);
        }
        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task OperationCanceled()
        {
            var config = A.Fake<ICircuitBreakerConfig>();
            config.FailureCount = 3;
            config.HealPeriod = TimeSpan.FromMilliseconds(1);

            var cb = new CircuitBreaker(config);
            for(var x = 0; x <= config.FailureCount ; x++)
            {
              
                try
                {
                    cb.ExecuteTask(() => throw new Exception(""));
                }
                catch
                {
                }
            }
            await Task.Delay(1);
            var cts=new System.Threading.CancellationTokenSource();
            var ct= cts.Token;
            cts.Cancel();

           await cb.ExecuteTaskAsync((t) => { t.ThrowIfCancellationRequested(); return Task.FromResult(0); },ct);
        }
    }
}
