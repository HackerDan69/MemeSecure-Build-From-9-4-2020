using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using IronBrew2.Bytecode_Library.Bytecode;
using IronBrew2.Bytecode_Library.IR;
using IronBrew2.Extensions;
using IronBrew2.Obfuscator.Opcodes;

namespace IronBrew2.Obfuscator.VM_Generation
{
	public class Generator
	{
		private ObfuscationContext _context;
		
		public Generator(ObfuscationContext context) =>
			_context = context;

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public bool IsUsed(Chunk chunk, VOpcode virt)
		{
			bool isUsed = false;
			foreach (Instruction ins in chunk.Instructions)
				if (virt.IsInstruction(ins))
				{
					if (!_context.InstructionMapping.ContainsKey(ins.OpCode))
						_context.InstructionMapping.Add(ins.OpCode, virt);

					ins.CustomData = new CustomInstructionData {Opcode = virt};
					isUsed = true;
				}

			foreach (Chunk sChunk in chunk.Functions)
				isUsed |= IsUsed(sChunk, virt);

			return isUsed;
		}

		public static List<int> Compress(byte[] uncompressed)
		{
			// build the dictionary
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			for (int i = 0; i < 256; i++)
				dictionary.Add(((char)i).ToString(), i);
 
			string    w          = string.Empty;
			List<int> compressed = new List<int>();
 
			foreach (byte b in uncompressed)
			{
				string wc = w + (char)b;
				if (dictionary.ContainsKey(wc))
					w = wc;
				
				else
				{
					// write w to output
					compressed.Add(dictionary[w]);
					// wc is a new sequence; add it to the dictionary
					dictionary.Add(wc, dictionary.Count);
					w = ((char) b).ToString();
				}
			}
 
			// write remaining output if necessary
			if (!string.IsNullOrEmpty(w))
				compressed.Add(dictionary[w]);
 
			return compressed;
		}

		public static string ToBase36(ulong value)
        {
            const string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var sb = new StringBuilder(13);
            do
            {
                sb.Insert(0, base36[(byte)(value % 36)]);
                value /= 36;
            } while (value != 0);
            return sb.ToString();
        }

		public static string CompressedToString(List<int> compressed)
		{
			StringBuilder sb = new StringBuilder();
			foreach (int i in compressed)
			{
				string n = ToBase36((ulong)i);
				
				sb.Append(ToBase36((ulong)n.Length));
				sb.Append(n);
			}

			return sb.ToString();
		}

		public List<OpMutated> GenerateMutations(List<VOpcode> opcodes)
		{
			Random r = new Random();
			List<OpMutated> mutated = new List<OpMutated>();

			foreach (VOpcode opc in opcodes)
			{
				if (opc is OpSuperOperator)
					continue;

				for (int i = 0; i < r.Next(35, 50); i++)
				{
					int[] rand = {0, 1, 2};
					rand.Shuffle();

					OpMutated mut = new OpMutated();

					mut.Registers = rand;
					mut.Mutated = opc;
						
					mutated.Add(mut);
				}
			}

			mutated.Shuffle();
			return mutated;
		}

		public void FoldMutations(List<OpMutated> mutations, HashSet<OpMutated> used, Chunk chunk)
		{
			bool[] skip = new bool[chunk.Instructions.Count + 1];
			
			for (int i = 0; i < chunk.Instructions.Count; i++)
			{
				Instruction opc = chunk.Instructions[i];

				switch (opc.OpCode)
				{
					case Opcode.Closure:
						for (int j = 1; j <= ((Chunk) opc.RefOperands[0]).UpvalueCount; j++)
							skip[i + j] = true;

						break;
				}
			}
			
			for (int i = 0; i < chunk.Instructions.Count; i++)
			{
				if (skip[i])
					continue;
				
				Instruction opc = chunk.Instructions[i];
				CustomInstructionData data = opc.CustomData;
				
				foreach (OpMutated mut in mutations)
					if (data.Opcode == mut.Mutated && data.WrittenOpcode == null)
					{
						if (!used.Contains(mut))
							used.Add(mut);

						data.Opcode = mut;
						break;
					}
			}
			
			foreach (Chunk _c in chunk.Functions)
				FoldMutations(mutations, used, _c);
		}

