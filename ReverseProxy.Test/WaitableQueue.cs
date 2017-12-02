using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReverseProxy.Test.Helper;

namespace ReverseProxy.Test
{
    [TestClass]
    public class WaitableQueueTest
    {
        [TestMethod]
        public async Task OneElementInQueue()
        {
            var queue = new WaitableQueue<int>();

            var testElement = 1;
            queue.Enqueue(testElement);

            var result = await queue.Dequeue();

            Assert.AreEqual(testElement, result);
        }

        [TestMethod]
        public async Task MultipleElementInQueue()
        {
            var queue = new WaitableQueue<int>();

            for(var i = 0; i < 10; ++i)
            {
                queue.Enqueue(i);
            }

            for(var i = 0; i < 10; ++i)
            {
                var result = await queue.Dequeue();
                Assert.AreEqual(i, result);
            }
        }

        [TestMethod]
        public async Task DequeueTimeout()
        {
            var queue = new WaitableQueue<int>();

            try
            {
                await queue.Dequeue(TimeSpan.FromSeconds(1));
            }
            catch(TimeoutException)
            {
                //ok
                return;
            }

            Assert.Fail("Should throw exception");
        }

        [TestMethod]
        public async Task CheckConcurrentSequenceOrder()
        {
            var queue = new WaitableQueue<int>();

            var sequence = new int[128];
            for(var i = 0; i < sequence.Length; ++i)
            {
                sequence[i] = i;
            }

            var task = Task.Delay(0);

            for(int i = 0; i < sequence.Length; i++)
            {
                var queueItem = i;
                task = task.ContinueWith(t => queue.Enqueue(queueItem));
            }

            await task;

            var dequeued = new List<int>();
            try
            {
                while(true)
                {
                    dequeued.Add(await queue.Dequeue(TimeSpan.FromSeconds(1)));
                }
            }
            catch(TimeoutException)
            {
                //ok
            }

            Assert.AreEqual(sequence.Length, dequeued.Count);
            Assert.IsTrue(dequeued.SequenceEqual(sequence));
        }

        [TestMethod]
        public async Task ConcurrentAccess()
        {
            var queue = new WaitableQueue<int>();

            var sequence = new int[128];
            for(var i = 0; i < sequence.Length; ++i)
            {
                sequence[i] = i;
            }

            Parallel.ForEach(sequence, value => queue.Enqueue(value));

            var dequeued = new List<int>();
            try
            {
                while(true)
                {
                    dequeued.Add(await queue.Dequeue(TimeSpan.FromSeconds(1)));
                }
            }
            catch(TimeoutException)
            {
                //ok
            }

            dequeued.Sort();

            Assert.AreEqual(sequence.Length, dequeued.Count);
            Assert.IsTrue(dequeued.SequenceEqual(sequence));
        }
    }
}