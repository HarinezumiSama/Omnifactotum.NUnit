using System;
using NUnit.Framework;

namespace Omnifactotum.NUnit.Tests
{
    [TestFixture]
    public sealed class MappingAccordancesTests
    {
        #region Tests

        [Test]
        public void TestSimpleScenario()
        {
            var innerMappingAccordances = MappingAccordances
                .From<SampleInnerSource>
                .To<SampleInnerDestination>()
                .RegisterNullReferenceCheck()
                .Register(source => source.Text, destination => destination.Message);

            Assert.That(innerMappingAccordances.IsNullReferenceCheckRegistered, Is.True);
            Assert.That(innerMappingAccordances.Count, Is.EqualTo(1));

            var mappingAccordances = MappingAccordances
                .From<SampleSource>
                .To<SampleDestination>()
                .Register(source => source.Name, destination => destination.FullName)
                .Register(source => source.Data, destination => destination.Info, innerMappingAccordances)
                .Register(
                    source => source.Progress,
                    destination => destination.Remaining,
                    expectedValue => Is.EqualTo(100 - expectedValue),
                    (sourceExpression, sourceValue, destinationExpression, destinationValue) =>
                        $@"The percentage values must complement each other: {sourceExpression} and {destinationExpression}.");

            Assert.That(mappingAccordances.IsNullReferenceCheckRegistered, Is.False);
            Assert.That(mappingAccordances.Count, Is.EqualTo(3));

            var sampleSource1 = new SampleSource
            {
                Name = "Job1",
                Progress = 45,
                Data = new SampleInnerSource { Text = "Hello World!" }
            };

            var sampleDestination1 = new SampleDestination
            {
                FullName = sampleSource1.Name,
                Remaining = 100 - sampleSource1.Progress,
                Info = new SampleInnerDestination { Message = sampleSource1.Data?.Text }
            };

            Assert.That(
                () => mappingAccordances.AssertAll(sampleSource1, null),
                Throws.TypeOf<NullReferenceException>());

            Assert.That(
                () => mappingAccordances.AssertAll(null, sampleDestination1),
                Throws.TypeOf<NullReferenceException>());

            Assert.That(
                () => mappingAccordances.AssertAll(null, null),
                Throws.TypeOf<NullReferenceException>());

            mappingAccordances.RegisterNullReferenceCheck();
            Assert.That(mappingAccordances.IsNullReferenceCheckRegistered, Is.True);

            Assert.That(
                () => mappingAccordances.AssertAll(sampleSource1, null),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .ContainsSubstring("Both the source and destination must be either null or non-null."));

            Assert.That(
                () => mappingAccordances.AssertAll(null, sampleDestination1),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .ContainsSubstring("Both the source and destination must be either null or non-null."));

            Assert.That(
                () => mappingAccordances.AssertAll(null, null),
                Throws.Nothing);

            Assert.That(() => mappingAccordances.AssertAll(sampleSource1, sampleDestination1), Throws.Nothing);

            var sampleSource2 = new SampleSource
            {
                Name = "Job1",
                Progress = 45,
                Data = null
            };

            var sampleDestination2 = new SampleDestination
            {
                FullName = sampleSource2.Name,
                Remaining = sampleSource2.Progress,
                Info = null
            };

            Assert.That(
                () => mappingAccordances.AssertAll(sampleSource2, sampleDestination2),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .ContainsSubstring("The percentage values must complement each other"));

            var sampleSource3 = new SampleSource
            {
                Name = "Job1",
                Progress = 35,
                Data = null
            };

            var sampleDestination3 = new SampleDestination
            {
                FullName = "System Job1",
                Remaining = 100 - sampleSource3.Progress,
                Info = null
            };

            Assert.That(
                () => mappingAccordances.AssertAll(sampleSource3, sampleDestination3),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .ContainsSubstring($".{nameof(SampleSource.Name)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .ContainsSubstring($".{nameof(SampleDestination.FullName)}"));

            var sampleSource4 = new SampleSource
            {
                Name = "Job1",
                Progress = 13,
                Data = new SampleInnerSource { Text = "Text" }
            };

            var sampleDestination4 = new SampleDestination
            {
                FullName = sampleSource4.Name,
                Remaining = 100 - sampleSource4.Progress,
                Info = new SampleInnerDestination { Message = "Message" }
            };

            Assert.That(
                () => mappingAccordances.AssertAll(sampleSource4, sampleDestination4),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .ContainsSubstring($".{nameof(SampleInnerSource.Text)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .ContainsSubstring($".{nameof(SampleInnerDestination.Message)}"));
        }

        [Test]
        public void TestAssertAllFailsWhenNoMapping()
        {
            var mappingAccordances = MappingAccordances.From<SampleSource>.To<SampleDestination>();

            Assert.That(
                () => mappingAccordances.AssertAll(null, null),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .ContainsSubstring(MappingAccordances.NoMappingsMessage));

            mappingAccordances.RegisterNullReferenceCheck();

            Assert.That(
                () => mappingAccordances.AssertAll(null, null),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .ContainsSubstring(MappingAccordances.NoMappingsMessage));
        }

        #endregion

        #region SampleSource Class

        private sealed class SampleSource
        {
            public string Name
            {
                get;
                set;
            }

            public int Progress
            {
                get;
                set;
            }

            public SampleInnerSource Data
            {
                get;
                set;
            }
        }

        #endregion

        #region SampleInnerSource Class

        private sealed class SampleInnerSource
        {
            public string Text
            {
                get;
                set;
            }
        }

        #endregion

        #region SampleDestination Class

        private sealed class SampleDestination
        {
            public string FullName
            {
                get;
                set;
            }

            public int Remaining
            {
                get;
                set;
            }

            public SampleInnerDestination Info
            {
                get;
                set;
            }
        }

        #endregion

        #region SampleInnerDestination Class

        private sealed class SampleInnerDestination
        {
            public string Message
            {
                get;
                set;
            }
        }

        #endregion
    }
}