		public List<OpSuperOperator> GenerateSuperOperators(Chunk chunk, int maxSize, int minSize = 5)
		{
			List<OpSuperOperator> results = new List<OpSuperOperator>();
			Random                r       = new Random();

			bool[] skip = new bool[chunk.Instructions.Count + 1];

			for (int i = 0; i < chunk.Instructions.Count - 1; i++)
			{
				switch (chunk.Instructions[i].OpCode)
				{
					case Opcode.Closure:
					{
						skip[i] = true;
						for (int j = 0; j < ((Chunk) chunk.Instructions[i].RefOperands[0]).UpvalueCount; j++)
							skip[i + j + 1] = true;
							
						break;
					}

					case Opcode.Eq:
					case Opcode.Lt:
					case Opcode.Le:
					case Opcode.Test:
					case Opcode.TestSet:
					case Opcode.TForLoop:
					case Opcode.SetList:
					case Opcode.LoadBool when chunk.Instructions[i].C != 0:
						skip[i + 1] = true;
						break;

					case Opcode.ForLoop:
					case Opcode.ForPrep:
					case Opcode.Jmp:
						chunk.Instructions[i].UpdateRegisters();
						
						skip[i + 1] = true;
						skip[i + chunk.Instructions[i].B + 1] = true;
						break;
				}
				
				if (chunk.Instructions[i].CustomData.WrittenOpcode is OpSuperOperator su && su.SubOpcodes != null)
					for (int j = 0; j < su.SubOpcodes.Length; j++)
						skip[i + j] = true;
			}
			
			int c = 0;
			while (c < chunk.Instructions.Count)
			{
				int targetCount = maxSize;
				OpSuperOperator superOperator = new OpSuperOperator {SubOpcodes = new VOpcode[targetCount]};

				bool d     = true;
				int cutoff = targetCount;

				for (int j = 0; j < targetCount; j++)
					if (c + j > chunk.Instructions.Count - 1 || skip[c + j])
					{
						cutoff = j; 
						d = false;
						break;
					}

				if (!d)
				{
					if (cutoff < minSize)
					{
						c += cutoff + 1;	
						continue;
					}
						
					targetCount = cutoff;	
					superOperator = new OpSuperOperator {SubOpcodes = new VOpcode[targetCount]};
				}
				
				for (int j = 0; j < targetCount; j++)
					superOperator.SubOpcodes[j] =
						chunk.Instructions[c + j].CustomData.Opcode;

				results.Add(superOperator);
				c += targetCount + 1;
			}

			foreach (var _c in chunk.Functions)
				results.AddRange(GenerateSuperOperators(_c, maxSize));
			
			return results;
		}

		public void FoldAdditionalSuperOperators(Chunk chunk, List<OpSuperOperator> operators, ref int folded)
		{
			bool[] skip = new bool[chunk.Instructions.Count + 1];
			for (int i = 0; i < chunk.Instructions.Count - 1; i++)
			{
				switch (chunk.Instructions[i].OpCode)
				{
					case Opcode.Closure:
					{
						skip[i] = true;
						for (int j = 0; j < ((Chunk) chunk.Instructions[i].RefOperands[0]).UpvalueCount; j++)
							skip[i + j + 1] = true;
							
						break;
					}

					case Opcode.Eq:
					case Opcode.Lt:
					case Opcode.Le:
					case Opcode.Test:
					case Opcode.TestSet:
					case Opcode.TForLoop:
					case Opcode.SetList:
					case Opcode.LoadBool when chunk.Instructions[i].C != 0:
						skip[i + 1] = true;
						break;

					case Opcode.ForLoop:
					case Opcode.ForPrep:
					case Opcode.Jmp:
						chunk.Instructions[i].UpdateRegisters();
						skip[i + 1] = true;
						skip[i + chunk.Instructions[i].B + 1] = true;
						break;
				}
				
				if (chunk.Instructions[i].CustomData.WrittenOpcode is OpSuperOperator su && su.SubOpcodes != null)
					for (int j = 0; j < su.SubOpcodes.Length; j++)
						skip[i + j] = true;
			}
			
			int c = 0;
			while (c < chunk.Instructions.Count)
			{
				if (skip[c])
				{
					c++;
					continue;
				}

				bool used = false;

				foreach (OpSuperOperator op in operators)
				{
					int targetCount = op.SubOpcodes.Length;
					bool cu = true;
					for (int j = 0; j < targetCount; j++)
					{
						if (c + j > chunk.Instructions.Count - 1 || skip[c + j])
						{
							cu = false;
							break;
						}
					}

					if (!cu)
						continue;


					List<Instruction> taken = chunk.Instructions.Skip(c).Take(targetCount).ToList();
					if (op.IsInstruction(taken))
					{
						for (int j = 0; j < targetCount; j++)
						{
							skip[c + j] = true;
							chunk.Instructions[c + j].CustomData.WrittenOpcode = new OpSuperOperator {VIndex = 0};
						}

						chunk.Instructions[c].CustomData.WrittenOpcode = op;

						used = true;
						break;
					}
				}

				if (!used)
					c++;
				else
					folded++;
			}

			foreach (var _c in chunk.Functions)
				FoldAdditionalSuperOperators(_c, operators, ref folded);
		}
		
