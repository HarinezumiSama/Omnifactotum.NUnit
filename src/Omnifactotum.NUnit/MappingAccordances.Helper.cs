using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NUnit.Framework;
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

        private static readonly string InnerMappingSeparator = new string('-', 80);

        private static readonly StringComparer PropertyNameCaseSensitiveComparer = StringComparer.Ordinal;

        private static readonly StringComparer PropertyNameIgnoreCaseComparer = StringComparer.OrdinalIgnoreCase;

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

        internal static string ToReadableString<T, TResult>([NotNull] Expression<Func<T, TResult>> expression)
        {
            Assert.That(expression, Is.Not.Null);

            var expressionBodyString = expression.Body.ToString();

            var result = string.Format(
                CultureInfo.InvariantCulture,
                "({0} {1}) => {2} : {3}",
                typeof(T).GetFullName(),
                expression.Parameters.Single().Name,
                expressionBodyString,
                expression.ReturnType.GetQualifiedName());

            return result;
        }

        internal static bool IsNullReference<T>(T value) => !typeof(T).IsValueType && ReferenceEquals(value, null);

        internal static string CreateDetailedFailureMessage(
            [NotNull] string baseFailureMessage,
            [NotNull] string sourceExpression,
            [NotNull] string destinationExpression,
            [CanBeNull] string parentFailureMessage)
        {
            const char Colon = ':';

            Assert.That(
                baseFailureMessage,
                Is.Not.Null.And.Not.Empty,
                @"The base failure message cannot be empty nor null.");

            Assert.That(sourceExpression, Is.Not.Null);
            Assert.That(destinationExpression, Is.Not.Null);

            var messageBuilder = new StringBuilder(baseFailureMessage.TrimSafely());

            if (messageBuilder.Length > 0 && messageBuilder[messageBuilder.Length - 1] != Colon)
            {
                messageBuilder.Append(Colon);
            }

            if (!string.IsNullOrEmpty(parentFailureMessage))
            {
                var parentMessageFormatted = $@"{parentFailureMessage}{Environment.NewLine}{
                    InnerMappingSeparator}{Environment.NewLine}[Inner Mapping]{Environment.NewLine}";

                messageBuilder.Insert(0, parentMessageFormatted);
            }

            messageBuilder.Append(
                $@"{Environment.NewLine}* Source: {sourceExpression}{Environment.NewLine}* Destination: {
                    destinationExpression}{Environment.NewLine}");

            return messageBuilder.ToString();
        }

        internal static StringComparer GetPropertyNameComparer(bool ignoreCase)
            => ignoreCase ? PropertyNameIgnoreCaseComparer : PropertyNameCaseSensitiveComparer;

        internal static Dictionary<string, PropertyInfo> GetSimpleReadableProperties(
            [NotNull] Type type,
            bool ignoreCase)
        {
            const BindingFlags PropertyBindingFlags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Assert.That(type, Is.Not.Null);

            var propertyNameComparer = GetPropertyNameComparer(ignoreCase);

            var properties = type
                .GetProperties(PropertyBindingFlags)
                .Where(info => info.CanRead && info.PropertyType.IsSimpleType())
                .ToDictionary(info => info.Name, propertyNameComparer);

            return properties;
        }

        internal static LambdaExpression CreatePropertyExpression(
            Type type,
            PropertyInfo property,
            string parameterName)
        {
            Assert.That(type, Is.Not.Null);
            Assert.That(property, Is.Not.Null);
            Assert.That(parameterName, Is.Not.Null & Is.Not.Empty);

            var funcType = Expression.GetFuncType(type, property.PropertyType);
            var parameterExpression = Expression.Parameter(type, parameterName);
            var memberExpression = Expression.Property(parameterExpression, property);
            var lambdaExpression = Expression.Lambda(funcType, memberExpression, parameterExpression);

            return lambdaExpression;
        }

        private static bool IsSimpleType([NotNull] this Type type)
        {
            Assert.That(type, Is.Not.Null);

            return type.IsSimpleTypeInternal() || IsSimpleNullableTypeInternal(type);
        }

        private static bool IsSimpleTypeInternal([NotNull] this Type type) =>
            type.IsPrimitive
                || type.IsEnum
                || type.IsPointer
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(Pointer)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset);

        private static bool IsSimpleNullableTypeInternal([NotNull] this Type type)
#if NET40
            => type.IsNullable() && Nullable.GetUnderlyingType(type).EnsureNotNull().IsSimpleTypeInternal();
#else
            => type.IsNullableValueType() && Nullable.GetUnderlyingType(type).EnsureNotNull().IsSimpleTypeInternal();
#endif

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