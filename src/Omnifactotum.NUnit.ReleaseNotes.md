### Changes in 0.2.0 (since 0.1.0.13)

#### Major Changes

- `Omnifactotum.NUnit` is now the multi-target package for:
  - .NET Framework 4.0, 4.6.1, and 4.7.2
  - .NET Standard 2.0 and 2.1
  - .NET 5.0
- Set `Omnifactotum` package dependency version based on the target framework

#### Breaking Changes

- `MappingAccordances.AssertionFailedMessageCreator<in TValue>` delegate: Signature has been changed
- The signature of the `MappingAccordances<,>.Register` method using constraint creator has been changed to allow different types of source and destination values

#### New features

- A new `MappingAccordances<,>.Register` method overload has been introduced which allows to pass a constant literal failure message
- `NUnitFactotum`: Introduced `AssertCast().To<TDestination>()`
- `MappingAccordances`: Introduced the new method `RegisterAllMatchingProperties`
- The `AssertEquality` method may now also test equality and inequality operators

#### Minor updates and fixes

- Improved support of inner `MappingAccordances` for complex (nested) properties
- Added support of inner `MappingAccordances` for complex `IList<T>` properties
- Enabled NRT for `NUnitFactotum`

### Changes in 0.1.0.13 (since 0.1.0.7)
- `MappingAccordances<TSource, TDestination>`: Source and/or destination values can be `null`; an optional `null` reference check can be registered
- Introduced the `GenericTestCasesBase<T>` class and made it the ancestor of `TestCasesBase`

### Changes in 0.1.0.7 (since 0.1.0.2)
- Introduced the `MappingAccordances<TSource, TDestination>` class

### First published version 0.1.0.2
- Introduced the `NUnitFactotum` helper class
- Introduced the `TestCasesBase` abstract class
