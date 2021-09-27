using System;
using NUnit.Framework;
using Omnifactotum.Annotations;

namespace Omnifactotum.NUnit.Tests
{
    [TestFixture]
    public sealed class MappingAccordancesTests
    {
        #region Constants and Fields

        private const string PercentageValuesMismatchMessage = @"The percentage values must complement each other";

        #endregion

        #region Tests

        [Test]
        public void TestAssertAllAgainstNullReferenceCheck()
        {
            const string NullMismatchMessage = @"Both the source and destination must be either null or non-null.";

            var testee = CreateTestee();

            Assert.That(
                () => testee.AssertAll(new SampleSource(), null),
                Throws.TypeOf<NullReferenceException>());

            Assert.That(
                () => testee.AssertAll(null, new SampleDestination()),
                Throws.TypeOf<NullReferenceException>());

            Assert.That(
                () => testee.AssertAll(null, null),
                Throws.TypeOf<NullReferenceException>());

            testee.RegisterNullReferenceCheck();
            Assert.That(testee.IsNullReferenceCheckRegistered, Is.True);

            Assert.That(
                () => testee.AssertAll(new SampleSource(), null),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains(NullMismatchMessage));

            Assert.That(
                () => testee.AssertAll(null, new SampleDestination()),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains(NullMismatchMessage));

            Assert.That(() => testee.AssertAll(null, null), Throws.Nothing);
        }

        [Test]
        public void TestAssertAllWhenObjectsMatch()
        {
            var testee = CreateTestee();

            var source = new SampleSource
            {
                Name = "Job1",
                Progress = 1,
                Data = new SampleInnerSource { Text = "Hello World!" },
                Items = new[] { new SampleSourceItem { Text = "Item text" } }
            };

            var destination = new SampleDestination
            {
                FullName = source.Name,
                Remaining = 100 - source.Progress,
                Info = new SampleInnerDestination { Message = source.Data.Text },
                Datas = new[] { new SampleDestinationItem { TextData = source.Items[0].Text } }
            };

            Assert.That(() => testee.AssertAll(source, destination), Throws.Nothing);
        }

        [Test]
        public void TestAssertAllWhenPropertyMismatchesCustomConstraint()
        {
            var testee = CreateTestee();

            var source = new SampleSource
            {
                Name = "Job2",
                Progress = 2,
                Data = null,
                Items = null
            };

            var destination = new SampleDestination
            {
                FullName = source.Name,
                Remaining = source.Progress,
                Info = null,
                Datas = null
            };

            Assert.That(
                () => testee.AssertAll(source, destination),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains(PercentageValuesMismatchMessage));
        }

        [Test]
        public void TestAssertAllWhenRegisteredWithConstantMessageAndPropertyMismatchesCustomConstraint()
        {
            const string RemainingPropertyMismatchMessage = @"The remaining value must be the negative progress.";

            var testee = MappingAccordances
                .From<SampleSource>
                .To<SampleDestination>()
                .Register(
                    source => source.Progress,
                    destination => destination.Remaining,
                    expectedValue => Is.EqualTo(-expectedValue),
                    RemainingPropertyMismatchMessage);

            var sampleSource = new SampleSource
            {
                Progress = 17
            };

            var sampleDestination = new SampleDestination
            {
                Remaining = sampleSource.Progress
            };

            Assert.That(
                () => testee.AssertAll(sampleSource, sampleDestination),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains(RemainingPropertyMismatchMessage));
        }

