#include "bg_lib.h"

int _main(int command, int arg0, int arg1);

int main(int command, int arg0, int arg1) {
    return _main(command, arg0, arg1);
}

typedef unsigned char uint8_t;
typedef char int8_t;
typedef int int32_t;
typedef short int16_t;
typedef unsigned int uint32_t;
typedef void* intptr_t;

typedef uint8_t byte;

typedef enum
{
    VM_NO_ERROR                    = 0,
    VM_INVALID_POINTER             = -1,
    VM_FAILED_TO_LOAD_BYTECODE     = -2,
    VM_NO_SYSCALL_CALLBACK         = -3,
    VM_FREE_ON_RUNNING_VM          = -4,
    VM_BLOCKCOPY_OUT_OF_RANGE      = -5,
    VM_PC_OUT_OF_RANGE             = -6,
    VM_JUMP_TO_INVALID_INSTRUCTION = -7,
    VM_STACK_OVERFLOW              = -8,
    VM_STACK_MISALIGNED            = -9,
    VM_OP_LOAD4_MISALIGNED         = -10,
    VM_STACK_ERROR                 = -11,
    VM_DATA_OUT_OF_RANGE           = -12,
    VM_MALLOC_FAILED               = -13,
    VM_BAD_INSTRUCTION             = -14,
    VM_NOT_LOADED                  = -15,
} vmErrorCode_t;

typedef enum
{
    VM_ALLOC_CODE_SEC             = 0,
    VM_ALLOC_DATA_SEC             = 1,
    VM_ALLOC_INSTRUCTION_POINTERS = 2,
    VM_ALLOC_DEBUG                = 3,
    VM_ALLOC_TYPE_MAX
} vmMallocType_t;

typedef struct
{
    int32_t vmMagic;
    int32_t instructionCount;
    int32_t codeOffset;
    int32_t codeLength;
    int32_t dataOffset;
    int32_t dataLength;
    int32_t litLength;
    int32_t bssLength;
} vmHeader_t;

typedef struct vmSymbol_s
{
    struct vmSymbol_s* next;

    int symValue;

    int  profileCount;
    char symName[1];

} vmSymbol_t;

typedef struct vm_s
{

    int programStack;

    intptr_t (*systemCall)(struct vm_s* vm, intptr_t* parms);

    char  name[64];
    void* searchPath;

    void* unused_dllHandle;
    intptr_t (*unused_entryPoint)(int callNum, ...);
    void (*unused_destroy)(struct vm_s* self);

    int currentlyInterpreting;

    int      compiled;
    uint8_t* codeBase;
    int      entryOfs;
    int      codeLength;

    intptr_t* instructionPointers;
    int       instructionCount;

    uint8_t* dataBase;
    int      dataMask;
    int      dataAlloc;

    int stackBottom;

    int         numSymbols;
    vmSymbol_t* symbols;

    int callLevel;
    int breakFunction;
    int breakCount;

    vmErrorCode_t lastError;
} vm_t;

typedef enum
{
    OP_UNDEF,

    OP_IGNORE,

    OP_BREAK,

    OP_ENTER,
    OP_LEAVE,
    OP_CALL,
    OP_PUSH,
    OP_POP,

    OP_CONST,
    OP_LOCAL,

    OP_JUMP,

    OP_EQ,
    OP_NE,

    OP_LTI,
    OP_LEI,
    OP_GTI,
    OP_GEI,

    OP_LTU,
    OP_LEU,
    OP_GTU,
    OP_GEU,

    OP_EQF,
    OP_NEF,

    OP_LTF,
    OP_LEF,
    OP_GTF,
    OP_GEF,

    OP_LOAD1,
    OP_LOAD2,
    OP_LOAD4,
    OP_STORE1,
    OP_STORE2,
    OP_STORE4,
    OP_ARG,

    OP_BLOCK_COPY,

    OP_SEX8,
    OP_SEX16,

    OP_NEGI,
    OP_ADD,
    OP_SUB,
    OP_DIVI,
    OP_DIVU,
    OP_MODI,
    OP_MODU,
    OP_MULI,
    OP_MULU,

    OP_BAND,
    OP_BOR,
    OP_BXOR,
    OP_BCOM,

    OP_LSH,
    OP_RSHI,
    OP_RSHU,

    OP_NEGF,
    OP_ADDF,
    OP_SUBF,
    OP_DIVF,
    OP_MULF,

    OP_CVIF,
    OP_CVFI,

    OP_MAX
} opcode_t;

