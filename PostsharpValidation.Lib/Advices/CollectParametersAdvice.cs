using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;

namespace PostsharpValidation.Lib.Advices
{
    class CollectParametersAdvice: IAdvice
    {
        private readonly MethodDefDeclaration declaration;

        public CollectParametersAdvice(MethodDefDeclaration declaration)
        {
            this.declaration = declaration;
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
