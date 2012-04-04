using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using PostSharp;
using PostSharp.Extensibility;
using PostSharp.Sdk.CodeModel;

namespace PostsharpValidation.Lib
{
    /// <summary>
    /// Provides a base aspect used to perform some kind of validation on the decorated code element.
    /// </summary>
    [Serializable]
    [RequirePostSharp("PostsharpValidation.Lib", "ValidationProcessor")]
    public abstract class ValidationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the type of <see cref="System.Exception"/> thrown upon validation failure.
        /// </summary>
        public Type Exception
        { get; set; }

        /// <summary>
        /// Gets or sets the message used when throwing an exception upon validation failure.
        /// </summary>
        public string FailureMessage
        { get; set; }

        /// <summary>
        /// Gets the validation method responsible for validating the method the advice is applied to.
        /// </summary>
        public abstract MethodBase ValidationMethod { get; }

        /// <summary>
        /// Method invoked at build-time to ensure that the target this advice is applied to is an appropriate one.
        /// </summary>
        /// <param name="metadata">Metadata for the parameter receiving the advice.</param>
        /// <param name="typeValidated">The type of object ultimately being validated.</param>
        /// <param name="messages">An <see cref="IMessageSink"/> instance, used to write messages.</param>
        [CLSCompliant(false)]
        public virtual void CompileTimeValidate(INamedMetadataDeclaration metadata, Type typeValidated, IMessageSink messages)
        {
            // Usually I don't throw exceptions in aspect methods that are active at compile-time, but this takes the cake...
            if (null == messages)
                throw new ArgumentNullException("messages", Strings.ValidationNullMessageSink);

            if (!ValidateNonNullMetadata(metadata, messages))
                return;

            if (!ValidateExceptionType(messages))
                return;

            if (!ValidateInitializableExceptionType(messages))
                return;

            messages.Write(
                new Message(MessageLocation.Of(metadata),
                            SeverityType.Verbose,
                            Strings.ValidationAspectPassedValidationMessageId,
                            String.Format(CultureInfo.InvariantCulture,
                                          Strings.ValidationAspectPassedValidationMessageFormat,
                                          metadata.Name),
                            String.Empty,
                            Assembly.GetCallingAssembly().FullName,
                            null));
        }