int vm_debugLevel;

vmHeader_t* VM_LoadQVM(vm_t* vm, const uint8_t* bytecode, int length);
					
					
int VM_PrepareInterpreter(vm_t* vm, const vmHeader_t* header);

int VM_CallInterpreted(vm_t* vm, int* args);

void VM_BlockCopy(unsigned int dest, unsigned int src, size_t n, vm_t* vm);

int LittleEndianToHost(const uint8_t b[4]);

void Q_strncpyz(char* dest, const char* src, int destsize);

void printf(const char* fmt, ...) {
    va_list argptr;
    char text[1024];

    va_start(argptr, fmt);
    vsprintf(text, fmt, argptr);
    va_end(argptr);

    trap_Printf(text);
}

int VM_Create(vm_t* vm, const char* module, const uint8_t* bytecode, int length, intptr_t(*systemCalls)(vm_t*, intptr_t*));

void VM_Free(vm_t* vm);

intptr_t VM_Call(vm_t* vm, int command, ...);

void* VM_ArgPtr(intptr_t vmAddr, vm_t* vm);

float VM_IntToFloat(int32_t x);

int32_t VM_FloatToInt(float f);

int VM_MemoryRangeValid(intptr_t vmAddr, size_t len, const vm_t* vm);

void VM_VmProfile_f(const vm_t* vm);

void VM_Debug(int level);

void Com_Error(vmErrorCode_t level, const char* error) {
    // TODO
    printf("Com_Error: %s\n", error);
}

void* Com_malloc(size_t size, vm_t* vm, vmMallocType_t type) {
    // TODO
    // printf("TODO: Com_malloc\n");
    return malloc((int)size);
}

void Com_free(void* p, vm_t* vm, vmMallocType_t type) {
    // TODO
    printf("TODO: Com_free\n");
}


int VM_Create(vm_t* vm, const char* name, const uint8_t* bytecode, int length,
              intptr_t (*systemCalls)(vm_t*, intptr_t*))
{	
	vmHeader_t* header;

	if (vm == ((void*)0))
    {
        Com_Error(VM_INVALID_POINTER, "Invalid vm pointer");
        return -1;
    }
    if (!systemCalls)
    {
        vm->lastError = VM_NO_SYSCALL_CALLBACK;
        Com_Error(vm->lastError, "No systemcalls provided");
        return -1;
    }

	memset(vm, 0, sizeof(vm_t));
	Q_strncpyz(vm->name, name, sizeof(vm->name));
	

	header = VM_LoadQVM(vm, bytecode, length);
	if (!header)
	{
		vm->lastError = VM_FAILED_TO_LOAD_BYTECODE;
		Com_Error(vm->lastError, "Failed to load bytecode");
		VM_Free(vm);
		return -1;
	}

	vm->systemCall = systemCalls;

    vm->instructionCount    = header->instructionCount;
    vm->instructionPointers = (intptr_t*)Com_malloc(
        vm->instructionCount * sizeof(*vm->instructionPointers), vm,
        VM_ALLOC_INSTRUCTION_POINTERS);
    if (!vm->instructionPointers)
    {
        vm->lastError = VM_MALLOC_FAILED;
        Com_Error(vm->lastError,
                  "Instr. pointer malloc failed: out of memory?");
        VM_Free(vm);
        return -1;
    }

    vm->codeLength = header->codeLength;

    vm->compiled = 0;
    if (!vm->compiled)
    {
        if (VM_PrepareInterpreter(vm, header) != 0)
        {
            VM_Free(vm);
            return -1;
        }
    }

    vm->programStack = vm->dataMask + 1;
    vm->stackBottom  = vm->programStack - 0x10000;

    return 0;
}

