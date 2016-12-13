using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Omnifactotum.Annotations;

namespace Omnifactotum.NUnit
{
    /// <summary>
    ///     <para>
    ///         Provides functionality to register an accordance between properties of the specified
    ///         source and destination types and then assert the values of these properties using an
    ///         instances of these types.
    ///     </para>
    /// </summary>
    /// <typeparam name="TSource">
    ///     The type of a source object.
    /// </typeparam>
    /// <typeparam name="TDestination">
    ///     The type of a destination object.
    /// </typeparam>
    public sealed class MappingAccordances<TSource, TDestination>
    {
        [NotNull]
        private readonly List<Action<TSource, TDestination, string>> _assertions;

        [CanBeNull]
        private volatile Func<TSource, TDestination, bool> _nullReferenceAssertion;

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="MappingAccordances{TSource,TDestination}"/> class.
        /// </summary>
        public MappingAccordances()
        {
            _assertions = new List<Action<TSource, TDestination, string>>();
        }

        /// <summary>
        ///     Gets the number of registered accordances.
        /// </summary>
        public int Count => _assertions.Count;

        /// <summary>
        ///     Gets a <see cref="bool"/> that indicates whether the null reference check is
        ///     registered (see <see cref="RegisterNullReferenceCheck"/>).
        /// </summary>
        public bool IsNullReferenceCheckRegistered => _nullReferenceAssertion != null;

        /// <summary>
        ///     Registers the check for null reference:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 If source is <c>null</c>, then destination must also be <c>null</c>.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 If source is NOT <c>null</c>, then destination must also be NOT <c>null</c>.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </summary>
        /// <returns>
        ///     This <see cref="MappingAccordances{TSource,TDestination}"/> instance.
        /// </returns>
        /// <remarks>
        ///     In case when the null reference check is registered, if both source and destinaton
        ///     passed to <see cref="AssertAll(TSource,TDestination)"/> are <c>null</c>, then
        ///     assertions for all the registered mappings are skipped and the
        ///     <see cref="AssertAll(TSource,TDestination)"/> call is deemed successful.
        /// </remarks>
        public MappingAccordances<TSource, TDestination> RegisterNullReferenceCheck()
        {
            if (_nullReferenceAssertion == null)
            {
                _nullReferenceAssertion =
                    (source, destination) =>
                    {
                        var isSourceNull = IsNullReference(source);
                        var isDestinationNull = IsNullReference(destination);

                        Assert.That(
                            isDestinationNull,
                            Is.EqualTo(isSourceNull),
                            MappingAccordances.NullMismatchMessage);

                        return isSourceNull && isDestinationNull;
                    };
            }

            return this;
        }

