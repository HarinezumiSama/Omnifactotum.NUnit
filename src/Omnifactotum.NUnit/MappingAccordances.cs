using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
        private readonly Lazy<MethodInfo> _registerMatchingPropertyMethodInfo;

        [NotNull]
        private readonly List<Action<TSource, TDestination, string>> _assertions;

        [CanBeNull]
        private volatile Func<TSource, TDestination, bool> _nullReferenceAssertion;

        private delegate MappingAccordances<TSource, TDestination> RegisterMatchingProperty(
            Expression<Func<TSource, object>> sourcePropertySelectorExpression,
            Expression<Func<TDestination, object>> destinationPropertySelectorExpression);

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="MappingAccordances{TSource,TDestination}"/> class.
        /// </summary>
        public MappingAccordances()
        {
            _registerMatchingPropertyMethodInfo = Lazy.Create(
                () => new RegisterMatchingProperty(Register).Method.GetGenericMethodDefinition());

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
                        var isSourceNull = MappingAccordances.IsNullReference(source);
                        var isDestinationNull = MappingAccordances.IsNullReference(destination);

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
        /// <typeparam name="TSourceValue">
        ///     The type of a value produced by the source property selector.
        /// </typeparam>
        /// <typeparam name="TDestinationValue">
        ///     The type of a value produced by the destination property selector.
        /// </typeparam>
        /// <returns>
        ///     This <see cref="MappingAccordances{TSource,TDestination}"/> instance.
        /// </returns>
        [NotNull]
        public MappingAccordances<TSource, TDestination> Register<TSourceValue, TDestinationValue>(
            [NotNull] Expression<Func<TSource, TSourceValue>> sourcePropertySelectorExpression,
            [NotNull] Expression<Func<TDestination, TDestinationValue>> destinationPropertySelectorExpression,
            [NotNull] ConstraintCreator<TSourceValue> createConstraintFromSourcePropertyValue,
            [NotNull] MappingAccordances.AssertionFailedMessageCreator<TSourceValue, TDestinationValue>
                getAssertionFailureMessage)
        {
            Assert.That(sourcePropertySelectorExpression, Is.Not.Null);
            Assert.That(destinationPropertySelectorExpression, Is.Not.Null);
            Assert.That(createConstraintFromSourcePropertyValue, Is.Not.Null);
            Assert.That(getAssertionFailureMessage, Is.Not.Null);

            var sourcePropertySelector = sourcePropertySelectorExpression.Compile();
            var destinationPropertySelector = destinationPropertySelectorExpression.Compile();

            var sourceExpression = MappingAccordances.ToReadableString(sourcePropertySelectorExpression);
            var destinationExpression = MappingAccordances.ToReadableString(destinationPropertySelectorExpression);

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

                    var failureMessage = MappingAccordances.CreateDetailedFailureMessage(
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
        /// <param name="assertionFailureMessage">
        ///     An assertion failure message.
        /// </param>
        /// <typeparam name="TSourceValue">
        ///     The type of a value produced by the source property selector.
        /// </typeparam>
        /// <typeparam name="TDestinationValue">
        ///     The type of a value produced by the destination property selector.
        /// </typeparam>
        /// <returns>
        ///     This <see cref="MappingAccordances{TSource,TDestination}"/> instance.
        /// </returns>
        [NotNull]
        public MappingAccordances<TSource, TDestination> Register<TSourceValue, TDestinationValue>(
            [NotNull] Expression<Func<TSource, TSourceValue>> sourcePropertySelectorExpression,
            [NotNull] Expression<Func<TDestination, TDestinationValue>> destinationPropertySelectorExpression,
            [NotNull] ConstraintCreator<TSourceValue> createConstraintFromSourcePropertyValue,
            [NotNull] string assertionFailureMessage)
            => Register(
                sourcePropertySelectorExpression,
                destinationPropertySelectorExpression,
                createConstraintFromSourcePropertyValue,
                (sourceValue, destinationValue) => assertionFailureMessage);

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

            var sourceExpression = MappingAccordances.ToReadableString(sourcePropertySelectorExpression);
            var destinationExpression = MappingAccordances.ToReadableString(destinationPropertySelectorExpression);

            _assertions.Add(
                (source, destination, parentFailureMessage) =>
                {
                    var sourcePropertyValue = sourcePropertySelector(source);
                    var destinationPropertyValue = destinationPropertySelector(destination);

                    var failureMessage = MappingAccordances.CreateDetailedFailureMessage(
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

            var sourceExpression = MappingAccordances.ToReadableString(sourcePropertySelectorExpression);
            var destinationExpression = MappingAccordances.ToReadableString(destinationPropertySelectorExpression);

            _assertions.Add(
                (source, destination, parentFailureMessage) =>
                {
                    var sourcePropertyValue = sourcePropertySelector(source);
                    var destinationPropertyValue = destinationPropertySelector(destination);

                    var isSourcePropertyValueNull = MappingAccordances.IsNullReference(sourcePropertyValue);
                    var isDestinationPropertyValueNull = MappingAccordances.IsNullReference(destinationPropertyValue);

                    var nullMismatchMessage = MappingAccordances.CreateDetailedFailureMessage(
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

                    var countMismatchMessage = MappingAccordances.CreateDetailedFailureMessage(
                        MappingAccordances.ListValueCountMismatchMessage,
                        sourceExpression,
                        destinationExpression,
                        parentFailureMessage);

                    Assert.That(itemCount, Is.EqualTo(sourcePropertyValue.Count), countMismatchMessage);

                    for (var index = 0; index < itemCount; index++)
                    {
                        var sourceItem = sourcePropertyValue[index];
                        var destinationItem = destinationPropertyValue[index];

                        var itemMismatchMessage = MappingAccordances.CreateDetailedFailureMessage(
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
                @"The values are expected to be equal");

        /// <summary>
        ///     Registers all the properties, in source and destination types, that match by name and
        ///     property type.
        /// </summary>
        /// <param name="ignoreCase">
        ///     <c>true</c> if the property name comparison is case insensitive; <c>false</c> if the
        ///     property name comparison is case sensitive.
        /// </param>
        /// <returns>
        ///     This <see cref="MappingAccordances{TSource,TDestination}"/> instance.
        /// </returns>
        public MappingAccordances<TSource, TDestination> RegisterAllMatchingProperties(bool ignoreCase)
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var sourceProperties = MappingAccordances.GetSimpleReadableProperties(sourceType, ignoreCase);
            var destinationProperties = MappingAccordances.GetSimpleReadableProperties(destinationType, ignoreCase);

            var propertyNameComparer = MappingAccordances.GetPropertyNameComparer(ignoreCase);

            var matchingNames = sourceProperties
                .Keys
                .Intersect(destinationProperties.Keys, propertyNameComparer)
                .ToArray();

            foreach (var name in matchingNames)
            {
                var sourceProperty = sourceProperties[name];
                var destinationProperty = destinationProperties[name];
                var propertyType = sourceProperty.PropertyType;

                if (propertyType != destinationProperty.PropertyType)
                {
                    continue;
                }

                var sourcePropertySelectorExpression = MappingAccordances.CreatePropertyExpression(
                    sourceType,
                    sourceProperty,
                    "source");

                var destinationPropertySelectorExpression = MappingAccordances.CreatePropertyExpression(
                    destinationType,
                    destinationProperty,
                    "destination");

                var register = _registerMatchingPropertyMethodInfo.Value.MakeGenericMethod(propertyType);

                register.Invoke(
                    this,
                    new object[] { sourcePropertySelectorExpression, destinationPropertySelectorExpression });
            }

            return this;
        }

        /// <summary>
        ///     Registers all the properties, in source and destination types, that match by name and
        ///     property type. The property name comparison is case sensitive.
        /// </summary>
        /// <returns>
        ///     This <see cref="MappingAccordances{TSource,TDestination}"/> instance.
        /// </returns>
        public MappingAccordances<TSource, TDestination> RegisterAllMatchingProperties()
            => RegisterAllMatchingProperties(false);

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
    }
}