union ughwtf_u {
	vmHeader_t* h;
	const uint8_t*    v;
};

vmHeader_t* VM_LoadQVM(vm_t* vm, const uint8_t* bytecode,
                                    int length)
{
    int dataLength;
    int i;
	
	union ughwtf_u header;
	header.v = bytecode;
	
    /*const union
    {
        const vmHeader_t* h;
        const uint8_t*    v;
    } header = { .v = bytecode };*/

    printf("Loading vm file %s...\n", vm->name);

    if (!header.h || !bytecode || length <= (int)sizeof(vmHeader_t) ||
        length > 0x400000)
    {
        printf("Failed. 0x000\n");
        return ((void*)0);
    }

    if (LittleEndianToHost((const uint8_t*)&(header.h->vmMagic)) == 0x12721444)
    {

        for (i = 0; i < (int)(sizeof(vmHeader_t)) / 4; i++)
        {
            ((int*)header.h)[i] =
                LittleEndianToHost((const uint8_t*)&(((int*)header.h)[i]));
        }

        if (header.h->bssLength < 0 || header.h->dataLength < 0 ||
            header.h->litLength < 0 || header.h->codeLength <= 0 ||
            header.h->codeOffset < 0 || header.h->dataOffset < 0 ||
            header.h->instructionCount <= 0 || header.h->bssLength > 10485760 ||
            header.h->codeOffset + header.h->codeLength > length ||
            header.h->dataOffset + header.h->dataLength + header.h->litLength >
                length)
        {
            printf("Warning: %s has bad header\n", vm->name);
            return ((void*)0);
        }
    }
    else
    {
        printf("Warning: Invalid magic number in header of \"%s\". "
               "Read: 0x%x, expected: 0x%x\n",
               vm->name,
               LittleEndianToHost((const uint8_t*)&(header.h->vmMagic)),
               0x12721444);
        return ((void*)0);
    }

    dataLength =
        header.h->dataLength + header.h->litLength + header.h->bssLength;
    for (i = 0; dataLength > (1 << i); i++)
    {
    }
    dataLength = 1 << i;

    vm->dataAlloc = dataLength + 4;
    vm->dataBase  = (uint8_t*)Com_malloc(vm->dataAlloc, vm, VM_ALLOC_DATA_SEC);
    vm->dataMask  = dataLength - 1;
    if (vm->dataBase == ((void*)0))
    {
        Com_Error(VM_MALLOC_FAILED, "Data malloc failed: out of memory?\n");
        return ((void*)0);
    }

    memset(vm->dataBase, 0, vm->dataAlloc);

    memcpy(vm->dataBase, header.v + header.h->dataOffset,
           header.h->dataLength + header.h->litLength);

    for (i = 0; i < header.h->dataLength; i += sizeof(int))
    {
        *(int*)(vm->dataBase + i) =
            LittleEndianToHost((const uint8_t*)&(*(int*)(vm->dataBase + i)));
    }

    return header.h;
}

intptr_t VM_Call(vm_t* vm, int command, ...)
{
    intptr_t r;
    int      args[13];
    va_list  ap;
    int      i;

    if (vm == ((void*)0))
    {
        Com_Error(VM_INVALID_POINTER, "VM_Call with NULL vm");
        return (intptr_t)-1;
    }
    if (vm->codeLength < 1)
    {
        vm->lastError = VM_NOT_LOADED;
        Com_Error(vm->lastError, "VM not loaded");
        return (intptr_t)-1;
    }

    args[0] = command;
    va_start(ap, command);
    for (i = 1; i < (int)(sizeof(args) / sizeof(*(args))); i++)
    {
        args[i] = va_arg(ap, int);
    }
    va_end(ap);

    ++vm->callLevel;
    r = (intptr_t)VM_CallInterpreted(vm, args);
    --vm->callLevel;

    return r;
}


