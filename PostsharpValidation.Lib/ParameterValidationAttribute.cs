using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using PostSharp.Extensibility;
using PostSharp.Sdk.CodeModel;

namespace PostsharpValidation.Lib
{
    /// <summary>
    /// Provides a base aspect used to perform some kind of validation on the decorated parameter.  This class is abstract.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public abstract class ParameterValidationAttribute : ValidationAttribute
    {
        /// <inheritdoc/>
        public override MethodBase ValidationMethod
        {
            get { return ReadMethodBase((x, y, z) => Validate(x, y, z)); }
        }

        /// <summary>
        /// Gets the name of the method that the parameter of interest belongs to.
        /// </summary>
        protected string MethodName
        { get; private set; }

        /// <inheritdoc/>
        [CLSCompliant(false)]
        public override void CompileTimeValidate(INamedMetadataDeclaration metadata, Type typeValidated, IMessageSink messages)
        {
            base.CompileTimeValidate(metadata, typeValidated, messages);

            MethodName = metadata.ParentMember.Name;
        }

        /// <summary>
        /// Executes the validation logic associated with this aspect.
        /// </summary>
        /// <param name="target">An instance of the object hosting the parameter on which the validation method is being invoked.</param>
        /// <param name="value">The value of the parameter being validated.</param>
        /// <param name="parameterName">The name of the parameter being validated.</param>
        /// <remarks>
        /// This needs to remain public, even if it appears it has no external callers. This method is invoked in a non-standard
        /// way by some MSIL added by our aspect weaver.
        /// </remarks>
        public abstract void Validate(object target, object value, string parameterName);

        /// <summary>
        /// Retrieves the <see cref="MethodBase"/> of the method expressed.
        /// </summary>
        /// <param name="methodExpression">An expression of the method being called.</param>
        /// <returns>Information regarding the method expressed in <c>methodExpression</c>.</returns>
        private static MethodBase ReadMethodBase(Expression<Action<object, object, string>> methodExpression)
        {
            return ((MethodCallExpression)methodExpression.Body).Method;
        }
    }

}
