using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MultiLevelCaching.Integration.Tests
{
    internal static class ThreadHelpers
    {
        public static void InvokeThreads(int numThreads, Action action)
        {
            var threads = new Thread[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                threads[i] = new Thread(Invoke);
            }
            for (int i = 0; i < numThreads; i++)
            {
                threads[i].Start(action);
            }
            for (int i = 0; i < numThreads; i++)
            {
                threads[i].Join();
            }
        }
        private static void Invoke(dynamic action) { action(); }

        public static void InvokeThreads<T>(this IEnumerable<T> items, ParameterizedThreadStart action)
        {
            var itemsArray = items.ToArray();
            int numThreads = itemsArray.Length;
            var threads = new Thread[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                threads[i] = new Thread(action);
            }
            for (int i = 0; i < numThreads; i++)
            {
                threads[i].Start(itemsArray[i]);
            }
            for (int i = 0; i < numThreads; i++)
            {
                threads[i].Join();
            }
        }
    }
}