void VM_Free(vm_t* vm)
{
    if (!vm)
    {
        return;
    }
    if (vm->callLevel)
    {
        vm->lastError = VM_FREE_ON_RUNNING_VM;
        Com_Error(vm->lastError, "VM_Free on running vm");
        return;
    }

    if (vm->codeBase)
    {
        Com_free(vm->codeBase, vm, VM_ALLOC_CODE_SEC);
        vm->codeBase = ((void*)0);
    }

    if (vm->dataBase)
    {
        Com_free(vm->dataBase, vm, VM_ALLOC_DATA_SEC);
        vm->dataBase = ((void*)0);
    }

    if (vm->instructionPointers)
    {
        Com_free(vm->instructionPointers, vm, VM_ALLOC_INSTRUCTION_POINTERS);
        vm->instructionPointers = ((void*)0);
    }

    memset(vm, 0, sizeof(*vm));
}


void* VM_ArgPtr(intptr_t vmAddr, vm_t* vm)
{
    if (!vmAddr)
    {
        return ((void*)0);
    }
    if (vm == ((void*)0))
    {
        Com_Error(VM_INVALID_POINTER, "Invalid VM pointer");
        return ((void*)0);
    }

    return (void*)(vm->dataBase + ((int)vmAddr & vm->dataMask));
}

float VM_IntToFloat(int32_t x)
{
    union
    {
        float    f;
        int32_t  i;
        uint32_t ui;
    } fi;
    fi.i = x;
    return fi.f;
}

int32_t VM_FloatToInt(float f)
{
    union
    {
        float    f;
        int32_t  i;
        uint32_t ui;
    } fi;
    fi.f = f;
    return fi.i;
}


int VM_MemoryRangeValid(intptr_t vmAddr, size_t len, const vm_t* vm)
{
	unsigned dest;
	unsigned dataMask;
	
    if (!vmAddr || !vm)
    {
        return -1;
    }
	
	dest     = (unsigned)vmAddr;
	dataMask = (unsigned)vm->dataMask;
	 
    if ((dest & dataMask) != dest || ((dest + len) & dataMask) != dest + len)
    {
        Com_Error(VM_DATA_OUT_OF_RANGE, "Memory access out of range");
        return -1;
    }
    else
    {
        return 0;
    }
}

void Q_strncpyz(char* dest, const char* src, int destsize)
{
    if (!dest || !src || destsize < 1)
    {
        return;
    }
    strncpy(dest, src, destsize - 1);
    dest[destsize - 1] = 0;
}


void VM_BlockCopy(unsigned int dest, unsigned int src, size_t n,
                         vm_t* vm)
{
    unsigned int dataMask = vm->dataMask;

    if ((dest & dataMask) != dest || (src & dataMask) != src ||
        ((dest + n) & dataMask) != dest + n ||
        ((src + n) & dataMask) != src + n)
    {
        Com_Error(vm->lastError = VM_BLOCKCOPY_OUT_OF_RANGE,
                  "OP_BLOCK_COPY out of range");
        return;
    }

    memcpy(vm->dataBase + dest, vm->dataBase + src, n);
}

int LittleEndianToHost(const uint8_t b[4])
{
    return (b[0] << 0) | (b[1] << 8) | (b[2] << 16) | (b[3] << 24);
}


