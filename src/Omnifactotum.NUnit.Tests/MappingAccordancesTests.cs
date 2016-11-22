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
            var mappingAccordances = MappingAccordances
                .From<SampleSource>
                .To<SampleDestination>()
                .Register(source => source.Name, destination => destination.FullName)
                .Register(
                    source => source.Progress,
                    destination => destination.Remaining,
                    expectedValue => Is.EqualTo(100 - expectedValue),
                    (sourceExpression, sourceValue, destinationExpression, destinationValue) =>
                        $@"The percentage values must complement each other: {sourceExpression} and {destinationExpression}.");

            Assert.That(mappingAccordances.Count, Is.EqualTo(2));

            var sampleSource1 = new SampleSource
            {
                Name = "Job1",
                Progress = 45
            };

            var sampleDestination1 = new SampleDestination
            {
                FullName = sampleSource1.Name,
                Remaining = 100 - sampleSource1.Progress
            };

            Assert.That(() => mappingAccordances.AssertAll(sampleSource1, sampleDestination1), Throws.Nothing);

            var sampleSource2 = new SampleSource
            {
                Name = "Job1",
                Progress = 45
            };

            var sampleDestination2 = new SampleDestination { FullName = "Job1", Remaining = sampleSource2.Progress };

            Assert.That(
                () => mappingAccordances.AssertAll(sampleSource2, sampleDestination2),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .ContainsSubstring("The percentage values must complement each other"));

            var sampleSource3 = new SampleSource
            {
                Name = "Job1",
                Progress = 35
            };

            var sampleDestination3 = new SampleDestination
            {
                FullName = "System Job1",
                Remaining = 100 - sampleSource3.Progress
            };

            Assert.That(
                () => mappingAccordances.AssertAll(sampleSource3, sampleDestination3),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .ContainsSubstring($".{nameof(SampleDestination.FullName)}"));
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
        }

        #endregion
    }
}