#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Omnifactotum.Annotations;
using static Omnifactotum.NUnit.AssertEqualityExpectation;
using static Omnifactotum.NUnit.AssertEqualityOperatorExpectation;

namespace Omnifactotum.NUnit.Tests
{
    [TestFixture(TestOf = typeof(NUnitFactotum))]
    internal sealed class NUnitFactotumTests
    {
        [Test]
        public void TestAssertNotNullWhenReferenceTypeAndNullThenThrowsAssertionException()
        {
            TestSingleCase(default(object));
            TestSingleCase(default(Type));
            TestSingleCase(default(string));

            static void TestSingleCase<T>(T? obj)
                where T : class
                => Assert.That(() => obj.AssertNotNull(), Throws.TypeOf<AssertionException>());
        }

        [Test]
        public void TestAssertNotNullWhenReferenceTypeAndNotNullThenSucceeds()
        {
            TestSingleCase(new object());

            TestSingleCase(GetType());

            TestSingleCase(new string('a', 42));
            TestSingleCase(string.Empty);
            TestSingleCase("TestData-5f9dcb84e9a54ac6b2ec54764fc24e6d");

            static void TestSingleCase<T>(T? obj)
                where T : class
                => Assert.That(() => obj.AssertNotNull(), Is.SameAs(obj));
        }

        [Test]
        public void TestAssertNotNullWhenValueTypeAndNullThenThrowsAssertionException()
        {
            TestSingleCase(default(int?));
            TestSingleCase(default(double?));
            TestSingleCase(default(DateTime?));

            static void TestSingleCase<T>(T? obj)
                where T : struct
                => Assert.That(() => obj.AssertNotNull(), Throws.TypeOf<AssertionException>());
        }

        [Test]
        public void TestAssertNotNullWhenValueTypeAndNotNullThenSucceeds()
        {
            TestSingleCase<int>(42);
            TestSingleCase<double>(1.5d);
            TestSingleCase<DateTime>(new DateTime(2021, 09, 27, 08, 34, 13));

            static void TestSingleCase<T>(T? obj)
                where T : struct
                => Assert.That(() => obj.AssertNotNull(), Is.EqualTo(obj));
        }

        [Test]
        public void TestAssertReadableWritableWhenInvalidArgumentsThenThrows()
        {
            TestSingleCase(
                null,
                PropertyAccessMode.ReadWrite,
                MethodAttributes.Public,
                CreateAssertionExceptionMessage(
                    @"The property expression cannot be null.",
                    @"Expected: not null",
                    @"But was:  null"));

            TestSingleCase(
                obj => obj.PublicReadPublicWrite,
                (PropertyAccessMode)int.MaxValue,
                MethodAttributes.Public,
                CreateAssertionExceptionMessage(
                    @"Invalid expected access mode.",
                    @"Expected: True",
                    @"But was:  False"));

            TestSingleCase(
                obj => obj.PublicReadPublicWrite,
                PropertyAccessMode.ReadOnly,
                MethodAttributes.Static,
                CreateAssertionExceptionMessage(
                    $@"Invalid accessor visibility. Must match the ""{nameof(MethodAttributes)}.{
                        nameof(MethodAttributes.MemberAccessMask)}"" mask.",
                    @"Expected: 0",
                    @"But was:  16"));

            static void TestSingleCase(
                Expression<Func<AssertReadableWritableTestClass, int>>? propertyGetterExpression,
                PropertyAccessMode expectedAccessMode,
                MethodAttributes visibleAccessorAttribute,
                string expectedAssertionExceptionMessage)
            {
                Assert.That(
                    () =>
                        NUnitFactotum.AssertReadableWritable(
                            propertyGetterExpression!,
                            expectedAccessMode,
                            visibleAccessorAttribute),
                    CreateConstraint());

                Assert.That(
                    () =>
                        NUnitFactotum.For<AssertReadableWritableTestClass>.AssertReadableWritable(
                            propertyGetterExpression!,
                            expectedAccessMode,
                            visibleAccessorAttribute),
                    CreateConstraint());

                IResolveConstraint CreateConstraint()
                    => Throws.TypeOf<AssertionException>().With.Message.EqualTo(expectedAssertionExceptionMessage);
            }
        }