int VM_PrepareInterpreter(vm_t* vm, const vmHeader_t* header)
{
    int      op;
    int      byte_pc;
    int      int_pc;
    uint8_t* code;
    int      instruction;
    int*     codeBase;

    vm->codeBase =
        (uint8_t*)Com_malloc(vm->codeLength * 4, vm, VM_ALLOC_CODE_SEC);
    if (!vm->codeBase)
    {
        Com_Error(VM_MALLOC_FAILED,
                  "Data pointer malloc failed: out of memory?");
        return -1;
    }

    memcpy(vm->codeBase, (uint8_t*)header + header->codeOffset, vm->codeLength);

    int_pc = byte_pc = 0;
    instruction      = 0;
    code             = (uint8_t*)header + header->codeOffset;
    codeBase         = (int*)vm->codeBase;

    while (instruction < header->instructionCount)
    {
        vm->instructionPointers[instruction] = (intptr_t)int_pc;
        instruction++;

        op               = (int)code[byte_pc];
        codeBase[int_pc] = op;
        if (byte_pc > header->codeLength)
        {
            Com_Error(vm->lastError = VM_PC_OUT_OF_RANGE,
                      "VM_PrepareInterpreter: pc > header->codeLength");
            return -1;
        }

        byte_pc++;
        int_pc++;

        switch (op)
        {
        case OP_ENTER:
        case OP_CONST:
        case OP_LOCAL:
        case OP_LEAVE:
        case OP_EQ:
        case OP_NE:
        case OP_LTI:
        case OP_LEI:
        case OP_GTI:
        case OP_GEI:
        case OP_LTU:
        case OP_LEU:
        case OP_GTU:
        case OP_GEU:
        case OP_EQF:
        case OP_NEF:
        case OP_LTF:
        case OP_LEF:
        case OP_GTF:
        case OP_GEF:
        case OP_BLOCK_COPY:
            codeBase[int_pc] = LittleEndianToHost(&code[byte_pc]);
            byte_pc += 4;
            int_pc++;
            break;
        case OP_ARG:
            codeBase[int_pc] = (int)code[byte_pc];
            byte_pc++;
            int_pc++;
            break;
        default:
            if (op < 0 || op >= OP_MAX)
            {
                vm->lastError = VM_BAD_INSTRUCTION;
                Com_Error(vm->lastError, "Bad VM instruction");
                return -1;
            }
            break;
        }
    }
    int_pc      = 0;
    instruction = 0;

    while (instruction < header->instructionCount)
    {
        op = codeBase[int_pc];
        instruction++;
        int_pc++;

        switch (op)
        {

        case OP_EQ:
        case OP_NE:
        case OP_LTI:
        case OP_LEI:
        case OP_GTI:
        case OP_GEI:
        case OP_LTU:
        case OP_LEU:
        case OP_GTU:
        case OP_GEU:
        case OP_EQF:
        case OP_NEF:
        case OP_LTF:
        case OP_LEF:
        case OP_GTF:
        case OP_GEF:
            if (codeBase[int_pc] < 0 || codeBase[int_pc] > vm->instructionCount)
            {
                Com_Error(vm->lastError = VM_JUMP_TO_INVALID_INSTRUCTION,
                          "VM_PrepareInterpreter: Jump to invalid "
                          "instruction number");
                return -1;
            }

            codeBase[int_pc] = (int)vm->instructionPointers[codeBase[int_pc]];
            int_pc++;
            break;

        case OP_ENTER:
        case OP_CONST:
        case OP_LOCAL:
        case OP_LEAVE:
        case OP_BLOCK_COPY:
        case OP_ARG:
            int_pc++;
            break;

        default:
            break;
        }
    }
    return 0;
}