        /// <summary>
        ///     Registers a mapping using the specified source and destination property selectors and
        ///     the specified constraint and error message.
        /// </summary>
        /// <param name="sourcePropertySelectorExpression">
        ///     The expression selecting the property in a source object.
        /// </param>
        /// <param name="destinationPropertySelectorExpression">
        ///     The expression selecting the property in a destination object.
        /// </param>
        /// <param name="createConstraintFromSourcePropertyValue">
        ///     A reference to a method that creates an instance of <see cref="IResolveConstraint"/>
        ///     using the value of the property in a source object.
        /// </param>
        /// <param name="getAssertionFailureMessage">
        ///     A reference to a method that creates an assertion failure message based on the
        ///     specified source and destination expressions and values.
        /// </param>
        /// <typeparam name="TValue">
        ///     The type of a value produced by any of the two property selectors.
        /// </typeparam>
        /// <returns>
        ///     This <see cref="MappingAccordances{TSource,TDestination}"/> instance.
        /// </returns>
        [NotNull]
        public MappingAccordances<TSource, TDestination> Register<TValue>(
            [NotNull] Expression<Func<TSource, TValue>> sourcePropertySelectorExpression,
            [NotNull] Expression<Func<TDestination, TValue>> destinationPropertySelectorExpression,
            [NotNull] ConstraintCreator<TValue> createConstraintFromSourcePropertyValue,
            [NotNull] MappingAccordances.AssertionFailedMessageCreator<TValue> getAssertionFailureMessage)
        {
            Assert.That(sourcePropertySelectorExpression, Is.Not.Null);
            Assert.That(destinationPropertySelectorExpression, Is.Not.Null);
            Assert.That(createConstraintFromSourcePropertyValue, Is.Not.Null);
            Assert.That(getAssertionFailureMessage, Is.Not.Null);

            var sourcePropertySelector = sourcePropertySelectorExpression.Compile();
            var destinationPropertySelector = destinationPropertySelectorExpression.Compile();

            var sourceExpression = ToReadableString(sourcePropertySelectorExpression);
            var destinationExpression = ToReadableString(destinationPropertySelectorExpression);

            _assertions.Add(
                (source, destination, parentFailureMessage) =>
                {
                    var sourcePropertyValue = sourcePropertySelector(source);
                    var destinationPropertyValue = destinationPropertySelector(destination);

                    var constraint = createConstraintFromSourcePropertyValue(sourcePropertyValue);

                    Assert.That(
                        constraint,
                        Is.Not.Null,
                        $@"A factory method specified in the argument '{
                            nameof(createConstraintFromSourcePropertyValue)}' returned an invalid value.");

                    var baseFailureMessage = getAssertionFailureMessage(sourcePropertyValue, destinationPropertyValue);

                    Assert.That(
                        baseFailureMessage,
                        Is.Not.Null.And.Not.Empty,
                        $@"A factory method specified in the argument '{
                            nameof(getAssertionFailureMessage)}' returned an invalid value.");

                    var failureMessage = CreateDetailedFailureMessage(
                        baseFailureMessage,
                        sourceExpression,
                        destinationExpression,
                        parentFailureMessage);

                    Assert.That(destinationPropertyValue, constraint, failureMessage);
                });

            return this;
        }

        /// <summary>
        ///     Registers a mapping using the specified source and destination property selectors and
        ///     the specified inner <see cref="MappingAccordances{TSourceValue,TDestinationValue}"/>.
        /// </summary>
        /// <param name="sourcePropertySelectorExpression">
        ///     The expression selecting the property in a source object.
        /// </param>
        /// <param name="destinationPropertySelectorExpression">
        ///     The expression selecting the property in a destination object.
        /// </param>
        /// <param name="innerMappingAccordances">
        ///     An instance of <see cref="MappingAccordances{TSourceValue,TDestinationValue}"/>
        ///     specifying the mapping for the corresponding properties of the source and destination objects.
        /// </param>
        /// <typeparam name="TSourceValue">
        ///     The type of a value produced by the property selector on the source object.
        /// </typeparam>
        /// <typeparam name="TDestinationValue">
        ///     The type of a value produced by the property selector on the destination object.
        /// </typeparam>
        /// <returns>
        ///     This <see cref="MappingAccordances{TSource,TDestination}"/> instance.
        /// </returns>
        [NotNull]
        public MappingAccordances<TSource, TDestination> Register<TSourceValue, TDestinationValue>(
            [NotNull] Expression<Func<TSource, TSourceValue>> sourcePropertySelectorExpression,
            [NotNull] Expression<Func<TDestination, TDestinationValue>> destinationPropertySelectorExpression,
            [NotNull] MappingAccordances<TSourceValue, TDestinationValue> innerMappingAccordances)
        {
            Assert.That(sourcePropertySelectorExpression, Is.Not.Null);
            Assert.That(destinationPropertySelectorExpression, Is.Not.Null);
            Assert.That(innerMappingAccordances, Is.Not.Null);

            var sourcePropertySelector = sourcePropertySelectorExpression.Compile();
            var destinationPropertySelector = destinationPropertySelectorExpression.Compile();

            var sourceExpression = ToReadableString(sourcePropertySelectorExpression);
            var destinationExpression = ToReadableString(destinationPropertySelectorExpression);

            _assertions.Add(
                (source, destination, parentFailureMessage) =>
                {
                    var sourcePropertyValue = sourcePropertySelector(source);
                    var destinationPropertyValue = destinationPropertySelector(destination);

                    var failureMessage = CreateDetailedFailureMessage(
                        @"The values are expected to match",
                        sourceExpression,
                        destinationExpression,
                        parentFailureMessage);

                    innerMappingAccordances.AssertAllInternal(
                        sourcePropertyValue,
                        destinationPropertyValue,
                        failureMessage);
                });

            return this;
        }

