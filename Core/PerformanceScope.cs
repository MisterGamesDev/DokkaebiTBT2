using System;
using System.Diagnostics;
using Dokkaebi.Utilities;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Utility for measuring and logging performance of code blocks
    /// </summary>
    public class PerformanceScope : IDisposable
    {
        private readonly string scopeName;
        private readonly Stopwatch stopwatch;
        private readonly bool logOnDispose;
        
        public PerformanceScope(string name, bool logOnDispose = true)
        {
            scopeName = name;
            this.logOnDispose = logOnDispose;
            stopwatch = Stopwatch.StartNew();
            
            SmartLogger.Log($"Starting {scopeName}", LogCategory.Performance);
        }
        
        public void Dispose()
        {
            stopwatch.Stop();
            if (logOnDispose)
            {
                SmartLogger.Log($"{scopeName} completed in {stopwatch.ElapsedMilliseconds}ms", LogCategory.Performance);
            }
        }
        
        /// <summary>
        /// Get the elapsed time in milliseconds
        /// </summary>
        public long GetElapsedMs()
        {
            return stopwatch.ElapsedMilliseconds;
        }
    }
}