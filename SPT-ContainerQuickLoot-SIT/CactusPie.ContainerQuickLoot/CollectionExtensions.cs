using System.Collections.Generic;

namespace CactusPie.ContainerQuickLoot
{
    public static class CollectionExtensions
    {
        /// <summary>Returns the only element of a sequence, or a default value if the sequence is empty or contains more than one element</summary>
        /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return the single element of.</param>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <returns>The single element of the input sequence, or <see langword="default" />(<paramref name="TSource" />) if the sequence contains more than one or no elements.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="source" /> is <see langword="null" />.</exception>
        public static TSource SingleOrDefaultWithoutException<TSource>(this IEnumerable<TSource> source) 
            where TSource : class
        {
            TSource result = default;
            
            foreach (TSource element in source)
            {
                if (result != null)
                {
                    return default;
                }
                
                result = element;
            }

            return result;
        }
    }
}