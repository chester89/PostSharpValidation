using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PostsharpValidation.Lib
{
    /// <summary>
    /// Provides a base aspect used to perform validation on the decorated method.  This class is abstract.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public abstract class MethodValidationAttribute: ValidationAttribute
    {
        public override MethodBase ValidationMethod
        {
            get { throw new NotImplementedException(); }
        }
    }
}
