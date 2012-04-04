using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;

namespace PostsharpValidation.Lib.Advices
{
    class ParameterValidationAdvice: IAdvice
    {
        private readonly CustomAttributeDeclaration declaration;
        private readonly ParameterDeclaration parameterDeclaration;
        private readonly int parameterIndex;

        public ParameterValidationAdvice(CustomAttributeDeclaration declaration, ParameterDeclaration parameterDeclaration, int parameterIndex)
        {
            this.declaration = declaration;
            this.parameterDeclaration = parameterDeclaration;
            this.parameterIndex = parameterIndex;
        }

        public bool RequiresWeave(WeavingContext context)
        {
            throw new NotImplementedException();
        }

        public void Weave(WeavingContext context, InstructionBlock block)
        {
            throw new NotImplementedException();
        }

        public int Priority
        {
            get { throw new NotImplementedException(); }
        }
    }
}
