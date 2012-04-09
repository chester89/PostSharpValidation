using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Extensibility;
using PostsharpValidation.Lib.Advices;

namespace PostsharpValidation.Lib
{
    /// <summary>
    /// Provides a PostSharp <see cref="Task"/> that applies advice that processes any validation attributes that decorate elements of our
    /// code.
    /// </summary>
    public class ValidationProcessorTask : Task
    {
        private readonly Weaver codeWeaver;

        public ValidationProcessorTask()
        {
            codeWeaver = new Weaver(Project);

            WeaveMethods();
            WeaveProperties();
        }

        /// <summary>
        /// Weaves validation advice for all the applicable parameter and method declarations found in the current assembly.
        /// </summary>
        private void WeaveMethods()
        {
            foreach (MethodDefDeclaration method in EnumerateMethods())
            {
                for (int i = 0; i < method.Parameters.Count; i++)
                    ApplyParameterAdvice(method.Parameters[i], method, i);

                ApplyMethodAdvice(method);
            }
        }

        /// <summary>
        /// Weaves validation advice for all the applicable property declarations found in the current assembly.
        /// </summary>
        private void WeaveProperties()
        {
            foreach (PropertyDeclaration property in EnumerateProperties())
                ApplyPropertyAdvice(property);
        }

        /// <summary>
        /// Creates and applies parameter validation advice for all parameters validation attributes found in provided parameter declaration.
        /// </summary>
        /// <param name="target">Parameter declaration that may contain one or more validation attributes.</param>
        /// <param name="method">The method that the parameter belongs to.</param>
        /// <param name="parameterIndex">The index of the parameter in the method's argument list.</param>
        private void ApplyParameterAdvice(ParameterDeclaration target, MethodDefDeclaration method, int parameterIndex)
        {
            IEnumerable<ParameterValidationAdvice> advices
                = FindAdvisableAttributes<ParameterValidationAttribute>(target)
                    .Select(x => new ParameterValidationAdvice(x, target, parameterIndex));

            foreach (var advice in advices)
                ApplyAdvice(advice, method);
        }

        /// <summary>
        /// Creates and applies parameter validation advice for all parameter validation attributes found in the provided property declaration.
        /// </summary>
        /// <param name="target">The property declaration to apply the advice to.</param>
        private void ApplyPropertyAdvice(PropertyDeclaration target)
        {
            if (!target.CanWrite)
                return;

            MethodDefDeclaration method = target.Members.GetBySemantic(MethodSemantics.Setter).Method;

            IEnumerable<ParameterValidationAdvice> advices
                = FindAdvisableAttributes<ParameterValidationAttribute>(target)
                    .Select(x => new ParameterValidationAdvice(x, method.Parameters[0], 0));

            foreach (ParameterValidationAdvice advice in advices)
                ApplyAdvice(advice, method);
        }

        /// <summary>
        /// Creates and applies method validation advice for all method validation attributes found in the provided method declaration.
        /// </summary>
        /// <param name="target">The method declaration to apply the advice to.</param>
        private void ApplyMethodAdvice(MethodDefDeclaration target)
        {
            IEnumerable<CustomAttributeDeclaration> attributes
                = FindAdvisableAttributes<MethodValidationAttribute>(target);

            foreach (CustomAttributeDeclaration attribute in attributes)
            {
                ApplyAdvice(new CollectParametersAdvice(target), target);
                ApplyAdvice(new MethodValidationAdvice(attribute), target);
            }
        }

        /// <summary>
        /// Applies the provided advice to the specified method.
        /// </summary>
        /// <param name="advice">Advice to apply to <c>method</c>.</param>
        /// <param name="method">The method declaration the advice gets applied to.</param>
        private void ApplyAdvice(IAdvice advice, MethodDefDeclaration method)
        {
            codeWeaver.AddMethodLevelAdvice(advice, new[] { method }, JoinPointKinds.BeforeMethodBody, null);
            codeWeaver.AddTypeLevelAdvice(advice, JoinPointKinds.BeforeStaticConstructor, new[] { method.DeclaringType });
        }

        /// <summary>
        /// Applies the provided advice to the specified method.
        /// </summary>
        /// <param name="advice">Advice responsible for creating parameter name/value pairs for <c>method</c>.</param>
        /// <param name="method">The method declaration the advice gets applied to.</param>
        private void ApplyAdvice(CollectParametersAdvice advice, MethodDefDeclaration method)
        {
            codeWeaver.AddMethodLevelAdvice(advice, new[] { method }, JoinPointKinds.BeforeMethodBody, null);
        }

        /// <summary>
        /// Exposes an enumerator for the various <see cref="MethodDefDeclaration"/> instances found in the current assembly.
        /// </summary>
        /// <returns>An exposed enumerator which supports the iteration over a collection of <see cref="MethodDefDeclaration"/> objects.</returns>
        private IEnumerable<MetadataDeclaration> EnumerateMethods()
        {
            IEnumerator<MetadataDeclaration> methodEnumerator = Project.Module.GetDeclarationEnumerator(TokenType.MethodDef);

            return Enumerate(methodEnumerator);
        }

        /// <summary>
        /// Exposes an enumerator for the various <see cref="PropertyDeclaration"/> instances found in the current assembly.
        /// </summary>
        /// <returns>An exposed enumerator which supports the iteration over a collection of <see cref="PropertyDeclaration"/> objects.</returns>
        private IEnumerable<MetadataDeclaration> EnumerateProperties()
        {
            IEnumerator<MetadataDeclaration> propertyEnumerator =
                Project.Module.GetDeclarationEnumerator(TokenType.Property);

            return Enumerate(propertyEnumerator);
        }

        /// <summary>
        /// Finds all <see cref="CustomAttributeDeclaration"/> belonging to the provided metadata that should be used to generate advice.
        /// </summary>
        /// <typeparam name="TValidationAttribute">The validation attribute type to look for.</typeparam>
        /// <param name="target">The metadata containing one or more validation attribute declarations.</param>
        /// <returns>
        /// Exposed enumerator supporting iteration over all validation attribute declarations that should be used to generate advice.
        /// </returns>
        private static IEnumerable<CustomAttributeDeclaration> FindAdvisableAttributes<TValidationAttribute>(IMetadataDeclaration target)
        {
            return from attribute in target.CustomAttributes
                   let attributeType = attribute.Constructor.DeclaringType.GetSystemType(null, null)
                   where typeof(TValidationAttribute).IsAssignableFrom(attributeType)
                   select attribute;
        }

        /// <summary>
        /// Exposes the provided enumerator.
        /// </summary>
        /// <typeparam name="T">The type of items in the enumerator.</typeparam>
        /// <param name="enumerator">Object which supports simple iteration over a generic collection.</param>
        /// <returns>An exposed enumerator that supports simple iteration over a generic collection.</returns>
        /// <remarks>
        /// For some reason PostSharp deals in returning <see cref="IEnumerator{T}"/> objects instead of the more common
        /// <see cref="IEnumerable{T}"/> type. This method exists to compensate for that.
        /// </remarks>
        private static IEnumerable<T> Enumerate<T>(IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }
}
