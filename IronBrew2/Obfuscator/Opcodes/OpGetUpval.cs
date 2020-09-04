using IronBrew2.Bytecode_Library.Bytecode;
using IronBrew2.Bytecode_Library.IR;

namespace IronBrew2.Obfuscator.Opcodes
{
	public class OpGetUpval : VOpcode
	{
		public override bool IsInstruction(Instruction instruction) =>
			instruction.OpCode == Opcode.GetUpval;

		public override string GetObfuscated(ObfuscationContext context) =>
			"Stk[Inst[D9_OP_A]]=Upvalues[Inst[D9_OP_B]];";
	}
}