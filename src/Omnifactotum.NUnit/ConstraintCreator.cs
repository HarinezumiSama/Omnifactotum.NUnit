using NUnit.Framework.Constraints;

namespace Omnifactotum.NUnit
{
    /// <summary>
    ///     Represents a reference to a method that creates an instance of
    ///     <see cref="IResolveConstraint"/> using the specified expected value.
    /// </summary>
    /// <param name="expectedValue">
    ///     The expected value.
    /// </param>
    /// <typeparam name="TValue">
    ///     The type of the expected value.
    /// </typeparam>
    public delegate IResolveConstraint ConstraintCreator<in TValue>(TValue expectedValue);
}