        /// <summary>
        /// Deserializes an object from the specified string.
        /// </summary>
        /// <param name="serializedValue">The serialized object.</param>
        /// <returns></returns>
        public static object DeserializeFromString(string serializedValue)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using (MemoryStream buffer = new MemoryStream(Convert.FromBase64String(serializedValue)))
            {
                return formatter.Deserialize(buffer);
            }
        }

        /// <summary>
        /// Handles the response to a failure in validation of the applied element by throwing the configured type
        /// of exception.
        /// </summary>
        /// <param name="message">
        /// A message to use when throwing the exception. This is ignored if the current <see cref="ValidationAttribute"/> is
        /// configured to use a standard validation error message.
        /// </param>
        /// <param name="exception">The type of exception to throw (ignoring the configured type).</param>
        protected void ValidationFailed(string message, Type exception = null)
        {
            Type exceptionType = exception ?? Exception;

            string errorMessage = String.IsNullOrEmpty(FailureMessage) ? message : FailureMessage;

            // If no exception has been configured, we'll just fallback to a "stock" type.
            // InvalidOperationException fits pretty much any situation, if a bit unclear.
            if (null == exceptionType)
                throw new InvalidOperationException(errorMessage);

            throw (Exception)Activator.CreateInstance(exceptionType, errorMessage);
        }

        /// <summary>
        /// Handles the response to a failure in validation of the applied element by throwing the configured type
        /// of exception.
        /// </summary>
        /// <param name="message">
        /// A message to use when throwing the exception. This is ignored if the current <see cref="ParameterValidationAttribute"/> is
        /// configured to use a standard validation error message.
        /// </param>
        /// <param name="elementValue">The value of the element that failed validation.</param>
        /// <param name="elementName">The name of the element that failed validation.</param>
        /// <param name="exception">The type of exception to throw (ignoring the configured type).</param>
        protected void ValidationFailed(string message, object elementValue, string elementName, Type exception = null)
        {
            Type exceptionType = exception ?? Exception;

            string errorMessage = String.IsNullOrEmpty(FailureMessage) ? message : FailureMessage;

            // If no exception has been configured, we'll just fallback to a "stock" type.
            // InvalidOperationException fits pretty much any situation, if a bit unclear.
            if (null == exceptionType)
                throw new InvalidOperationException(errorMessage);

            // We'll make an attempt to tap into some built-in functionality of some well-known types
            // in the event one of them is the configured exception type.
            if (typeof(ArgumentNullException).Equals(exceptionType))
                throw new ArgumentNullException(elementName, message);

            if (typeof(ArgumentException).Equals(exceptionType))
                throw new ArgumentException(message, elementName);

            if (typeof(ArgumentOutOfRangeException).Equals(exceptionType))
                throw new ArgumentOutOfRangeException(elementName, elementValue, message);

            throw (Exception)Activator.CreateInstance(exceptionType, errorMessage);
        }

        /// <summary>
        /// Validates that the configured exception <see cref="Type"/> is an actual type of <see cref="System.Exception"/>.
        /// </summary>
        /// <param name="messages">An <see cref="IMessageSink"/> instance, used to write messages.</param>
        /// <returns>True if <see cref="Exception"/> is a valid type of exception; otherwise, false.</returns>
        private bool ValidateExceptionType(IMessageSink messages)
        {
            bool isException = null != Exception && typeof(Exception).IsAssignableFrom(Exception);

            if (!isException)
            {
                messages.Write(
                    new Message(MessageLocation.Of(Exception),
                                SeverityType.Fatal,
                                Strings.ErrorValidationExceptionBadTypeMessageId,
                                String.Format(CultureInfo.InvariantCulture,
                                              Strings.ErrorValidationExceptionBadTypeMessageFormat,
                                              Exception.Name),
                                String.Empty,
                                Assembly.GetCallingAssembly().FullName,
                                null));
            }

            return isException;
        }

        /// <summary>
        /// Validates that the configured validation failure exception is able to be initialized by the aspect.
        /// </summary>
        /// <param name="messages">An <see cref="IMessageSink"/> instance, used to write messages.</param>
        /// <returns>True if the configured exception can be instantiated by this aspect; otherwise, false.</returns>
        /// <remarks>
        /// A properly implemented exception will additionally declare constructors that match the signatures of the base
        /// <see cref="Exception"/> type. This aspect relies particularly on the presence of a constructor overload
        /// that accepts a single string parameter (the exception message).
        /// </remarks>
        private bool ValidateInitializableExceptionType(IMessageSink messages)
        {
            ConstructorInfo constructor = Exception.GetConstructor(BindingFlags.Public | BindingFlags.Instance,
                                                                   null,
                                                                   new[] { typeof(string) },
                                                                   null);
            bool isInstantiable = null != constructor;

            if (!isInstantiable)
            {
                messages.Write(
                    new Message(MessageLocation.Of(Exception),
                                SeverityType.Fatal,
                                Strings.ErrorValidationExceptionImproperTypeMessageId,
                                String.Format(CultureInfo.InvariantCulture,
                                              Strings.ErrorValidationExceptionImproperTypeMessageFormat,
                                              Exception.Name),
                                String.Empty,
                                Assembly.GetCallingAssembly().FullName,
                                null));
            }

            return isInstantiable;
        }

        /// <summary>
        /// Validates that the provided metadata is not null.
        /// </summary>
        /// <param name="metadata">The metadata to do a null check on.</param>
        /// <param name="messages">An <see cref="IMessageSink"/> instance, used to write messages.</param>
        /// <returns>True if <c>metadata</c> is not null; otherwise, false.</returns>
        private static bool ValidateNonNullMetadata(INamedMetadataDeclaration metadata, IMessageSink messages)
        {
            bool isNotNull = null != metadata;

            if (!isNotNull)
            {
                messages.Write(
                    new Message(MessageLocation.Of(metadata),
                                SeverityType.Fatal,
                                Strings.ErrorNullMetadataMessageId,
                                Strings.ErrorNullMetadataMessage,
                                String.Empty,
                                Assembly.GetCallingAssembly().FullName,
                                null));
            }

            return isNotNull;
        }
    }
}
