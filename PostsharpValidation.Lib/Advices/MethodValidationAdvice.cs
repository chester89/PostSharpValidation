using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;

namespace PostsharpValidation.Lib.Advices
{
    class MethodValidationAdvice: IAdvice
    {
        private readonly CustomAttributeDeclaration attributeDeclaration;

        public MethodValidationAdvice(CustomAttributeDeclaration attributeDeclaration)
        {
            this.attributeDeclaration = attributeDeclaration;
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
