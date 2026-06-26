using System;
using Microsoft.Extensions.ObjectPool;
using AspNetCore.Localizer.Json.Format;

namespace AspNetCore.Localizer.Json.Localizer.Pooling
{
    /// <summary>
    /// Object pool for LocalizedFormat instances to reduce memory allocations.
    /// Reuses LocalizedFormat objects instead of creating new ones for each localization entry.
    /// Expected reduction: 20-30% of allocations in localization initialization.
    /// </summary>
    internal static class LocalizedFormatPool
    {
        private static readonly ObjectPool<LocalizedFormat> _pool = 
            new DefaultObjectPool<LocalizedFormat>(
                new LocalizedFormatPooledObjectPolicy());

        /// <summary>
        /// Rents a LocalizedFormat instance from the pool.
        /// </summary>
        /// <returns>A LocalizedFormat instance ready for use.</returns>
        public static LocalizedFormat Rent()
        {
            return _pool.Get();
        }

        /// <summary>
        /// Returns a LocalizedFormat instance to the pool for reuse.
        /// </summary>
        /// <param name="item">The LocalizedFormat instance to return.</param>
        public static void Return(LocalizedFormat item)
        {
            _pool.Return(item);
        }
    }

    /// <summary>
    /// Pooled object policy for LocalizedFormat.
    /// Manages creation and reset of LocalizedFormat instances for object pooling.
    /// </summary>
    internal class LocalizedFormatPooledObjectPolicy : IPooledObjectPolicy<LocalizedFormat>
    {
        /// <summary>
        /// Creates a new LocalizedFormat instance.
        /// </summary>
        /// <returns>A new LocalizedFormat instance.</returns>
        public LocalizedFormat Create()
        {
            return new LocalizedFormat();
        }

        /// <summary>
        /// Resets the state of a LocalizedFormat instance for reuse.
        /// </summary>
        /// <param name="obj">The LocalizedFormat instance to reset.</param>
        /// <returns>True if the object can be reused, false otherwise.</returns>
        public bool Return(LocalizedFormat obj)
        {
            // Reset state for reuse
            obj.IsParent = false;
            obj.Value = null;
            return true;
        }
    }
}