int VM_CallInterpreted(vm_t* vm, int* args)
{
    uint8_t  stack[1024 + 15];
    int*     opStack;
    uint8_t  opStackOfs;
    int      programCounter;
    int      programStack;
    int      stackOnEntry;
    uint8_t* image;
    int*     codeImage;
    int      v1;
    int      dataMask;
    int      arg;
    int opcode, r0, r1;
	
    vm->currentlyInterpreting = 1;

    programStack = stackOnEntry = vm->programStack;

    image          = vm->dataBase;
    codeImage      = (int*)vm->codeBase;
    dataMask       = vm->dataMask;
    programCounter = 0;
    programStack -= (8 + 4 * 13);

    for (arg = 0; arg < 13; arg++)
    {
        *(int*)&image[programStack + 8 + arg * 4] = args[arg];
    }

    *(int*)&image[programStack + 4] = 0;
    *(int*)&image[programStack]     = -1;

    // opStack    = ((void*)((((intptr_t)(stack)) + ((16)) - 1) & ~(((16)) - 1)));
	opStack = (int*)stack;
    *opStack   = 0x0000BEEF;
    opStackOfs = 0;

    //int opcode, r0, r1;

    while (1)
    {

    nextInstruction:
        r0 = opStack[opStackOfs];
        r1 = opStack[(uint8_t)(opStackOfs - 1)];
    nextInstruction2:
        opcode = codeImage[programCounter++];

        switch (opcode)

        {

        case OP_UNDEF:
            Com_Error(vm->lastError = VM_BAD_INSTRUCTION, "Bad VM instruction");
            return -1;
        case OP_IGNORE:
            goto nextInstruction2;
        case OP_BREAK:
            vm->breakCount++;
            goto nextInstruction2;
        case OP_CONST:
            opStackOfs++;
            r1 = r0;
            r0 = opStack[opStackOfs] = codeImage[programCounter];

            programCounter += 1;
            goto nextInstruction2;
        case OP_LOCAL:
            opStackOfs++;
            r1 = r0;
            r0 = opStack[opStackOfs] = codeImage[programCounter] + programStack;

            programCounter += 1;
            goto nextInstruction2;
        case OP_LOAD4:

            r0 = opStack[opStackOfs] = *(int*)&image[r0 & dataMask];
            goto nextInstruction2;
        case OP_LOAD2:
            r0 = opStack[opStackOfs] = *(unsigned short*)&image[r0 & dataMask];
            goto nextInstruction2;
        case OP_LOAD1:
            r0 = opStack[opStackOfs] = image[r0 & dataMask];
            goto nextInstruction2;

        case OP_STORE4:
            *(int*)&image[r1 & dataMask] = r0;
            opStackOfs -= 2;
            goto nextInstruction;
        case OP_STORE2:
            *(short*)&image[r1 & dataMask] = r0;
            opStackOfs -= 2;
            goto nextInstruction;
        case OP_STORE1:
            image[r1 & dataMask] = r0;
            opStackOfs -= 2;
            goto nextInstruction;
        case OP_ARG:

            *(int*)&image[(codeImage[programCounter] + programStack) &
                          dataMask] = r0;
            opStackOfs--;
            programCounter += 1;
            goto nextInstruction;
        case OP_BLOCK_COPY:
            VM_BlockCopy(r1, r0, codeImage[programCounter], vm);
            programCounter += 1;
            opStackOfs -= 2;
            goto nextInstruction;
        case OP_CALL:

            *(int*)&image[programStack] = programCounter;

            programCounter = r0;
            opStackOfs--;
            if (programCounter < 0)
            {
                int r;

                vm->programStack = programStack - 4;

                *(int*)&image[programStack + 4] = -1 - programCounter;

                if (sizeof(intptr_t) != sizeof(int))
                {
                    intptr_t argarr[16];
                    int*     imagePtr = (int*)&image[programStack];
                    int      i;
                    for (i = 0; i < (int)(sizeof(argarr) / sizeof(*(argarr)));
                         ++i)
                    {
                        argarr[i] = (intptr_t) *(++imagePtr);
                    }
                    r = (int)vm->systemCall(vm, argarr);
                }
                else
                {
                    r = (int)vm->systemCall(vm, (intptr_t*)&image[programStack + 4]);
                }

                opStackOfs++;
                opStack[opStackOfs] = r;
                programCounter      = *(int*)&image[programStack];
            }
            else if ((unsigned)programCounter >= (unsigned)vm->instructionCount)
            {
                vm->lastError = VM_PC_OUT_OF_RANGE;
                Com_Error(vm->lastError,
                          "VM program counter out of range in OP_CALL");
                return -1;
            }
            else
            {
                programCounter = (int)vm->instructionPointers[programCounter];
            }
            goto nextInstruction;

        case OP_PUSH:
            opStackOfs++;
            goto nextInstruction;
        case OP_POP:
            opStackOfs--;
            goto nextInstruction;
        case OP_ENTER:

            v1 = codeImage[programCounter];

            programCounter += 1;
            programStack -= v1;

            goto nextInstruction;
        case OP_LEAVE:

            v1 = codeImage[programCounter];

            programStack += v1;

            programCounter = *(int*)&image[programStack];

            if (programCounter == -1)
            {
                goto done;
            }
            else if ((unsigned)programCounter >= (unsigned)vm->codeLength)
            {
                Com_Error(vm->lastError = VM_PC_OUT_OF_RANGE,
                          "VM program counter out of range in OP_LEAVE");
                return -1;
            }
            goto nextInstruction;

        case OP_JUMP:
            if ((unsigned)r0 >= (unsigned)vm->instructionCount)
            {
                Com_Error(vm->lastError = VM_PC_OUT_OF_RANGE,
                          "VM program counter out of range in OP_JUMP");
                return -1;
            }

            programCounter = (int)vm->instructionPointers[r0];

            opStackOfs--;
            goto nextInstruction;
        case OP_EQ:
            opStackOfs -= 2;
            if (r1 == r0)
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_NE:
            opStackOfs -= 2;
            if (r1 != r0)
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_LTI:
            opStackOfs -= 2;
            if (r1 < r0)
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_LEI:
            opStackOfs -= 2;
            if (r1 <= r0)
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_GTI:
            opStackOfs -= 2;
            if (r1 > r0)
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_GEI:
            opStackOfs -= 2;
            if (r1 >= r0)
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_LTU:
            opStackOfs -= 2;
            if (((unsigned)r1) < ((unsigned)r0))
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_LEU:
            opStackOfs -= 2;
            if (((unsigned)r1) <= ((unsigned)r0))
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_GTU:
            opStackOfs -= 2;
            if (((unsigned)r1) > ((unsigned)r0))
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_GEU:
            opStackOfs -= 2;
            if (((unsigned)r1) >= ((unsigned)r0))
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_EQF:
            opStackOfs -= 2;

            if (((float*)opStack)[(uint8_t)(opStackOfs + 1)] ==
                ((float*)opStack)[(uint8_t)(opStackOfs + 2)])
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_NEF:
            opStackOfs -= 2;

            if (((float*)opStack)[(uint8_t)(opStackOfs + 1)] !=
                ((float*)opStack)[(uint8_t)(opStackOfs + 2)])
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_LTF:
            opStackOfs -= 2;

            if (((float*)opStack)[(uint8_t)(opStackOfs + 1)] <
                ((float*)opStack)[(uint8_t)(opStackOfs + 2)])
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_LEF:
            opStackOfs -= 2;

            if (((float*)opStack)[(uint8_t)((uint8_t)(opStackOfs + 1))] <=
                ((float*)opStack)[(uint8_t)((uint8_t)(opStackOfs + 2))])
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_GTF:
            opStackOfs -= 2;

            if (((float*)opStack)[(uint8_t)(opStackOfs + 1)] >
                ((float*)opStack)[(uint8_t)(opStackOfs + 2)])
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }
        case OP_GEF:
            opStackOfs -= 2;

            if (((float*)opStack)[(uint8_t)(opStackOfs + 1)] >=
                ((float*)opStack)[(uint8_t)(opStackOfs + 2)])
            {
                programCounter = codeImage[programCounter];
                goto nextInstruction;
            }
            else
            {
                programCounter += 1;
                goto nextInstruction;
            }

        case OP_NEGI:
            opStack[opStackOfs] = -r0;
            goto nextInstruction;
        case OP_ADD:
            opStackOfs--;
            opStack[opStackOfs] = r1 + r0;
            goto nextInstruction;
        case OP_SUB:
            opStackOfs--;
            opStack[opStackOfs] = r1 - r0;
            goto nextInstruction;
        case OP_DIVI:
            opStackOfs--;
            opStack[opStackOfs] = r1 / r0;
            goto nextInstruction;
        case OP_DIVU:
            opStackOfs--;
            opStack[opStackOfs] = ((unsigned)r1) / ((unsigned)r0);
            goto nextInstruction;
        case OP_MODI:
            opStackOfs--;
            opStack[opStackOfs] = r1 % r0;
            goto nextInstruction;
        case OP_MODU:
            opStackOfs--;
            opStack[opStackOfs] = ((unsigned)r1) % ((unsigned)r0);
            goto nextInstruction;
        case OP_MULI:
            opStackOfs--;
            opStack[opStackOfs] = r1 * r0;
            goto nextInstruction;
        case OP_MULU:
            opStackOfs--;
            opStack[opStackOfs] = ((unsigned)r1) * ((unsigned)r0);
            goto nextInstruction;
        case OP_BAND:
            opStackOfs--;
            opStack[opStackOfs] = ((unsigned)r1) & ((unsigned)r0);
            goto nextInstruction;
        case OP_BOR:
            opStackOfs--;
            opStack[opStackOfs] = ((unsigned)r1) | ((unsigned)r0);
            goto nextInstruction;
        case OP_BXOR:
            opStackOfs--;
            opStack[opStackOfs] = ((unsigned)r1) ^ ((unsigned)r0);
            goto nextInstruction;
        case OP_BCOM:
            opStack[opStackOfs] = ~((unsigned)r0);
            goto nextInstruction;
        case OP_LSH:
            opStackOfs--;
            opStack[opStackOfs] = r1 << r0;
            goto nextInstruction;
        case OP_RSHI:
            opStackOfs--;
            opStack[opStackOfs] = r1 >> r0;
            goto nextInstruction;
        case OP_RSHU:
            opStackOfs--;
            opStack[opStackOfs] = ((unsigned)r1) >> r0;
            goto nextInstruction;
        case OP_NEGF:
            ((float*)opStack)[opStackOfs] = -((float*)opStack)[opStackOfs];
            goto nextInstruction;
        case OP_ADDF:
            opStackOfs--;
            ((float*)opStack)[opStackOfs] =
                ((float*)opStack)[opStackOfs] +
                ((float*)opStack)[(uint8_t)(opStackOfs + 1)];
            goto nextInstruction;
        case OP_SUBF:
            opStackOfs--;
            ((float*)opStack)[opStackOfs] =
                ((float*)opStack)[opStackOfs] -
                ((float*)opStack)[(uint8_t)(opStackOfs + 1)];
            goto nextInstruction;
        case OP_DIVF:
            opStackOfs--;
            ((float*)opStack)[opStackOfs] =
                ((float*)opStack)[opStackOfs] /
                ((float*)opStack)[(uint8_t)(opStackOfs + 1)];
            goto nextInstruction;
        case OP_MULF:
            opStackOfs--;
            ((float*)opStack)[opStackOfs] =
                ((float*)opStack)[opStackOfs] *
                ((float*)opStack)[(uint8_t)(opStackOfs + 1)];
            goto nextInstruction;
        case OP_CVIF:
            ((float*)opStack)[opStackOfs] = (float)opStack[opStackOfs];
            goto nextInstruction;
        case OP_CVFI:
            opStack[opStackOfs] = ((long)(((float*)opStack)[opStackOfs]));
            goto nextInstruction;
        case OP_SEX8:
            opStack[opStackOfs] = (int8_t)opStack[opStackOfs];
            goto nextInstruction;
        case OP_SEX16:
            opStack[opStackOfs] = (int16_t)opStack[opStackOfs];
            goto nextInstruction;
        }
    }

done:
    vm->currentlyInterpreting = 0;

    if (opStackOfs != 1 || *opStack != 0x0000BEEF)
    {
        Com_Error(vm->lastError = VM_STACK_ERROR, "Interpreter stack error");
    }

    vm->programStack = stackOnEntry;

    return opStack[opStackOfs];
}

void VM_Debug(int level)
{
    vm_debugLevel = level;
}

void VM_VmProfile_f(const vm_t* vm)
{
    (void)vm;
}


intptr_t syscalls_func(vm_t* vm, intptr_t* args) {
    const int id = -1 - ((int)args[0]);
    char* str;

    switch (id) {
        case -1:
            str = (char*)VM_ArgPtr(args[1], vm);
            printf("%s", str);
            break;

        case -69:
            printf("Nice.\n");
            break;

        default:
            printf("Bad system call: %i\n", id);
            break;
    }

    return (intptr_t)0;
}

int _main(int command, int arg0, int arg1) {
    vm_t vm;
    int result;

    VM_Create(&vm, "Que?", (byte*)arg0, arg1, syscalls_func);
    VM_Call(&vm, 0);
    result = (int)VM_Call(&vm, 1);

    printf("Que? resulted in = %i\n", result);
    return 789;
}
