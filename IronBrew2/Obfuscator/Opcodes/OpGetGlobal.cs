using IronBrew2.Bytecode_Library.Bytecode;
using IronBrew2.Bytecode_Library.IR;

namespace IronBrew2.Obfuscator.Opcodes
{
	public class OpGetGlobal : VOpcode
	{
		public override bool IsInstruction(Instruction instruction) =>
			instruction.OpCode == Opcode.GetGlobal;

		public override string GetObfuscated(ObfuscationContext context) =>
			"Stk[Inst[D9_OP_A]]=Env[Inst[D9_OP_B]];";

		public override void Mutate(Instruction instruction)
		{
			instruction.B++;
			instruction.ConstantMask |= InstructionConstantMask.RB;
		}
	}
}