        [Test]
        public void TestAssertAllWhenPropertyValueIsNotEqual()
        {
            var testee = CreateTestee();

            var source = new SampleSource
            {
                Name = "Job3",
                Progress = 3,
                Data = null,
                Items = null
            };

            var destination = new SampleDestination
            {
                FullName = "System" + source.Name,
                Remaining = 100 - source.Progress,
                Info = null,
                Datas = null
            };

            Assert.That(
                () => testee.AssertAll(source, destination),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleSource.Name)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleDestination.FullName)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains("The values are expected to be equal:"));
        }

        [Test]
        public void TestAssertAllWhenInnerComplexPropertyMismatches()
        {
            var testee = CreateTestee();

            var source = new SampleSource
            {
                Name = "Job4",
                Progress = 4,
                Data = new SampleInnerSource { Text = "Text" },
                Items = null
            };

            var destination = new SampleDestination
            {
                FullName = source.Name,
                Remaining = 100 - source.Progress,
                Info = new SampleInnerDestination { Message = "Message" },
                Datas = null
            };

            Assert.That(
                () => testee.AssertAll(source, destination),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleInnerSource.Text)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleInnerDestination.Message)}"));
        }

        [Test]
        public void TestAssertAllWhenInnerListPropertyMismatchesByNullReferenceCheck()
        {
            const string ExpectedMessagePart =
                "Both the source and destination values must be either null or non-null";

            var testee = CreateTestee();

            var source1 = new SampleSource
            {
                Name = "Job6A",
                Progress = 6,
                Data = null,
                Items = new[] { new SampleSourceItem { Text = "Text6" } }
            };

            var destination1 = new SampleDestination
            {
                FullName = source1.Name,
                Remaining = 100 - source1.Progress,
                Info = null,
                Datas = null
            };

            Assert.That(
                () => testee.AssertAll(source1, destination1),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleSource.Items)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleDestination.Datas)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains(ExpectedMessagePart));

            var source2 = new SampleSource
            {
                Name = "Job6B",
                Progress = 6,
                Data = null,
                Items = null
            };

            var destination2 = new SampleDestination
            {
                FullName = source2.Name,
                Remaining = 100 - source2.Progress,
                Info = null,
                Datas = new[] { new SampleDestinationItem { TextData = "Text6" } }
            };

            Assert.That(
                () => testee.AssertAll(source2, destination2),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleSource.Items)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleDestination.Datas)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains(ExpectedMessagePart));
        }

        [Test]
        public void TestAssertAllWhenInnerListPropertyMismatchesByCount()
        {
            var testee = CreateTestee();

            var source = new SampleSource
            {
                Name = "Job6",
                Progress = 6,
                Data = null,
                Items =
                    new[]
                    {
                        new SampleSourceItem { Text = "ItemText5" },
                        new SampleSourceItem { Text = "ItemText5-A" }
                    }
            };

            var destination = new SampleDestination
            {
                FullName = source.Name,
                Remaining = 100 - source.Progress,
                Info = null,
                Datas = new[] { new SampleDestinationItem { TextData = source.Items[0].Text } }
            };

            Assert.That(
                () => testee.AssertAll(source, destination),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleSource.Items)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleDestination.Datas)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains("The source and destination must have the same item count"));
        }

        [Test]
        public void TestAssertAllWhenInnerListPropertyMismatchesByItem()
        {
            var testee = CreateTestee();

            var source = new SampleSource
            {
                Name = "Job5",
                Progress = 5,
                Data = null,
                Items = new[] { new SampleSourceItem { Text = "ItemText5" } }
            };

            var destination = new SampleDestination
            {
                FullName = source.Name,
                Remaining = 100 - source.Progress,
                Info = null,
                Datas = new[] { new SampleDestinationItem { TextData = "TextData5" } }
            };

            Assert.That(
                () => testee.AssertAll(source, destination),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleSource.Items)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleDestination.Datas)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleSourceItem.Text)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains($".{nameof(SampleDestinationItem.TextData)}")
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains("The source and destination must have the matching item at index 0"));
        }

        [Test]
        public void TestAssertAllFailsWhenNoMapping()
        {
            const string NoMappingsMessage = @"There must be at least one registered mapping to assert.";

            var testee = MappingAccordances.From<SampleSource>.To<SampleDestination>();

            Assert.That(
                () => testee.AssertAll(null, null),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains(NoMappingsMessage));

            testee.RegisterNullReferenceCheck();

            Assert.That(
                () => testee.AssertAll(null, null),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains(NoMappingsMessage));
        }

        [Test]
        public void TestAssertAllWithRegisterAllMatchingProperties()
        {
            var testeeImplicitCaseSensitive =
                MappingAccordances.From<SampleSource>.To<SampleDestination>().RegisterAllMatchingProperties();

            var testeeExplicitCaseSensitive =
                MappingAccordances.From<SampleSource>.To<SampleDestination>().RegisterAllMatchingProperties(false);

            var testeeCaseInsensitive =
                MappingAccordances.From<SampleSource>.To<SampleDestination>().RegisterAllMatchingProperties(true);

            Assert.That(testeeImplicitCaseSensitive.Count, Is.EqualTo(1));
            Assert.That(testeeExplicitCaseSensitive.Count, Is.EqualTo(1));
            Assert.That(testeeCaseInsensitive.Count, Is.EqualTo(2));

            var sampleSource = new SampleSource
            {
                Name = "John",
                MatchingName = "matching",
                MatchingTextCaseInsensitive = "Text"
            };

            var sampleDestination1 = new SampleDestination
            {
                FullName = "FullName",
                MatchingName = sampleSource.MatchingName,
                matchingtextcaseinsensitive = sampleSource.MatchingTextCaseInsensitive
            };

            Assert.That(() => testeeImplicitCaseSensitive.AssertAll(sampleSource, sampleDestination1), Throws.Nothing);
            Assert.That(() => testeeExplicitCaseSensitive.AssertAll(sampleSource, sampleDestination1), Throws.Nothing);
            Assert.That(() => testeeCaseInsensitive.AssertAll(sampleSource, sampleDestination1), Throws.Nothing);

            var sampleDestination2 = new SampleDestination
            {
                FullName = sampleSource.Name,
                MatchingName = "no match",
                matchingtextcaseinsensitive = sampleSource.MatchingTextCaseInsensitive
            };

            const string SourcePrefix = "source.";
            const string DestinationPrefix = "destination.";

            Assert.That(
                () => testeeImplicitCaseSensitive.AssertAll(sampleSource, sampleDestination2),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains(SourcePrefix + nameof(SampleSource.MatchingName))
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains(DestinationPrefix + nameof(SampleDestination.MatchingName)));

            Assert.That(
                () => testeeExplicitCaseSensitive.AssertAll(sampleSource, sampleDestination2),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains(SourcePrefix + nameof(SampleSource.MatchingName))
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains(DestinationPrefix + nameof(SampleDestination.MatchingName)));

            Assert.That(
                () => testeeCaseInsensitive.AssertAll(sampleSource, sampleDestination2),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains(SourcePrefix + nameof(SampleSource.MatchingName))
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains(DestinationPrefix + nameof(SampleDestination.MatchingName)));

            var sampleDestination3 = new SampleDestination
            {
                FullName = sampleSource.Name,
                MatchingName = sampleSource.MatchingName,
                matchingtextcaseinsensitive = "nothing"
            };

            Assert.That(() => testeeImplicitCaseSensitive.AssertAll(sampleSource, sampleDestination3), Throws.Nothing);
            Assert.That(() => testeeExplicitCaseSensitive.AssertAll(sampleSource, sampleDestination3), Throws.Nothing);

            Assert.That(
                () => testeeCaseInsensitive.AssertAll(sampleSource, sampleDestination3),
                Throws.TypeOf<AssertionException>()
                    .With
                    .Property(nameof(AssertionException.Message))
                    .Contains(SourcePrefix + nameof(SampleSource.MatchingTextCaseInsensitive))
                    .And
                    .Property(nameof(AssertionException.Message))
                    .Contains(DestinationPrefix + nameof(SampleDestination.matchingtextcaseinsensitive)));
        }

        #endregion

        #region Private Methods

        private static MappingAccordances<SampleSource, SampleDestination> CreateTestee()
        {
            var innerMappingAccordances = MappingAccordances
                .From<SampleInnerSource>
                .To<SampleInnerDestination>()
                .RegisterNullReferenceCheck()
                .Register(source => source.Text, destination => destination.Message);

            Assert.That(innerMappingAccordances.IsNullReferenceCheckRegistered, Is.True);
            Assert.That(innerMappingAccordances.Count, Is.EqualTo(1));

            var innerItemMappingAccordances = MappingAccordances
                .From<SampleSourceItem>
                .To<SampleDestinationItem>()
                .RegisterNullReferenceCheck()
                .Register(source => source.Text, destination => destination.TextData);

            Assert.That(innerMappingAccordances.IsNullReferenceCheckRegistered, Is.True);
            Assert.That(innerMappingAccordances.Count, Is.EqualTo(1));

            var testee = MappingAccordances
                .From<SampleSource>
                .To<SampleDestination>()
                .Register(source => source.Name, destination => destination.FullName)
                .Register(source => source.Data, destination => destination.Info, innerMappingAccordances)
                .Register(source => source.Items, destination => destination.Datas, innerItemMappingAccordances)
                .Register(
                    source => source.Progress,
                    destination => destination.Remaining,
                    expectedValue => Is.EqualTo(100 - expectedValue),
                    (sourceValue, destinationValue) => PercentageValuesMismatchMessage);

            Assert.That(testee.IsNullReferenceCheckRegistered, Is.False);
            Assert.That(testee.Count, Is.EqualTo(4));

            return testee;
        }

        #endregion

        #region SampleSource Class

        private sealed class SampleSource
        {
            [UsedImplicitly]
            public string Name
            {
                get;
                set;
            }

            [UsedImplicitly]
            public int Progress
            {
                get;
                set;
            }

            [UsedImplicitly]
            public SampleInnerSource Data
            {
                get;
                set;
            }

            [UsedImplicitly]
            public SampleSourceItem[] Items
            {
                get;
                set;
            }

            [UsedImplicitly]
            public string MatchingName
            {
                get;
                set;
            }

            [UsedImplicitly]
            public string MatchingTextCaseInsensitive
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

        #region SampleSourceItem Class

        private sealed class SampleSourceItem
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
            [UsedImplicitly]
            public string FullName
            {
                get;
                set;
            }

            [UsedImplicitly]
            public int Remaining
            {
                get;
                set;
            }

            [UsedImplicitly]
            public SampleInnerDestination Info
            {
                get;
                set;
            }

            [UsedImplicitly]
            public SampleDestinationItem[] Datas
            {
                get;
                set;
            }

            [UsedImplicitly]
            public string MatchingName
            {
                get;
                set;
            }

            [UsedImplicitly]
            //// ReSharper disable once InconsistentNaming -- For the case-insensitive test case
            public string matchingtextcaseinsensitive
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

        #region SampleDestinationItem Class

        private sealed class SampleDestinationItem
        {
            public string TextData
            {
                get;
                set;
            }
        }

        #endregion
    }
}