        /// <summary>
        ///     Registers a mapping using the specified source and destination property selectors and
        ///     the specified inner <see cref="MappingAccordances{TSourceItem,TDestinationItem}"/>.
        /// </summary>
        /// <param name="sourcePropertySelectorExpression">
        ///     The expression selecting the property in a source object.
        /// </param>
        /// <param name="destinationPropertySelectorExpression">
        ///     The expression selecting the property in a destination object.
        /// </param>
        /// <param name="innerMappingAccordances">
        ///     An instance of <see cref="MappingAccordances{TSourceValue,TDestinationValue}"/>
        ///     specifying the mapping for the items in the corresponding properties of the source
        ///     and destination objects.
        /// </param>
        /// <typeparam name="TSourceItem">
        ///     The type of items in the list produced by the property selector on the source object.
        /// </typeparam>
        /// <typeparam name="TDestinationItem">
        ///     The type of items in the list produced by the property selector on the destination object.
        /// </typeparam>
        /// <returns>
        ///     This <see cref="MappingAccordances{TSource,TDestination}"/> instance.
        /// </returns>
        [NotNull]
        public MappingAccordances<TSource, TDestination> Register<TSourceItem, TDestinationItem>(
            [NotNull] Expression<Func<TSource, IList<TSourceItem>>> sourcePropertySelectorExpression,
            [NotNull] Expression<Func<TDestination, IList<TDestinationItem>>> destinationPropertySelectorExpression,
            [NotNull] MappingAccordances<TSourceItem, TDestinationItem> innerMappingAccordances)
        {
            Assert.That(sourcePropertySelectorExpression, Is.Not.Null);
            Assert.That(destinationPropertySelectorExpression, Is.Not.Null);
            Assert.That(innerMappingAccordances, Is.Not.Null);

            var sourcePropertySelector = sourcePropertySelectorExpression.Compile();
            var destinationPropertySelector = destinationPropertySelectorExpression.Compile();

            var sourceExpression = ToReadableString(sourcePropertySelectorExpression);
            var destinationExpression = ToReadableString(destinationPropertySelectorExpression);

            _assertions.Add(
                (source, destination, parentFailureMessage) =>
                {
                    var sourcePropertyValue = sourcePropertySelector(source);
                    var destinationPropertyValue = destinationPropertySelector(destination);

                    var isSourcePropertyValueNull = IsNullReference(sourcePropertyValue);
                    var isDestinationPropertyValueNull = IsNullReference(destinationPropertyValue);

                    var nullMismatchMessage = CreateDetailedFailureMessage(
                        MappingAccordances.ListValueNullMismatchMessage,
                        sourceExpression,
                        destinationExpression,
                        parentFailureMessage);

                    Assert.That(
                        isDestinationPropertyValueNull,
                        Is.EqualTo(isSourcePropertyValueNull),
                        nullMismatchMessage);

                    if (isSourcePropertyValueNull && isDestinationPropertyValueNull)
                    {
                        return;
                    }

                    var itemCount = destinationPropertyValue.Count;

                    var countMismatchMessage = CreateDetailedFailureMessage(
                        MappingAccordances.ListValueCountMismatchMessage,
                        sourceExpression,
                        destinationExpression,
                        parentFailureMessage);

                    Assert.That(itemCount, Is.EqualTo(sourcePropertyValue.Count), countMismatchMessage);

                    for (var index = 0; index < itemCount; index++)
                    {
                        var sourceItem = sourcePropertyValue[index];
                        var destinationItem = destinationPropertyValue[index];

                        var itemMismatchMessage = CreateDetailedFailureMessage(
                            $@"The source and destination must have the matching item at index {index}",
                            sourceExpression,
                            destinationExpression,
                            parentFailureMessage);

                        innerMappingAccordances.AssertAllInternal(sourceItem, destinationItem, itemMismatchMessage);
                    }
                });

            return this;
        }

