using Omnifactotum.Annotations;

namespace Omnifactotum.NUnit
{
    /// <summary>
    ///     Provides helper functionality for the
    ///     <see cref="MappingAccordances{TSource,TDestination}"/> class.
    /// </summary>
    public static class MappingAccordances
    {
        /// <summary>
        ///     Provides the fluid syntax of creating instances of the
        ///     <see cref="MappingAccordances{TSource,TDestination}"/> class.
        /// </summary>
        /// <typeparam name="TSource">
        ///     The type of a source object.
        /// </typeparam>
        public static class From<TSource>
        {
            /// <summary>
            ///     Creates a new instance of the
            ///     <see cref="MappingAccordances{TSource,TDestination}"/> class.
            /// </summary>
            /// <typeparam name="TDestination">
            ///     The type of a destination object.
            /// </typeparam>
            /// <returns>
            ///     A new instance of the <see cref="MappingAccordances{TSource,TDestination}"/> class.
            /// </returns>
            [Pure]
            public static MappingAccordances<TSource, TDestination> To<TDestination>()
                => new MappingAccordances<TSource, TDestination>();
        }

        /// <summary>
        ///     Represents a reference to a method that creates an assertion failure message based on
        ///     the specified source and destination expressions and values.
        /// </summary>
        /// <param name="sourceExpression">
        ///     A string representation of the expression selecting the property in a source object.
        /// </param>
        /// <param name="sourceValue">
        ///     The value of the property in a source object.
        /// </param>
        /// <param name="destinationExpression">
        ///     A string representation of the expression selecting the property in a destination object.
        /// </param>
        /// <param name="destinationValue">
        ///     The value of the property in a destination object.
        /// </param>
        /// <typeparam name="TValue">
        ///     The type of the source and destination properties.
        /// </typeparam>
        public delegate string AssertionFailedMessageCreator<in TValue>(
            string sourceExpression,
            TValue sourceValue,
            string destinationExpression,
            TValue destinationValue);
    }
}