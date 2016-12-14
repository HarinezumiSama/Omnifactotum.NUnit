using Omnifactotum.Annotations;

namespace Omnifactotum.NUnit
{
    /// <summary>
    ///     Provides helper functionality for the
    ///     <see cref="MappingAccordances{TSource,TDestination}"/> class.
    /// </summary>
    public static class MappingAccordances
    {
        internal const string NoMappingsMessage =
            @"There must be at least one registered mapping to assert.";

        internal const string NullMismatchMessage =
            @"Both the source and destination must be either null or non-null.";

        internal const string ListValueNullMismatchMessage =
            @"Both the source and destination values must be either null or non-null";

        internal const string ListValueCountMismatchMessage =
            @"The source and destination must have the same item count";

        internal static readonly string InnerMappingSeparator = new string('-', 80);

        /// <summary>
        ///     Represents a reference to a method that returns an assertion failure message.
        /// </summary>
        /// <param name="sourceValue">
        ///     The value of the property in a source object.
        /// </param>
        /// <param name="destinationValue">
        ///     The value of the property in a destination object.
        /// </param>
        /// <typeparam name="TSourceValue">
        ///     The type of the value of a source property.
        /// </typeparam>
        /// <typeparam name="TDestinationValue">
        ///     The type of the value of a destination property.
        /// </typeparam>
        public delegate string AssertionFailedMessageCreator<in TSourceValue, in TDestinationValue>(
            [CanBeNull] TSourceValue sourceValue,
            [CanBeNull] TDestinationValue destinationValue);

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
    }
}