using IronBrew2.Bytecode_Library.Bytecode;
using IronBrew2.Bytecode_Library.IR;

namespace IronBrew2.Obfuscator.Opcodes
{
	public class OpConcat : VOpcode
	{
		public override bool IsInstruction(Instruction instruction) =>
			instruction.OpCode == Opcode.Concat;

		public override string GetObfuscated(ObfuscationContext context) =>
			"local B=Inst[D9_OP_B];local K=Stk[B] for Idx=B+1,Inst[D9_OP_C] do K=K..Stk[Idx];end;Stk[Inst[D9_OP_A]]=K;";
	}
}