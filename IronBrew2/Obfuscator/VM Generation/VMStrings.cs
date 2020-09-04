using System;
using System.Text;

namespace IronBrew2.Obfuscator.VM_Generation
{
	public static class VMStrings
    {

        static char[] charactersAvailable = {'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
                                             '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};

        public static string GetRandomAlphaNumString(uint stringLength)
        {
            StringBuilder randomString = new StringBuilder();

            Random randomCharacter = new Random();

            for (uint i = 0; i < stringLength; i++)
            {
                int randomCharSelected = randomCharacter.Next(0, (charactersAvailable.Length - 1));

                randomString.Append(charactersAvailable[randomCharSelected]);
            }

            return randomString.ToString();
        }

        public static string VMP1 = "local coolkidskey = '" + GetRandomAlphaNumString(1000) + "'" + @"
local bitchnigga = ToNumber(decodebase64('XOR_KEY'))
local EEEE = {[1] = 'illegal',[2] = 'illegal',[3] = 'illegal',[4] = 'illegal',[5] = 'illegal',[6] = 'illegal',[7] = 'illegal',[10] = 'illegal',[11] = 'illegal',[12] = 'illegal',[13] = 'illegal',[14] = 'illegal',[15] = 'illegal',[16] = 'illegal',[17] = 'illegal',[18] = 'illegal',[19] = 'illegal',[20] = 'illegal'}
local BitXOR = bit and bit.bxor or function(a,b)
    local p,c=1,0
    while a>0 and b>0 do
        local ra,rb=a%2,b%2
        if ra~=rb then c=c+p end
        a,b,p=(a-ra)/2,(b-rb)/2,p*2
    end
    if a<b then a=b end
    while a>0 do
        local ra=a%2
        if ra>0 then c=c+p end
        a,p=(a-ra)/2,p*2
    end
    return c
end

local function gBit(Bit, Start, End)
	if End then
		local Res = (Bit / 2 ^ (Start - 1)) % 2 ^ ((End - 1) - (Start - 1) + 1);
		return Res - Res % 1;
	else
		local Plc = 2 ^ (Start - 1);
        return (Bit % (Plc + Plc) >= Plc) and 1 or 0;
	end;
end;

local Pos = 1;

local function gBits32()
    local W, X, Y, Z = Byte(ByteString, Pos, Pos + 3);

	W = BitXOR(W, 1*bitchnigga*0xD9ED/0xD9ED)
	X = BitXOR(X, 1*bitchnigga*0xD9ED/0xD9ED)
	Y = BitXOR(Y, 1*bitchnigga*0xD9ED/0xD9ED)
	Z = BitXOR(Z, 1*bitchnigga*0xD9ED/0xD9ED)

    Pos	= Pos + 4;
    return (Z*16777216) + (Y*65536) + (X*256) + W;
end;

local function gBits8()
    local F = BitXOR(Byte(ByteString, Pos, Pos), 1*bitchnigga*2/2);
    Pos = Pos + 1;
    return F;
end;

local function gBits16()
    local W, X = Byte(ByteString, Pos, Pos + 2);

	W = BitXOR(W, bitchnigga)
	X = BitXOR(X, bitchnigga)

    Pos	= Pos + 2;
    return (X*256) + W;
end;

local function gFloat()
	local Left = gBits32();
	local Right = gBits32();
	local IsNormal = 1;
	local Mantissa = (gBit(Right, 1, 20) * (2 ^ 32))
					+ Left;
	local Exponent = gBit(Right, 21, 31);
	local Sign = ((-1) ^ gBit(Right, 32));
	if (Exponent == 0) then
		if (Mantissa == 0) then
			return Sign * 0; -- +-0
		else
			Exponent = 1;
			IsNormal = 0;
		end;
	elseif (Exponent == 2047) then
        return (Mantissa == 0) and (Sign * (1 / 0)) or (Sign * (0 / 0));
	end;
	return LDExp(Sign, Exponent - 1023) * (IsNormal + (Mantissa / (2 ^ 52)));
end;

local gSizet = gBits32;
local function gString(Len)
    local Str;
    if (not Len) then
        Len = gSizet();
        if (Len == 0) then
            return '';
        end;
    end;

    Str	= Sub(ByteString, Pos, Pos + Len - 1);
    Pos = Pos + Len;

	local FStr = {}
	for Idx = 1, #Str do
		FStr[Idx] = Char(BitXOR(Byte(Sub(Str, Idx, Idx)), 1*bitchnigga*2/2))
	end

    return Concat(FStr);
end;

local gInt = gBits32;
local function _R(...) return {...}, Select('#', ...) end
local function rsh(value,shift)
	return math.floor(value/2^shift) % 256
end

local function Deserialize()
    local Instrs = {};
    local Functions = {};
	local Lines = {};
    local Chunk = 
	{
		{[1]={[1]=Instrs}},
		{[1]={[1]=Functions}},
		nil,
		Lines,
        nil,
    
	};

	local ConstCount = gBits32()
    local Consts = {}

	for Idx=1, ConstCount do 
		local Type =gBits8();
		local Cons;
	
		if(Type==CONST_BOOL) then Cons = (gBits8() ~= 0);
		elseif(Type==CONST_FLOAT) then Cons = gFloat();
		elseif(Type==CONST_STRING) then Cons = gString();
		end;
		
		Consts[Idx] = Cons;
	end;
if game == nil or workspace == nil then
return ThisIsTehTable
end
";
		
		public static string VMP2 = @"

local function Wrap(BONANA, what, Chunk, Upvalues, Env, Boo, Cow)
	local Instr  = Chunk[1];
	local Proto  = Chunk[2];
	local Params = Chunk[3];

    hambanana = 'Cheese'

	return function(...) -- a
		local Instr  = Instr[1][1]; 
		local Proto  = Proto[1][1]; 
		local Params = Params;

		local _R = _R
		local InstrPoint = 1;
		local Top = -1;

		local Vararg = {};
		local Args	= {...};

		local PCount = Select('#', ...) - 1;

		local Lupvals	= {};
		local Stk		= {};

		for Idx = 0, PCount do -- b
			if (Idx >= Params) then
				Vararg[Idx - Params] = Args[Idx + 1];
			else
				Stk[Idx] = Args[Idx + 1];
			end;
		end;

		local Varargsz = PCount - Params + 1

		local Inst;
		local Enum;	

		while true do
			Inst		= Instr[InstrPoint];
			Enum		= Inst[D9_OP_ENUM];";

        public static string VMP3 = @"
			InstrPoint	= InstrPoint + 1;
		end;
    end;
end;	
local function donothing(e)end
Wrap(ThisIsTehTable, EEEE, Deserialize(), {}, GetFEnv(),'e','" + GetRandomAlphaNumString(1000) + @"')(); end 
Mocha(nil)";
		public static string VMP2_LI = @"
local PCall = pcall
local function Wrap(BONANA, what, Chunk, Upvalues, Env, Boo, Cow)
	local Instr = Chunk[1];
	local Proto = Chunk[2];
	local Params = Chunk[3];

    local hambanana = 'Cheese'

	return function(...)
		local InstrPoint = 1;
		local Top = -1;

		local Args = {...};
		local PCount = Select('#', ...) - 1;

		local function Loop()
			local Instr  = Instr; 
			local Const  = Const; 
			local Proto  = Proto; 
			local Params = Params;

			local _R = _R
			local Vararg = {};

			local Lupvals	= {};
			local Stk		= {};
	
			for Idx = 0, PCount do
				if (Idx >= Params) then
					Vararg[Idx - Params] = Args[Idx + 1];
				else
					Stk[Idx] = Args[Idx + 1];
				end;
			end;
	
			local Varargsz = PCount - Params + 1

			local Inst;
			local Enum;	

			while true do
				Inst		= Instr[InstrPoint];
				Enum		= Inst[D9_OP_ENUM];";
		
		public static string VMP3_LI = @"
				InstrPoint	= InstrPoint + 1;
			end;
		end;

		A, B = _R(PCall(Loop))
		if not A[1] then
			local line = Chunk[7][InstrPoint] or '?'
			error('ERROR IN IRONBREW SCRIPT [LINE ' .. line .. ']:' .. A[2])
		else
			return Unpack(A, 2, B)
		end;
	end;
end;	
Wrap(ThisIsTehTable, EEEE, Deserialize(), {}, GetFEnv(),'e','call')();
";
	}
}