using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
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
        private readonly List<Action<TSource, TDestination>> _assertions;

        [CanBeNull]
        private volatile Func<TSource, TDestination, bool> _nullReferenceAssertion;

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="MappingAccordances{TSource,TDestination}"/> class.
        /// </summary>
        public MappingAccordances()
        {
            _assertions = new List<Action<TSource, TDestination>>();
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
        ///                 If source is NOT <c>null</c>, then destination must also be NOT
        ///                 <c>null</c>.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </summary>
        /// <returns>
        ///     This <see cref="MappingAccordances{TSource,TDestination}"/> instance.
        /// </returns>
        /// <remarks>
        ///     In case when the null reference check is registered, if both source and destinaton
        ///     passed to <see cref="AssertAll"/> are <c>null</c>, then assertions for all the
        ///     registered mappings are skipped and the <see cref="AssertAll"/> call is deemed
        ///     successful.
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
                            "Both the source and destination must be either null or non-null.");

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
        /// <param name="createAssertionFailedMessage">
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
            [NotNull] MappingAccordances.AssertionFailedMessageCreator<TValue> createAssertionFailedMessage)
        {
            Assert.That(sourcePropertySelectorExpression, Is.Not.Null);
            Assert.That(destinationPropertySelectorExpression, Is.Not.Null);
            Assert.That(createConstraintFromSourcePropertyValue, Is.Not.Null);
            Assert.That(createAssertionFailedMessage, Is.Not.Null);

            var sourcePropertySelector = sourcePropertySelectorExpression.Compile();
            var destinationPropertySelector = destinationPropertySelectorExpression.Compile();

            _assertions.Add(
                (source, destination) =>
                {
                    var sourcePropertyValue = sourcePropertySelector(source);
                    var destinationPropertyValue = destinationPropertySelector(destination);

                    var constraint = createConstraintFromSourcePropertyValue(sourcePropertyValue);

                    Assert.That(
                        constraint,
                        Is.Not.Null,
                        $@"A factory method specified in the argument '{
                            nameof(createConstraintFromSourcePropertyValue)}' returned invalid value.");

                    var message = createAssertionFailedMessage(
                        ToReadableString(sourcePropertySelectorExpression),
                        sourcePropertyValue,
                        ToReadableString(destinationPropertySelectorExpression),
                        destinationPropertyValue);

                    Assert.That(
                        message,
                        Is.Not.Null.And.Not.Empty,
                        $@"A factory method specified in the argument '{
                            nameof(createAssertionFailedMessage)}' returned invalid value.");

                    Assert.That(destinationPropertyValue, constraint, message);
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

            _assertions.Add(
                (source, destination) =>
                {
                    var sourcePropertyValue = sourcePropertySelector(source);
                    var destinationPropertyValue = destinationPropertySelector(destination);

                    innerMappingAccordances.AssertAll(sourcePropertyValue, destinationPropertyValue);
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
                (sourceExpression, sourceValue, destinationExpression, destinationValue) =>
                    $@"The values are expected to be equal:{Environment.NewLine}Source: {sourceExpression}{
                        Environment.NewLine}Destination: {destinationExpression}");

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
        {
            Assert.That(_assertions.Count, Is.Not.EqualTo(0), MappingAccordances.NoMappingsMessage);

            var allNull = _nullReferenceAssertion?.Invoke(source, destination);
            if (allNull.GetValueOrDefault())
            {
                return;
            }

            foreach (var assertion in _assertions)
            {
                assertion(source, destination);
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
    }
}