namespace IronBrew2.Obfuscator
{
	public class ObfuscationSettings
	{
		public bool EncryptStrings;
		public bool EncryptImportantStrings;
		public bool ControlFlow;
		public bool BytecodeCompress;
		public int DecryptTableLen;
		public bool PreserveLineInfo;
		public bool Mutate;
		public bool SuperOperators;
		public int MaxMiniSuperOperators;
		public int MaxMegaSuperOperators;
		public int MaxMutations;
		
		public ObfuscationSettings()
		{
			EncryptStrings = true;
			EncryptImportantStrings = false;
			ControlFlow = true;
			BytecodeCompress = true;
			DecryptTableLen = 500;
			PreserveLineInfo = false;
			Mutate = true;
			SuperOperators = true;
			MaxMegaSuperOperators = 694201337;
			MaxMiniSuperOperators = 694201337;
			MaxMutations = 694201337;
		}
	}
}