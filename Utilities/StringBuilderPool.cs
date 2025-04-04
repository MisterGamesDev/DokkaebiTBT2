using System.Collections.Generic;
using System.Text;

namespace Dokkaebi.Utilities
{
    /// <summary>
    /// Provides a pool of StringBuilder objects to reduce garbage collection
    /// </summary>
    public static class StringBuilderPool
    {
        // Pool of available StringBuilder objects
        private static readonly Stack<StringBuilder> pool = new Stack<StringBuilder>();
        
        /// <summary>
        /// Get a StringBuilder from the pool or create a new one
        /// </summary>
        public static StringBuilder Get()
        {
            lock (pool)
            {
                if (pool.Count > 0)
                {
                    return pool.Pop();
                }
            }
            
            return new StringBuilder();
        }
        
        /// <summary>
        /// Return a StringBuilder to the pool
        /// </summary>
        public static void Return(StringBuilder builder)
        {
            if (builder == null)
                return;
                
            // Clear the builder for reuse
            builder.Clear();
            
            lock (pool)
            {
                pool.Push(builder);
            }
        }
        
        /// <summary>
        /// Get the builder's string content and return it to the pool
        /// </summary>
        public static string GetStringAndReturn(StringBuilder builder)
        {
            if (builder == null)
                return string.Empty;
                
            string result = builder.ToString();
            Return(builder);
            return result;
        }
    }

    /// <summary>
    /// Scope for automatic StringBuilder management using using statements
    /// </summary>
    public class StringBuilderScope : System.IDisposable
    {
        private StringBuilder builder;
        
        public StringBuilderScope(out StringBuilder builder)
        {
            this.builder = StringBuilderPool.Get();
            builder = this.builder;
        }
        
        public void Dispose()
        {
            if (builder != null)
            {
                StringBuilderPool.Return(builder);
                builder = null;
            }
        }
    }
}