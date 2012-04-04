using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PostSharp;
using PostSharp.Extensibility;
using PostSharp.Sdk.CodeModel;

namespace PostsharpValidation.Lib
{
    /// <summary>
    /// Provides a validation attribute that, when applied to a parameter, will ensure that the parameter is not null.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class NotNullAttribute : ParameterValidationAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotNullAttribute"/> class.
        /// </summary>
        /// <remarks>The default exception type set by the constructor is <see cref="ArgumentNullException"/>.</remarks>
        public NotNullAttribute()
        {
            Exception = typeof(ArgumentNullException);
        }

        /// <inheritdoc/>
        [CLSCompliant(false)]
        public override void CompileTimeValidate(INamedMetadataDeclaration metadata, Type typeValidated, IMessageSink messages)
        {
            base.CompileTimeValidate(metadata, typeValidated, messages);

            if (typeValidated.IsValueType && null == Nullable.GetUnderlyingType(typeValidated))
            {
                messages.Write(
                    new Message(MessageLocation.Of(metadata),
                                SeverityType.Error,
                                Strings.ErrorNotNullNonNullableMessageId,
                                String.Format(CultureInfo.InvariantCulture,
                                              Strings.ErrorNotNullNonNullableMessageFormat,
                                              typeValidated.Name),
                                String.Empty,
                                Assembly.GetCallingAssembly().FullName,
                                null));
            }
        }

        /// <inheritdoc/>
        public override void Validate(object target, object value, string parameterName)
        {
            if (null == value)
            {
                ValidationFailed(
                    String.Format(CultureInfo.InvariantCulture,
                                  Strings.ErrorIsNotNullFailedFormat,
                                  parameterName,
                                  MethodName),
                    value,
                    parameterName);
            }
        }
    }

}
