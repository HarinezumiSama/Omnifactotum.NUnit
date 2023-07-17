namespace Omnifactotum.NUnit
{
    /// <summary>
    ///     Represents the expectation for the overloaded equality and inequality operators.
    /// </summary>
    /// <seealso cref="NUnitFactotum.AssertEquality{T}"/>
    public enum AssertEqualityOperatorExpectation
    {
        /// <summary>
        ///     The type may or may not define the equality and inequality operators, but if they are defined, they will be tested.
        /// </summary>
        MayDefine,

        /// <summary>
        ///     The type must not define the equality and inequality operators.
        /// </summary>
        MustNotDefine,

        /// <summary>
        ///     The type must define the equality and inequality operators, and they will be tested.
        /// </summary>
        MustDefine
    }
}