        /// <summary>
        ///     Registers a mapping using the specified source and destination property selectors and
        ///     the <see cref="Is.EqualTo"/> constraint.
        /// </summary>
        /// <param name="sourcePropertySelectorExpression">
        ///     The expression selecting the property in a source object.
        /// </param>
        /// <param name="destinationPropertySelectorExpression">
        ///     The expression selecting the property in a destination object.
        /// </param>
        /// <typeparam name="TValue">
        ///     The type of a value produced by any of the two property selectors.
        /// </typeparam>
        /// <returns>
        ///     This <see cref="MappingAccordances{TSource,TDestination}"/> instance.
        /// </returns>
        [NotNull]
        public MappingAccordances<TSource, TDestination> Register<TValue>(
            [NotNull] Expression<Func<TSource, TValue>> sourcePropertySelectorExpression,
            [NotNull] Expression<Func<TDestination, TValue>> destinationPropertySelectorExpression)
            => Register(
                sourcePropertySelectorExpression,
                destinationPropertySelectorExpression,
                expectedValue => Is.EqualTo(expectedValue),
                (sourceValue, destinationValue) => @"The values are expected to be equal");

        /// <summary>
        ///     Executes assertion of all the registered mappings for the specified source and
        ///     destination objects.
        /// </summary>
        /// <param name="source">
        ///     The source object.
        /// </param>
        /// <param name="destination">
        ///     The destination object.
        /// </param>
        public void AssertAll([CanBeNull] TSource source, [CanBeNull] TDestination destination)
            => AssertAllInternal(source, destination, null);

        private void AssertAllInternal(
            [CanBeNull] TSource source,
            [CanBeNull] TDestination destination,
            string parentFailureMessage)
        {
            Assert.That(_assertions.Count, Is.Not.EqualTo(0), MappingAccordances.NoMappingsMessage);

            var allNull = _nullReferenceAssertion?.Invoke(source, destination);
            if (allNull.GetValueOrDefault())
            {
                return;
            }

            foreach (var assertion in _assertions)
            {
                assertion(source, destination, parentFailureMessage);
            }
        }

        private static string ToReadableString<T, TResult>([NotNull] Expression<Func<T, TResult>> expression)
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

        private static bool IsNullReference<T>(T value) => !typeof(T).IsValueType && ReferenceEquals(value, null);

        private static string CreateDetailedFailureMessage(
            string baseFailureMessage,
            string sourceExpression,
            string destinationExpression,
            string parentFailureMessage)
        {
            const char Colon = ':';

            Assert.That(
                baseFailureMessage,
                Is.Not.Null.And.Not.Empty,
                @"The base failure message cannot be empty nor null.");

            var messageBuilder = new StringBuilder(baseFailureMessage.TrimSafely());

            if (messageBuilder.Length > 0 && messageBuilder[messageBuilder.Length - 1] != Colon)
            {
                messageBuilder.Append(Colon);
            }

            if (!string.IsNullOrEmpty(parentFailureMessage))
            {
                var parentMessageFormatted = $@"{parentFailureMessage}{Environment.NewLine}{
                    MappingAccordances.InnerMappingSeparator}{Environment.NewLine}[Inner Mapping]{
                    Environment.NewLine}";

                messageBuilder.Insert(0, parentMessageFormatted);
            }

            messageBuilder.Append(
                $@"{Environment.NewLine}* Source: {sourceExpression}{Environment.NewLine}* Destination: {
                    destinationExpression}{Environment.NewLine}");

            return messageBuilder.ToString();
        }
    }
}