        [Test]
        public void TestAssertReadableWritableWhenValidArgumentsThenBehavesProperly()
        {
            var testClassFullName = typeof(AssertReadableWritableTestClass).GetFullName();

            TestSingleCase(obj => obj.PublicReadPublicWrite, PropertyAccessMode.ReadWrite, MethodAttributes.Public);
            TestSingleCase(obj => obj.PublicReadPrivateWrite, PropertyAccessMode.ReadOnly, MethodAttributes.Public);
            TestSingleCase(obj => obj.InternalReadPublicWrite, PropertyAccessMode.WriteOnly, MethodAttributes.Public);
            TestSingleCase(obj => obj.InternalReadPrivateWrite, PropertyAccessMode.ReadOnly, MethodAttributes.Assembly);

            TestSingleCase(
                obj => obj.PublicReadPublicWrite,
                PropertyAccessMode.ReadOnly,
                MethodAttributes.Public,
                $@"The property ""{testClassFullName}.{
                    nameof(AssertReadableWritableTestClass.PublicReadPublicWrite)}"" MUST NOT be writable (visibility: Public).",
                @"Expected: False",
                @"But was:  True");

            TestSingleCase(
                obj => obj.PublicReadPublicWrite,
                PropertyAccessMode.WriteOnly,
                MethodAttributes.Public,
                $@"The property ""{testClassFullName}.{
                    nameof(AssertReadableWritableTestClass.PublicReadPublicWrite)}"" MUST NOT be readable (visibility: Public).",
                @"Expected: False",
                @"But was:  True");

            TestSingleCase(
                obj => obj.PublicReadPrivateWrite,
                PropertyAccessMode.ReadWrite,
                MethodAttributes.Public,
                $@"The property ""{testClassFullName}.{
                    nameof(AssertReadableWritableTestClass.PublicReadPrivateWrite)}"" MUST be writable (visibility: Public).",
                @"Expected: True",
                @"But was:  False");

            TestSingleCase(
                obj => obj.PublicReadPrivateWrite,
                PropertyAccessMode.WriteOnly,
                MethodAttributes.Public,
                $@"The property ""{testClassFullName}.{
                    nameof(AssertReadableWritableTestClass.PublicReadPrivateWrite)}"" MUST NOT be readable (visibility: Public).",
                @"Expected: False",
                @"But was:  True");

            TestSingleCase(
                obj => obj.InternalReadPublicWrite,
                PropertyAccessMode.ReadOnly,
                MethodAttributes.Public,
                $@"The property ""{testClassFullName}.{
                    nameof(AssertReadableWritableTestClass.InternalReadPublicWrite)}"" MUST be readable (visibility: Public).",
                @"Expected: True",
                @"But was:  False");

            TestSingleCase(
                obj => obj.InternalReadPublicWrite,
                PropertyAccessMode.ReadWrite,
                MethodAttributes.Public,
                $@"The property ""{testClassFullName}.{
                    nameof(AssertReadableWritableTestClass.InternalReadPublicWrite)}"" MUST be readable (visibility: Public).",
                @"Expected: True",
                @"But was:  False");

            TestSingleCase(
                obj => obj.InternalReadPrivateWrite,
                PropertyAccessMode.ReadWrite,
                MethodAttributes.Assembly,
                $@"The property ""{testClassFullName}.{
                    nameof(AssertReadableWritableTestClass.InternalReadPrivateWrite)}"" MUST be writable (visibility: Assembly).",
                @"Expected: True",
                @"But was:  False");

            TestSingleCase(
                obj => obj.InternalReadPrivateWrite,
                PropertyAccessMode.WriteOnly,
                MethodAttributes.Assembly,
                $@"The property ""{testClassFullName}.{
                    nameof(AssertReadableWritableTestClass.InternalReadPrivateWrite)}"" MUST NOT be readable (visibility: Assembly).",
                @"Expected: False",
                @"But was:  True");

            static void TestSingleCase(
                Expression<Func<AssertReadableWritableTestClass, int>> propertyGetterExpression,
                PropertyAccessMode expectedAccessMode,
                MethodAttributes visibleAccessorAttribute,
                params string[] expectedAssertionExceptionMessageParts)
            {
                Func<IResolveConstraint> createConstraint = expectedAssertionExceptionMessageParts.Length == 0
                    ? () => Throws.Nothing
                    : () => Throws
                        .TypeOf<AssertionException>()
                        .With
                        .Message
                        .EqualTo(CreateAssertionExceptionMessage(expectedAssertionExceptionMessageParts));

                Assert.That(
                    () =>
                        NUnitFactotum.AssertReadableWritable(
                            propertyGetterExpression,
                            expectedAccessMode,
                            visibleAccessorAttribute),
                    createConstraint());

                Assert.That(
                    () =>
                        NUnitFactotum.For<AssertReadableWritableTestClass>.AssertReadableWritable(
                            propertyGetterExpression,
                            expectedAccessMode,
                            visibleAccessorAttribute),
                    createConstraint());
            }
        }

        [Test]
        public void TestAssertEqualityWhenVariousCasesThenBehavesProperly()
        {
            InternalTestAssertEqualityWhenVariousCasesThenBehavesProperly(
                hashCode => new AssertEqualityNoOperatorsTestClass(hashCode),
                false);

            InternalTestAssertEqualityWhenVariousCasesThenBehavesProperly(
                hashCode => new AssertEqualityWithOperatorsTestClass(hashCode),
                true);
        }

        [Test]
        public void TestGenerateCombinatorialTestCasesWhenArgumentsParametersIsNullThenThrowsAssertionException()
            => InternalTestGenerateCombinatorialTestCasesWhenInvalidArgumentsParametersThenThrowsAssertionException(
                null,
                "null");

        [Test]
        public void TestGenerateCombinatorialTestCasesWhenArgumentsParametersIsEmptyThenThrowsAssertionException()
            => InternalTestGenerateCombinatorialTestCasesWhenInvalidArgumentsParametersThenThrowsAssertionException(
                new object?[0],
                "<empty>");

        [Test]
        public void TestGenerateCombinatorialTestCasesWhenValidArgumentsThenSucceeds(
            [Values(null, false, true)] bool? passProcessTestCaseDelegate)
        {
            const string TestIndexFormat = "000";

            const int IntValue1 = 17;
            const int IntValue2 = 42;

            const string StringValue1 = "a";
            const string StringValue2 = "b";

            bool? nullableBooleanValue1 = false;
            bool? nullableBooleanValue2 = true;
            bool? nullableBooleanValue3 = null;

            const char CharValue = '@';

            var intValues = new[] { IntValue1, IntValue2 };
            var stringValues = new[] { StringValue1, StringValue2 };
            var nullableBooleanValues = new[] { nullableBooleanValue1, nullableBooleanValue2, nullableBooleanValue3 };

            Assert.That(intValues, Is.Unique);
            Assert.That(stringValues, Is.Unique);
            Assert.That(nullableBooleanValues, Is.Unique);

            var methodArguments = new object?[] { intValues, stringValues, nullableBooleanValues, CharValue };

            var testIndex = 0;

            var generatedTestCases =
                passProcessTestCaseDelegate.HasValue
                    ? NUnitFactotum.GenerateCombinatorialTestCases(
                        passProcessTestCaseDelegate.Value
                            ? data => data.SetName((testIndex++).ToString(TestIndexFormat))
                            : null,
                        methodArguments)
                    : NUnitFactotum.GenerateCombinatorialTestCases(methodArguments);

            Assert.That(generatedTestCases, Is.Not.Null);
            Assert.That(generatedTestCases, Has.Count.EqualTo(intValues.Length * stringValues.Length * nullableBooleanValues.Length));

            void AssertTestCaseProperties(int generatedTestCaseIndex, int intValue, string stringValue, bool? nullableBooleanValue)
            {
                Assert.That(
                    generatedTestCases[generatedTestCaseIndex],
                    Has.Property(nameof(TestCaseData.Arguments))
                        .EqualTo(
                            new object?[]
                            {
                                intValue, stringValue, nullableBooleanValue,
                                CharValue
                            }));

                var expectedTestName = passProcessTestCaseDelegate.GetValueOrDefault()
                    ? generatedTestCaseIndex.ToString(TestIndexFormat)
                    : null;

                Assert.That(
                    generatedTestCases[generatedTestCaseIndex],
                    Has.Property(nameof(TestCaseData.TestName)).EqualTo(expectedTestName));
            }

            AssertTestCaseProperties(0, intValues[0], stringValues[0], nullableBooleanValues[0]);
            AssertTestCaseProperties(1, intValues[1], stringValues[0], nullableBooleanValues[0]);
            AssertTestCaseProperties(2, intValues[0], stringValues[1], nullableBooleanValues[0]);
            AssertTestCaseProperties(3, intValues[1], stringValues[1], nullableBooleanValues[0]);
            AssertTestCaseProperties(4, intValues[0], stringValues[0], nullableBooleanValues[1]);
            AssertTestCaseProperties(5, intValues[1], stringValues[0], nullableBooleanValues[1]);
            AssertTestCaseProperties(6, intValues[0], stringValues[1], nullableBooleanValues[1]);
            AssertTestCaseProperties(7, intValues[1], stringValues[1], nullableBooleanValues[1]);
            AssertTestCaseProperties(8, intValues[0], stringValues[0], nullableBooleanValues[2]);
            AssertTestCaseProperties(9, intValues[1], stringValues[0], nullableBooleanValues[2]);
            AssertTestCaseProperties(10, intValues[0], stringValues[1], nullableBooleanValues[2]);
            AssertTestCaseProperties(11, intValues[1], stringValues[1], nullableBooleanValues[2]);
        }

        [Test]
        public void TestAssertCast()
        {
            AssertCastTestClassBase instance1 = new AssertCastTestClass1();

            Assert.That(
                () => instance1.AssertCast().To<AssertCastTestClass1>(),
                Is.SameAs(instance1));

            Assert.That(
                () => instance1.AssertCast().To<AssertCastTestClass2>(),
                Throws
                    .TypeOf<AssertionException>()
                    .With
                    .Message
                    .EqualTo(
                        CreateAssertionExceptionMessage(
                            $"Expected: instance of <{typeof(AssertCastTestClass2).FullName}>",
                            $"But was:  <{typeof(AssertCastTestClass1).FullName}>")));
        }

        private static void InternalTestAssertEqualityWhenVariousCasesThenBehavesProperly<TTestee>(
            Func<int, TTestee> createTestee,
            bool hasEqualityOperators)
            where TTestee : AssertEqualityTestClassBase
        {
            const int HashCode1 = 42;
            const int HashCode2 = 142;

            Assert.That(new[] { HashCode1, HashCode2 }, Is.Unique);

            var value1A = createTestee(HashCode1);
            var value1B = createTestee(HashCode1);
            var value1WrongHashCode = createTestee(HashCode2);
            var value2 = createTestee(HashCode1); // The same hash code, but not equal

            bool OnEquals(object? obj1, object? obj2)
                => IsSameAsOneOf(obj1, new[] { value1A, value1B, value1WrongHashCode })
                    && IsSameAsOneOf(obj2, new[] { value1A, value1B, value1WrongHashCode });

            value1A.OnEquals = OnEquals;
            value1B.OnEquals = OnEquals;
            value1WrongHashCode.OnEquals = OnEquals;
            value2.OnEquals = OnEquals;

            var positiveOperatorExpectation = hasEqualityOperators ? MustDefine : MustNotDefine;

            TestSingleCase(
                null,
                null,
                EqualAndCannotBeSame,
                () => Throws.Nothing,
                MayDefine,
                positiveOperatorExpectation);

            TestSingleCase(value1A, value1A, EqualAndMayBeSame, () => Throws.Nothing, MayDefine, positiveOperatorExpectation);
            TestSingleCase(value1B, value1B, EqualAndMayBeSame, () => Throws.Nothing, MayDefine, positiveOperatorExpectation);

            TestSingleCase(value1A, value1B, EqualAndCannotBeSame, () => Throws.Nothing, MayDefine, positiveOperatorExpectation);

            var negativeOperatorExpectation = hasEqualityOperators ? MustNotDefine : MustDefine;

            var negativeOperatorExpectationAssertionExceptionMessage = hasEqualityOperators
                ? CreateAssertionExceptionMessage(
                    $@"Equality operator (==) must not be defined for the type ""{value1A.GetType().GetFullName()}"".",
                    @"Expected: null",
                    $@"But was:  <{nameof(Boolean)} op_Equality({nameof(AssertEqualityWithOperatorsTestClass)}, {
                        nameof(AssertEqualityWithOperatorsTestClass)})>")
                : CreateAssertionExceptionMessage(
                    $@"Equality operator (==) must be defined for the type ""{value1A.GetType().GetFullName()}"".",
                    @"Expected: not null",
                    @"But was:  null");

            TestSingleCase(
                value1A,
                value1B,
                EqualAndCannotBeSame,
                () => Throws
                    .TypeOf<AssertionException>()
                    .With
                    .Message
                    .EqualTo(negativeOperatorExpectationAssertionExceptionMessage),
                negativeOperatorExpectation);

            TestSingleCase(
                value1A,
                value1WrongHashCode,
                EqualAndCannotBeSame,
                () => Throws
                    .TypeOf<AssertionException>()
                    .With
                    .Message
                    .EqualTo(
                        CreateAssertionExceptionMessage(
                            @"When the values are equal, their hash codes must also be equal.",
                            $@"Expected: {HashCode1}",
                            $@"But was:  {HashCode2}")),
                MayDefine,
                positiveOperatorExpectation);

            TestSingleCase(
                value1A,
                value2,
                EqualAndCannotBeSame,
                () => Throws
                    .TypeOf<AssertionException>()
                    .With
                    .Message
                    .EqualTo(
                        CreateAssertionExceptionMessage(
                            @"Expected: True",
                            @"But was:  False")),
                MayDefine,
                positiveOperatorExpectation);

            static void TestSingleCase(
                TTestee? value1,
                TTestee? value2,
                AssertEqualityExpectation equalityExpectation,
                Func<IResolveConstraint> createConstraint,
                params AssertEqualityOperatorExpectation[] operatorExpectations)
            {
                Assert.That(createConstraint, Is.Not.Null);
                Assert.That(operatorExpectations, Is.Not.Null.And.Not.Empty);

                foreach (var operatorExpectation in operatorExpectations)
                {
                    Assert.That(
                        () => NUnitFactotum.AssertEquality(value1, value2, equalityExpectation, operatorExpectation),
                        createConstraint());
                }
            }
        }

        private static void InternalTestGenerateCombinatorialTestCasesWhenInvalidArgumentsParametersThenThrowsAssertionException(
            object?[]? methodArguments,
            string actualValueInAssertionMessage)
        {
            IResolveConstraint CreateConstraint()
                => Throws
                    .TypeOf<AssertionException>()
                    .With
                    .Message
                    .EqualTo(
                        CreateAssertionExceptionMessage(
                            "Expected: not null and not <empty>",
                            $"But was:  {actualValueInAssertionMessage}"));

            Assert.That(
                () => NUnitFactotum.GenerateCombinatorialTestCases(methodArguments!),
                CreateConstraint());

            Assert.That(
                () => NUnitFactotum.GenerateCombinatorialTestCases(null, methodArguments!),
                CreateConstraint());

            Assert.That(
                () => NUnitFactotum.GenerateCombinatorialTestCases(ProcessTestCase, methodArguments!),
                CreateConstraint());

            static void ProcessTestCase(TestCaseData data) => throw new NotSupportedException("Not supposed to be called ever.");
        }

        private static string CreateAssertionExceptionMessage(params string[] assertionExceptionMessageParts)
            => assertionExceptionMessageParts.Select(s => $"\x0020\x0020{s}{Environment.NewLine}").Join(string.Empty);

        private static bool IsSameAsOneOf<T>(object? referenceValue, T?[] otherValues)
            where T : class
        {
            Assert.That(otherValues, Is.Not.Null);
            return otherValues.Any(item => ReferenceEquals(referenceValue, item));
        }

        private sealed class AssertReadableWritableTestClass
        {
            [UsedImplicitly]
            public int PublicReadPublicWrite { get; set; }

            [UsedImplicitly]
            public int PublicReadPrivateWrite { get; private set; }

            [UsedImplicitly]
            public int InternalReadPublicWrite { internal get; set; }

            [UsedImplicitly]
            internal int InternalReadPrivateWrite { get; private set; }
        }

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private abstract class AssertEqualityTestClassBase
        {
            protected AssertEqualityTestClassBase(int hashCode) => HashCode = hashCode;

            public Func<object?, object?, bool> OnEquals { get; set; } = (_, _) => throw new NotSupportedException();

            private int HashCode { get; }

            public static bool operator ==(AssertEqualityTestClassBase? left, AssertEqualityTestClassBase? right)
                => throw new NotSupportedException();

            public static bool operator !=(AssertEqualityTestClassBase? left, AssertEqualityTestClassBase? right)
                => throw new NotSupportedException();

            public override string ToString()
                => $@"{{ {GetType().Name}#{RuntimeHelpers.GetHashCode(this):X8}: {nameof(HashCode)} = {HashCode} }}";

            public override bool Equals(object? obj) => OnEquals(this, obj);

            public override int GetHashCode() => HashCode;
        }

        private sealed class AssertEqualityNoOperatorsTestClass : AssertEqualityTestClassBase
        {
            public AssertEqualityNoOperatorsTestClass(int hashCode)
                : base(hashCode)
            {
                // Nothing to do
            }
        }

        private sealed class AssertEqualityWithOperatorsTestClass : AssertEqualityTestClassBase
        {
            public AssertEqualityWithOperatorsTestClass(int hashCode)
                : base(hashCode)
            {
                // Nothing to do
            }

            public static bool operator ==(AssertEqualityWithOperatorsTestClass? left, AssertEqualityWithOperatorsTestClass? right)
                => Equals(left, right);

            public static bool operator !=(AssertEqualityWithOperatorsTestClass? left, AssertEqualityWithOperatorsTestClass? right)
                => !Equals(left, right);

            //// ReSharper disable once RedundantOverriddenMember
            public override bool Equals(object? obj) => base.Equals(obj);

            public override int GetHashCode() => base.GetHashCode();

            private static bool Equals(AssertEqualityWithOperatorsTestClass? left, AssertEqualityWithOperatorsTestClass? right)
            {
                if (ReferenceEquals(left, right))
                {
                    return true;
                }

                if (left is null || right is null)
                {
                    return false;
                }

                return left.Equals(right);
            }
        }

        private abstract class AssertCastTestClassBase
        {
        }

        private sealed class AssertCastTestClass1 : AssertCastTestClassBase
        {
        }

        private sealed class AssertCastTestClass2 : AssertCastTestClassBase
        {
        }
    }
}