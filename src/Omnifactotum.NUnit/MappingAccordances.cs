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
        private readonly List<Action<TSource, TDestination>> _assertions;

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
        ///     Registers a mapping using the specified source and destination property selectors and
        ///     specified constraint and error message.
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
                (obj1, obj2) =>
                {
                    var sourcePropertyValue = sourcePropertySelector(obj1);
                    var destinationPropertyValue = destinationPropertySelector(obj2);

                    var constraint = createConstraintFromSourcePropertyValue(sourcePropertyValue);

                    Assert.That(
                        constraint,
                        Is.Not.Null,
                        $@"A factory method specified in the argument '{
                            nameof(createConstraintFromSourcePropertyValue)}' returned null.");

                    var message = createAssertionFailedMessage(
                        ToReadableString(sourcePropertySelectorExpression),
                        sourcePropertyValue,
                        ToReadableString(destinationPropertySelectorExpression),
                        destinationPropertyValue);

                    Assert.That(destinationPropertyValue, constraint, message);
                });

            return this;
        }

        /// <summary>
        ///     Registers a mapping using the specified source and destination property selectors and
        ///     <see cref="Is.EqualTo"/> constraint.
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
            Expression<Func<TSource, TValue>> sourcePropertySelectorExpression,
            Expression<Func<TDestination, TValue>> destinationPropertySelectorExpression)
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
        public void AssertAll([NotNull] TSource source, [NotNull] TDestination destination)
        {
            Assert.That(source, Is.Not.Null);
            Assert.That(destination, Is.Not.Null);

            foreach (var assertion in _assertions)
            {
                assertion(source, destination);
            }
        }

        private static string ToReadableString<T, TResult>(Expression<Func<T, TResult>> expression)
        {
            Assert.That(expression, Is.Not.Null);

            var result = string.Format(
                CultureInfo.InvariantCulture,
                "({0} {1}) => {2} : {3}",
                typeof(T).GetFullName(),
                expression.Parameters.Single().Name,
                expression.Body.ToString(),
                expression.ReturnType.GetQualifiedName());

            return result;
        }
    }
}