		public string GenerateVM(ObfuscationSettings settings)
		{
			Random r = new Random();

			List<VOpcode> virtuals = Assembly.GetExecutingAssembly().GetTypes()
			                                 .Where(t => t.IsSubclassOf(typeof(VOpcode)))
			                                 .Select(Activator.CreateInstance)
			                                 .Cast<VOpcode>()
			                                 .Where(t => IsUsed(_context.HeadChunk, t))
			                                 .ToList();

			
			if (settings.Mutate)
			{
				List<OpMutated> muts = GenerateMutations(virtuals).Take(settings.MaxMutations).ToList();
				
				Console.WriteLine("Created " + muts.Count + " mutations.");
				
				HashSet<OpMutated> used = new HashSet<OpMutated>();
				FoldMutations(muts, used, _context.HeadChunk);
				
				Console.WriteLine("Used " + used.Count + " mutations.");
				
				virtuals.AddRange(used);
			}
			
			if (settings.SuperOperators)
			{
				int folded = 0;
				
				var megaOperators = GenerateSuperOperators(_context.HeadChunk, 80, 60).OrderBy(t => r.Next())
					.Take(settings.MaxMegaSuperOperators).ToList();
				
				Console.WriteLine("Created " + megaOperators.Count + " mega super operators.");
				
				virtuals.AddRange(megaOperators);
				
				FoldAdditionalSuperOperators(_context.HeadChunk, megaOperators, ref folded);
				
				var miniOperators = GenerateSuperOperators(_context.HeadChunk, 10).OrderBy(t => r.Next())
					.Take(settings.MaxMiniSuperOperators).ToList();
				
				Console.WriteLine("Created " + miniOperators.Count + " mini super operators.");
				
				virtuals.AddRange(miniOperators);
				
				FoldAdditionalSuperOperators(_context.HeadChunk, miniOperators, ref folded);
				
				Console.WriteLine("Folded " + folded + " instructions into super operators.");
			}
			
			virtuals.Shuffle();
			
			for (int i = 0; i < virtuals.Count; i++)
				virtuals[i].VIndex = i;

			string vm = "";

			byte[] bs = new Serializer(_context, settings).SerializeLChunk(_context.HeadChunk);
			
			vm += @"
local Mocha = function(Coffee)
while true do
break;
end
local Allahs_car = [[Can I have a joe?]]
local dingling = getfenv()['\115\116\114\105\110\103']
local Byte         = dingling.byte;
local Char         = dingling.char;
local Sub          = dingling.sub;
local Insert       = table.insert;
local LDExp        = math.ldexp;
local hambanana;
local GetFEnv      = getfenv or function() return _ENV end;
local xyy          = function(dong)
return dong
end
local Setmetatable = setmetatable;
local Select       = select;
local been = Select;
local subcontainment = Sub;
local car = 'allah'
local Cocomelon = table.concat

local Concat       = function(t,l)
    if not l then l = '' end;
    local final = ''
    for i = 1, #t do
        if i == #t then  
            final = final .. t[i]
            break
        end
        final = final .. t[i] .. l
    end
    return final end
if not DeepPrint == nil then
return error('n')
end
local base64bytes = {['A']=0,['B']=1,['C']=2,['D']=3,['E']=4,['F']=5,['G']=6,['H']=7,['I']=8,['J']=9,['K']=10,['L']=11,['M']=12,['N']=13,['O']=14,['P']=15,['Q']=16,['R']=17,['S']=18,['T']=19,['U']=20,['V']=21,['W']=22,['X']=23,['Y']=24,['Z']=25,['a']=26,['b']=27,['c']=28,['d']=29,['e']=30,['f']=31,['g']=32,['h']=33,['i']=34,['j']=35,['k']=36,['l']=37,['m']=38,['n']=39,['o']=40,['p']=41,['q']=42,['r']=43,['s']=44,['t']=45,['u']=46,['v']=47,['w']=48,['x']=49,['y']=50,['z']=51,['0']=52,['1']=53,['2']=54,['3']=55,['4']=56,['5']=57,['6']=58,['7']=59,['8']=60,['9']=61,['-']=62,['_']=63,['=']=nil}
local function konkshredernigga(x,b)
	return (math.fmod(x, 2^b) - math.fmod(x, 2^(b-1)) > 0)
end
local function lsh(value,shift)
	return (value*(2^shift)) % 256
end

-- shift right
local function rsh(value,shift)
	return math.floor(value/2^shift) % 256
end

-- logic OR for number values
local function lor(x,y)
	result = 0
	for p=1,8 do result = result + (((konkshredernigga(x,p) or konkshredernigga(y,p)) == true) and 2^(p-1) or 0) end
	return result
end

-- encryption table
local base64chars = {[0]='A',[1]='B',[2]='C',[3]='D',[4]='E',[5]='F',[6]='G',[7]='H',[8]='I',[9]='J',[10]='K',[11]='L',[12]='M',[13]='N',[14]='O',[15]='P',[16]='Q',[17]='R',[18]='S',[19]='T',[20]='U',[21]='V',[22]='W',[23]='X',[24]='Y',[25]='Z',[26]='a',[27]='b',[28]='c',[29]='d',[30]='e',[31]='f',[32]='g',[33]='h',[34]='i',[35]='j',[36]='k',[37]='l',[38]='m',[39]='n',[40]='o',[41]='p',[42]='q',[43]='r',[44]='s',[45]='t',[46]='u',[47]='v',[48]='w',[49]='x',[50]='y',[51]='z',[52]='0',[53]='1',[54]='2',[55]='3',[56]='4',[57]='5',[58]='6',[59]='7',[60]='8',[61]='9',[62]='-',[63]='_'}

local function encodebase64a(data)
	local bytes = {}
	local result = ''
	for spos=0,string.len(data)-1,3 do
		for byte=1,3 do bytes[byte] = string.byte(string.sub(data,(spos+byte))) or 0 end
		result = string.format('%s%s%s%s%s',result,base64chars[rsh(bytes[1],2)],base64chars[lor(lsh((bytes[1] % 4),4), rsh(bytes[2],4))] or ' = ',((#data-spos) > 1) and base64chars[lor(lsh(bytes[2] % 16,2), rsh(bytes[3],6))] or ' = ',((#data-spos) > 2) and base64chars[(bytes[3] % 64)] or ' = ')

    end
	return result
end
local function decodebase64(data)
	local chars = {}
	local result=''
	for dpos=0,string.len(data)-1,4 do
		for char=1,4 do chars[char] = base64bytes[(string.sub(data,(dpos+char),(dpos+char)) or '=')] end
		result = string.format('%s%s%s%s',result,string.char(lor(lsh(chars[1],2), rsh(chars[2],4))),(chars[3] ~= nil) and string.char(lor(lsh(chars[2],4), rsh(chars[3],2))) or '',(chars[4] ~= nil) and string.char(lor(lsh(chars[3],6) % 192, (chars[4]))) or '')
	end
	return result
end
local Unpack = unpack or table.unpack;
local ToNumber = tonumber;
local function donothing(e)
end
";

			if (settings.BytecodeCompress)
			{
				vm += "local ThisIsTehTable = {[1]='What you just did is very gay, please go to time out and never return lmao\\n\\n'} local function decompress(b)local c,d,e=\"\",\"\",{}local f=256;local g={}for h=0,f-1 do g[h]=Char(h)end;local i=1;local function k()local l=ToNumber(Sub(b, i,i),36)i=i+1;local m=ToNumber(Sub(b, i,i+l-1),36)i=i+l;return m end;c=Char(k())e[1]=c;while i<#b do local n=k()if g[n]then d=g[n]else d=c..Sub(c, 1,1)end;g[f]=c..Sub(d, 1,1)e[#e+1],c,f=d,d,f+1 end;return Concat(e)end;";
				vm += "local ByteString=decompress('" + CompressedToString(Compress(bs)) + "');\n";
			}
			else
			{
				vm += "ByteString='";

				StringBuilder sb = new StringBuilder();
				foreach (byte b in bs)
				{
					sb.Append('\\');
					sb.Append(b);
				}

				vm += sb + "';\n";
			}

			int maxConstants = 0;

			void ComputeConstants(Chunk c)
			{
				if (c.Constants.Count > maxConstants)
					maxConstants = c.Constants.Count;
				
				foreach (Chunk _c in c.Functions)
					ComputeConstants(_c);
			}
			
			ComputeConstants(_context.HeadChunk);

			vm += VMStrings.VMP1
                // changed this for confusion

				.Replace("XOR_KEY", Base64Encode(_context.PrimaryXorKey.ToString()))
				.Replace("CONST_BOOL", _context.ConstantMapping[1].ToString())
				.Replace("CONST_FLOAT", _context.ConstantMapping[2].ToString())
				.Replace("CONST_STRING", _context.ConstantMapping[3].ToString());
			
			for (int i = 0; i < (int) ChunkStep.StepCount; i++)
			{
				switch (_context.ChunkSteps[i])
				{
					case ChunkStep.ParameterCount:
						vm += "Chunk[3] = gBits8();";
						break;
					case ChunkStep.Instructions:
						vm +=
							$@"for Idx=1,gBits32() do 
									local Descriptor = gBits8();
									if (gBit(Descriptor, 1, 1) == 0) then
										local Type = gBit(Descriptor, 2, 3);
										local Mask = gBit(Descriptor, 4, 6);
										
										local Inst=
										{{
											gBits16(),
											gBits16(),
											nil,
											nil
										}};
	
										if (Type == 0) then 
											Inst[D9_OP_B] = gBits16(); 
											Inst[D9_OP_C] = gBits16();
										elseif(Type==1) then 
											Inst[D9_OP_B] = gBits32();
										elseif(Type==2) then 
											Inst[D9_OP_B] = gBits32() - (2 ^ 16)
										elseif(Type==3) then 
											Inst[D9_OP_B] = gBits32() - (2 ^ 16)
											Inst[D9_OP_C] = gBits16();
										end;
	
										if (gBit(Mask, 1, 1) == 1) then Inst[D9_OP_A] = Consts[Inst[D9_OP_A]] end
										if (gBit(Mask, 2, 2) == 1) then Inst[D9_OP_B] = Consts[Inst[D9_OP_B]] end
										if (gBit(Mask, 3, 3) == 1) then Inst[D9_OP_C] = Consts[Inst[D9_OP_C]] end
										
										Instrs[Idx] = Inst;
									end
								end;";
						break;
					case ChunkStep.Functions:
						vm += "for Idx=1,gBits32() do Functions[Idx-1]=Deserialize();end;";
						break;
					case ChunkStep.LineInfo:
						if (settings.PreserveLineInfo)
							vm += "for Idx=1,gBits32() do Lines[Idx]=gBits32();end;";
						break;
				}
			}

			vm += "return Chunk;end;";
			vm += settings.PreserveLineInfo ? VMStrings.VMP2_LI : VMStrings.VMP2;

			int maxFunc = 0;

			void ComputeFuncs(Chunk c)
			{
				if (c.Functions.Count > maxFunc)
					maxFunc = c.Functions.Count;
				
				foreach (Chunk _c in c.Functions)
					ComputeFuncs(_c);
			}
			
			ComputeFuncs(_context.HeadChunk);

			int maxInstrs = 0;

			void ComputeInstrs(Chunk c)
			{
				if (c.Instructions.Count > maxInstrs)
					maxInstrs = c.Instructions.Count;
				
				foreach (Chunk _c in c.Functions)
					ComputeInstrs(_c);
			}
			
			ComputeInstrs(_context.HeadChunk);
			
			string GetStr(List<int> opcodes)
			{
				string str = "";
				
				if (opcodes.Count == 1)
					str += $"{virtuals[opcodes[0]].GetObfuscated(_context)}";

				else if (opcodes.Count == 2) 
				{
					if (r.Next(2) == 0)
					{
						str +=
							$"if Enum > {virtuals[opcodes[0]].VIndex} then {virtuals[opcodes[1]].GetObfuscated(_context)}";
						str += $"else {virtuals[opcodes[0]].GetObfuscated(_context)}";
						str += "end;";
					}
					else
					{
						str +=
							$"if Enum == {virtuals[opcodes[0]].VIndex} then {virtuals[opcodes[0]].GetObfuscated(_context)}";
						str += $"else {virtuals[opcodes[1]].GetObfuscated(_context)}";
						str += "end;";
					}
				}
				else
				{
					List<int> ordered = opcodes.OrderBy(o => o).ToList();
					var sorted = new[] { ordered.Take(ordered.Count / 2).ToList(), ordered.Skip(ordered.Count / 2).ToList() };
					
					str += "if Enum <= " + sorted[0].Last() + " then ";
					str += GetStr(sorted[0]);
					str += " else";
					str += GetStr(sorted[1]);
				}

				return str;
			}

			vm += GetStr(Enumerable.Range(0, virtuals.Count).ToList());
			vm += settings.PreserveLineInfo ? VMStrings.VMP3_LI : VMStrings.VMP3;
            // changed this
			vm = vm.Replace("D9_OP_ENUM", "0xD9ED/0xD9ED*0x01")
				.Replace("D9_OP_A", "xyy(0xD9ED/0xD9ED*0x02)")
				.Replace("D9_OP_B", "xyy(0xD9ED/0xD9ED*0x03)")
				.Replace("D9_OP_C", "xyy(0xD9ED/0xD9ED*0x04)");

			
			return vm;
		}
	}
}
