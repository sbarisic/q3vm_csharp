using System;
using System.Runtime.InteropServices;

// https://www.icculus.org/~phaethon/q3mc/q3vm_specs.html

namespace Q3VM2 {
    enum vmErrorCode_t {
        VM_NO_ERROR = 0,
        VM_INVALID_POINTER = -1,
        VM_FAILED_TO_LOAD_BYTECODE = -2,
        VM_NO_SYSCALL_CALLBACK = -3,
        VM_FREE_ON_RUNNING_VM = -4,
        VM_BLOCKCOPY_OUT_OF_RANGE = -5,
        VM_PC_OUT_OF_RANGE = -6,
        VM_JUMP_TO_INVALID_INSTRUCTION = -7,
        VM_STACK_OVERFLOW = -8,
        VM_STACK_MISALIGNED = -9,
        VM_OP_LOAD4_MISALIGNED = -10,
        VM_STACK_ERROR = -11,
        VM_DATA_OUT_OF_RANGE = -12,
        VM_MALLOC_FAILED = -13,
        VM_BAD_INSTRUCTION = -14,
        VM_NOT_LOADED = -15,
    }

    enum vmMallocType_t {
        VM_ALLOC_CODE_SEC = 0,
        VM_ALLOC_DATA_SEC = 1,
        VM_ALLOC_INSTRUCTION_POINTERS = 2,
        VM_ALLOC_DEBUG = 3,
        VM_ALLOC_TYPE_MAX
    }

    struct vmHeader_t {
        public int vmMagic;
        public int instructionCount;
        public int codeOffset;
        public int codeLength;
        public int dataOffset;
        public int dataLength;
        public int litLength;
        public int bssLength;
    }

    unsafe struct vmSymbol_t {
        public vmSymbol_t* next;

        public int symValue;

        public int profileCount;
        public fixed char symName[1];
    }

    [StructLayout(LayoutKind.Explicit)]
    struct num_union {
        [FieldOffset(0)] public float f;
        [FieldOffset(0)] public int i;
        [FieldOffset(0)] public uint ui;
    }

    [StructLayout(LayoutKind.Explicit)]
    unsafe struct header_union {
        [FieldOffset(0)] public vmHeader_t* h;
        [FieldOffset(0)] public byte* v;
    }

    unsafe delegate IntPtr systemCallFunc(ref VirtMachine vm, params IntPtr[] parms);

    enum opcode_t {
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
    }

    unsafe struct VirtMachine {
        public int programStack;

        // IntPtr(*systemCall)(ref vm_t vm, IntPtr* parms);
        public systemCallFunc systemCall;


        //public fixed char name[64];
        public string Name;

        public void* searchPath;

        // void* unused_dllHandle;
        // IntPtr(*unused_entryPoint)(int callNum, ...);
        // void (* unused_destroy) (ref vm_t self);

        public int currentlyInterpreting;

        public int compiled;
        public byte* codeBase;
        public int entryOfs;
        public int codeLength;

        public IntPtr* instructionPointers;
        public int instructionCount;

        public byte* dataBase;
        public int dataMask;
        public int dataAlloc;

        public int dataMallocLen;
        public int dataMallocStart;

        public int stackBottom;

        public int numSymbols;
        public vmSymbol_t* symbols;

        public int callLevel;
        public int breakFunction;
        public int breakCount;

        public vmErrorCode_t lastError;
    }

    unsafe static class VM {

        static int vm_debugLevel;

        static void Com_Error(vmErrorCode_t level, string error) {
            string msg = string.Format("{0} - {1}", level, error);
            Console.WriteLine(msg);
            throw new Exception(msg);
        }

        static void Warn(string str, params object[] args) {
            // todo
            // Console.Write(string.Format(str, args));
        }

        public static void memset(void* str, int c, uint n) {
            for (int i = 0; i < n; i++)
                ((byte*)str)[i] = (byte)c;
        }

        public static void memcpy(void* dest, void* src, uint n) {
            byte* dest_b = (byte*)dest;
            byte* src_b = (byte*)src;

            for (int i = 0; i < n; i++)
                dest_b[i] = src_b[i];
        }

        public static void memcpy(void* dest, byte[] src, uint n) {
            byte* dest_b = (byte*)dest;

            for (int i = 0; i < n; i++)
                dest_b[i] = src[i];
        }

        static void strncpy(char* dest, char* src, uint n) {
            throw new Exception();
        }

        static void* Com_malloc2(byte[] Data) {
            byte* DataPtr = (byte*)Marshal.AllocHGlobal(Data.Length);
            Marshal.Copy(Data, 0, (IntPtr)DataPtr, Data.Length);
            return (void*)DataPtr;
        }

        static void* Com_malloc(uint size, ref VirtMachine vm, vmMallocType_t type) {
            return (void*)Marshal.AllocHGlobal((int)size);
        }

        static void Com_free(void* p, ref VirtMachine vm, vmMallocType_t type) {
            Marshal.FreeHGlobal((IntPtr)p);
        }

        public static bool VM_Create(ref VirtMachine vm, string Name, byte[] Bytecode, systemCallFunc systemCalls) {
            int length = Bytecode.Length;
            byte* bytecode = (byte*)Com_malloc2(Bytecode);

            // TODO
            /*if (vm == null) {
				Com_Error(vmErrorCode_t.VM_INVALID_POINTER, "Invalid vm pointer");
				return -1;
			}*/

            if (systemCalls == null) {
                vm.lastError = vmErrorCode_t.VM_NO_SYSCALL_CALLBACK;
                Com_Error(vm.lastError, "No systemcalls provided");
                return false;
            }



            //memset(vm, 0, (uint)sizeof(vm_t));
            //Q_strncpyz(vm.name, name, sizeof(vm.name));
            vm.Name = Name;

            vmHeader_t* header = VM_LoadQVM(ref vm, bytecode, length);

            if (header == null) {
                vm.lastError = vmErrorCode_t.VM_FAILED_TO_LOAD_BYTECODE;
                Com_Error(vm.lastError, "Failed to load bytecode");
                VM_Free(ref vm);
                return false;
            }

            vm.systemCall = systemCalls;

            vm.instructionCount = header->instructionCount;
            vm.instructionPointers = (IntPtr*)Com_malloc((uint)(vm.instructionCount * IntPtr.Size), ref vm, vmMallocType_t.VM_ALLOC_INSTRUCTION_POINTERS);
            if (vm.instructionPointers == null) {
                vm.lastError = vmErrorCode_t.VM_MALLOC_FAILED;
                Com_Error(vm.lastError,
                          "Instr. pointer malloc failed: out of memory?");
                VM_Free(ref vm);
                return false;
            }

            vm.codeLength = header->codeLength;

            vm.compiled = 0;
            if (vm.compiled == 0) {
                if (VM_PrepareInterpreter(ref vm, header) != 0) {
                    VM_Free(ref vm);
                    return false;
                }
            }

            vm.programStack = vm.dataMask + 1;
            vm.stackBottom = vm.programStack - 0x10000;

            return true;
        }

        static vmHeader_t* VM_LoadQVM(ref VirtMachine vm, byte* bytecode, int length) {

            int dataLength;
            int i;

            header_union header = new header_union();
            header.v = bytecode;

            /*const union  {
				const vmHeader_t* h;
				const byte* v;
			} header = { .v = bytecode };*/

            Warn("Loading vm file {0}...\n", vm.Name);

            if (header.h == null || bytecode == null || length <= (int)sizeof(vmHeader_t) || length > 0x400000) {
                Warn("Failed.\n");

                return null;
            }

            if (LittleEndianToHost((byte*)&(header.h->vmMagic)) == 0x12721444) {

                for (i = 0; i < (int)(sizeof(vmHeader_t)) / 4; i++) {
                    ((int*)header.h)[i] = LittleEndianToHost((byte*)&(((int*)header.h)[i]));
                }

                if (header.h->bssLength < 0 || header.h->dataLength < 0 ||
                    header.h->litLength < 0 || header.h->codeLength <= 0 ||
                    header.h->codeOffset < 0 || header.h->dataOffset < 0 ||
                    header.h->instructionCount <= 0 || header.h->bssLength > 10485760 ||
                    header.h->codeOffset + header.h->codeLength > length ||
                    header.h->dataOffset + header.h->dataLength + header.h->litLength > length) {

                    Warn("Warning: {0} has bad header\n", vm.Name);
                    return null;
                }
            } else {
                Warn("Warning: Invalid magic number in header of \"{0}\". \n\nRead: {1}, expected: {2}\n", vm.Name, LittleEndianToHost((byte*)&(header.h->vmMagic)), 0x12721444);
                return null;
            }

            vm.dataMallocLen = 1024 * 150;
            dataLength = header.h->dataLength + header.h->litLength + header.h->bssLength;

            vm.dataMallocStart = dataLength + 16;
            dataLength += vm.dataMallocLen;

            for (i = 0; dataLength > (1 << i); i++) {
            }

            dataLength = 1 << i;

            vm.dataAlloc = dataLength + 4;
            vm.dataBase = (byte*)Com_malloc((uint)(vm.dataAlloc ), ref vm, vmMallocType_t.VM_ALLOC_DATA_SEC);
            vm.dataMask = dataLength - 1;
            if (vm.dataBase == null) {
                Com_Error(vmErrorCode_t.VM_MALLOC_FAILED, "Data malloc failed: out of memory?\n");
                return null;
            }



            memset(vm.dataBase, 0, (uint)vm.dataAlloc);
            memcpy(vm.dataBase, header.v + header.h->dataOffset, (uint)(header.h->dataLength + header.h->litLength));

            for (i = 0; i < header.h->dataLength; i += sizeof(int)) {
                *(int*)(vm.dataBase + i) =
                    LittleEndianToHost((byte*)&(*(int*)(vm.dataBase + i)));
            }

            return header.h;
        }

        public static IntPtr VM_Call(ref VirtMachine vm, int command, params int[] command_args) {
            // TODO
            /*if (vm == null) {
				Com_Error(vmErrorCode_t.VM_INVALID_POINTER, "VM_Call with NULL vm");
				return (IntPtr)(-1);
			}*/

            if (vm.codeLength < 1) {
                vm.lastError = vmErrorCode_t.VM_NOT_LOADED;
                Com_Error(vm.lastError, "VM not loaded");
                return (IntPtr)(-1);
            }

            int* args = stackalloc int[13];
            args[0] = command;

            if (command_args != null) {
                for (int i = 1; i < command_args.Length + 1; i++)
                    args[i] = command_args[i - 1];
            }


            ++vm.callLevel;
            IntPtr r = (IntPtr)VM_CallInterpreted(ref vm, args);
            --vm.callLevel;

            return r;
        }

        public static void VM_Free(ref VirtMachine vm) {
            // TODO
            /*if (vm == null) {
				return;
			}*/

            if (vm.callLevel != 0) {
                vm.lastError = vmErrorCode_t.VM_FREE_ON_RUNNING_VM;
                Com_Error(vm.lastError, "VM_Free on running vm");
                return;
            }

            if (vm.codeBase != null) {
                Com_free(vm.codeBase, ref vm, vmMallocType_t.VM_ALLOC_CODE_SEC);
                vm.codeBase = null;
            }

            if (vm.dataBase != null) {
                Com_free(vm.dataBase, ref vm, vmMallocType_t.VM_ALLOC_DATA_SEC);
                vm.dataBase = null;
            }

            if (vm.instructionPointers != null) {
                Com_free(vm.instructionPointers, ref vm, vmMallocType_t.VM_ALLOC_INSTRUCTION_POINTERS);
                vm.instructionPointers = null;
            }

            // TODO: Clear vm
            //memset(vm, 0, sizeof(*vm));
        }

        static void* VM_ArgPtr(IntPtr vmAddr, ref VirtMachine vm) {
            if (vmAddr == null) {
                return null;
            }

            // TODO
            /*if (vm == null) {
				Com_Error(vmErrorCode_t.VM_INVALID_POINTER, "Invalid VM pointer");
				return null;
			}*/

            return (void*)(vm.dataBase + ((int)vmAddr & vm.dataMask));
        }

        static float VM_IntToFloat(int x) {
            num_union fi = new num_union() { i = x };
            return fi.f;
        }

        static int VM_FloatToInt(float f) {
            num_union fi = new num_union() { f = f };
            return fi.i;
        }

        public static int VM_MemoryRangeValid(IntPtr vmAddr, uint len, ref VirtMachine vm) {
            if (vmAddr == null) {
                return -1;
            }

            uint dest = (uint)vmAddr;
            uint dataMask = (uint)vm.dataMask;

            if ((dest & dataMask) != dest || ((dest + len) & dataMask) != dest + len) {
                Com_Error(vmErrorCode_t.VM_DATA_OUT_OF_RANGE, "Memory access out of range");
                return -1;
            } else {
                return 0;
            }
        }

        static void Q_strncpyz(char* dest, char* src, int destsize) {
            if (dest == null || src == null || destsize < 1) {
                return;
            }

            strncpy(dest, src, (uint)(destsize - 1));
            dest[destsize - 1] = (char)0;
        }

        static void VM_BlockCopy(uint dest, uint src, uint n, ref VirtMachine vm) {
            uint dataMask = (uint)vm.dataMask;

            if ((dest & dataMask) != dest || (src & dataMask) != src ||
                ((dest + n) & dataMask) != dest + n ||
                ((src + n) & dataMask) != src + n) {
                Com_Error(vm.lastError = vmErrorCode_t.VM_BLOCKCOPY_OUT_OF_RANGE,
                          "OP_BLOCK_COPY out of range");
                return;
            }

            memcpy(vm.dataBase + dest, vm.dataBase + src, n);
        }

        // b is 4 bytes always
        static int LittleEndianToHost(byte* b) {
            return (b[0] << 0) | (b[1] << 8) | (b[2] << 16) | (b[3] << 24);
        }

        static int VM_PrepareInterpreter(ref VirtMachine vm, vmHeader_t* header) {
            int op;
            int byte_pc;
            int int_pc;
            byte* code;
            int instruction;
            int* codeBase;

            vm.codeBase = (byte*)Com_malloc((uint)(vm.codeLength * 4), ref vm, vmMallocType_t.VM_ALLOC_CODE_SEC);

            if (vm.codeBase == null) {
                Com_Error(vmErrorCode_t.VM_MALLOC_FAILED,
                          "Data pointer malloc failed: out of memory?");
                return -1;
            }

            memcpy(vm.codeBase, (byte*)header + header->codeOffset, (uint)vm.codeLength);

            int_pc = byte_pc = 0;
            instruction = 0;
            code = (byte*)header + header->codeOffset;
            codeBase = (int*)vm.codeBase;

            while (instruction < header->instructionCount) {
                vm.instructionPointers[instruction] = (IntPtr)int_pc;
                instruction++;

                op = (int)code[byte_pc];
                codeBase[int_pc] = op;
                if (byte_pc > header->codeLength) {
                    Com_Error(vm.lastError = vmErrorCode_t.VM_PC_OUT_OF_RANGE,
                              "VM_PrepareInterpreter: pc > header->codeLength");
                    return -1;
                }

                byte_pc++;
                int_pc++;

                switch ((opcode_t)op) {
                    case opcode_t.OP_ENTER:
                    case opcode_t.OP_CONST:
                    case opcode_t.OP_LOCAL:
                    case opcode_t.OP_LEAVE:
                    case opcode_t.OP_EQ:
                    case opcode_t.OP_NE:
                    case opcode_t.OP_LTI:
                    case opcode_t.OP_LEI:
                    case opcode_t.OP_GTI:
                    case opcode_t.OP_GEI:
                    case opcode_t.OP_LTU:
                    case opcode_t.OP_LEU:
                    case opcode_t.OP_GTU:
                    case opcode_t.OP_GEU:
                    case opcode_t.OP_EQF:
                    case opcode_t.OP_NEF:
                    case opcode_t.OP_LTF:
                    case opcode_t.OP_LEF:
                    case opcode_t.OP_GTF:
                    case opcode_t.OP_GEF:
                    case opcode_t.OP_BLOCK_COPY:
                        codeBase[int_pc] = LittleEndianToHost(&code[byte_pc]);
                        byte_pc += 4;
                        int_pc++;
                        break;
                    case opcode_t.OP_ARG:
                        codeBase[int_pc] = (int)code[byte_pc];
                        byte_pc++;
                        int_pc++;
                        break;
                    default:
                        if (op < 0 || op >= (int)opcode_t.OP_MAX) {
                            vm.lastError = vmErrorCode_t.VM_BAD_INSTRUCTION;
                            Com_Error(vm.lastError, "Bad VM instruction");
                            return -1;
                        }
                        break;
                }
            }
            int_pc = 0;
            instruction = 0;

            while (instruction < header->instructionCount) {
                op = codeBase[int_pc];
                instruction++;
                int_pc++;

                switch ((opcode_t)op) {

                    case opcode_t.OP_EQ:
                    case opcode_t.OP_NE:
                    case opcode_t.OP_LTI:
                    case opcode_t.OP_LEI:
                    case opcode_t.OP_GTI:
                    case opcode_t.OP_GEI:
                    case opcode_t.OP_LTU:
                    case opcode_t.OP_LEU:
                    case opcode_t.OP_GTU:
                    case opcode_t.OP_GEU:
                    case opcode_t.OP_EQF:
                    case opcode_t.OP_NEF:
                    case opcode_t.OP_LTF:
                    case opcode_t.OP_LEF:
                    case opcode_t.OP_GTF:
                    case opcode_t.OP_GEF:
                        if (codeBase[int_pc] < 0 || codeBase[int_pc] > vm.instructionCount) {
                            Com_Error(vm.lastError = vmErrorCode_t.VM_JUMP_TO_INVALID_INSTRUCTION,
                                      "VM_PrepareInterpreter: Jump to invalid instruction number");
                            return -1;
                        }

                        codeBase[int_pc] = (int)vm.instructionPointers[codeBase[int_pc]];
                        int_pc++;
                        break;

                    case opcode_t.OP_ENTER:
                    case opcode_t.OP_CONST:
                    case opcode_t.OP_LOCAL:
                    case opcode_t.OP_LEAVE:
                    case opcode_t.OP_BLOCK_COPY:
                    case opcode_t.OP_ARG:
                        int_pc++;
                        break;

                    default:
                        break;
                }
            }
            return 0;
        }

        static int VM_CallInterpreted(ref VirtMachine vm, int* args) {
            //byte stack[1024 + 15];
            byte* stack = stackalloc byte[1024 + 15];

            int* opStack;
            byte opStackOfs;
            int programCounter;
            int programStack;
            int stackOnEntry;
            byte* image;
            int* codeImage;
            int v1;
            int dataMask;
            int arg;

            vm.currentlyInterpreting = 1;

            programStack = stackOnEntry = vm.programStack;

            image = vm.dataBase;
            codeImage = (int*)vm.codeBase;
            dataMask = vm.dataMask;
            programCounter = 0;
            programStack -= (8 + 4 * 13);

            for (arg = 0; arg < 13; arg++) {
                *(int*)&image[programStack + 8 + arg * 4] = args[arg];
            }

            *(int*)&image[programStack + 4] = 0;
            *(int*)&image[programStack] = -1;

            // Pad the stack to 16 bit alignment?
            //opStack = (int*)(((int)stack + 15) & ~(15));
            //int DIST = (int)(stack - (byte*)opStack);

            opStack = (int*)stack;
            *opStack = 0x0000BEEF;
            opStackOfs = 0;

            int opcode, r0, r1;

            while (true) {

            nextInstruction:
                r0 = opStack[opStackOfs];
                r1 = opStack[(byte)(opStackOfs - 1)];
            nextInstruction2:
                opcode = codeImage[programCounter++];
                opcode_t opcode_type = (opcode_t)opcode;

                switch (opcode_type) {
                    case opcode_t.OP_UNDEF:
                        Com_Error(vm.lastError = vmErrorCode_t.VM_BAD_INSTRUCTION, "Bad VM instruction");
                        return -1;
                    case opcode_t.OP_IGNORE:
                        goto nextInstruction2;
                    case opcode_t.OP_BREAK:
                        vm.breakCount++;
                        goto nextInstruction2;
                    case opcode_t.OP_CONST:
                        opStackOfs++;
                        r1 = r0;
                        r0 = opStack[opStackOfs] = codeImage[programCounter];

                        programCounter += 1;
                        goto nextInstruction2;
                    case opcode_t.OP_LOCAL:
                        opStackOfs++;
                        r1 = r0;
                        r0 = opStack[opStackOfs] = codeImage[programCounter] + programStack;

                        programCounter += 1;
                        goto nextInstruction2;
                    case opcode_t.OP_LOAD4:

                        r0 = opStack[opStackOfs] = *(int*)&image[r0 & dataMask];
                        goto nextInstruction2;
                    case opcode_t.OP_LOAD2:
                        r0 = opStack[opStackOfs] = *(ushort*)&image[r0 & dataMask];
                        goto nextInstruction2;
                    case opcode_t.OP_LOAD1:
                        r0 = opStack[opStackOfs] = image[r0 & dataMask];
                        goto nextInstruction2;

                    case opcode_t.OP_STORE4:
                        *(int*)&image[r1 & dataMask] = r0;
                        opStackOfs -= 2;
                        goto nextInstruction;
                    case opcode_t.OP_STORE2:
                        *(short*)&image[r1 & dataMask] = (short)r0;
                        opStackOfs -= 2;
                        goto nextInstruction;
                    case opcode_t.OP_STORE1:
                        image[r1 & dataMask] = (byte)r0;
                        opStackOfs -= 2;
                        goto nextInstruction;
                    case opcode_t.OP_ARG:

                        *(int*)&image[(codeImage[programCounter] + programStack) &
                                      dataMask] = r0;
                        opStackOfs--;
                        programCounter += 1;
                        goto nextInstruction;
                    case opcode_t.OP_BLOCK_COPY:
                        VM_BlockCopy((uint)r1, (uint)r0, (uint)codeImage[programCounter], ref vm);
                        programCounter += 1;
                        opStackOfs -= 2;
                        goto nextInstruction;
                    case opcode_t.OP_CALL:

                        *(int*)&image[programStack] = programCounter;

                        programCounter = r0;
                        opStackOfs--;
                        if (programCounter < 0) {
                            int r = -1;

                            vm.programStack = programStack - 4;

                            *(int*)&image[programStack + 4] = -1 - programCounter;

                            if (IntPtr.Size != sizeof(int)) {
                                //IntPtr* argarr = stackalloc IntPtr[16]; // 16 bytes always and fixed

                                // Todo: use span
                                IntPtr[] arguments = new IntPtr[16];

                                int* imagePtr = (int*)&image[programStack];


                                for (int i = 0; i < 16; ++i) {
                                    // argarr[i] = (IntPtr)(*(++imagePtr));
                                    arguments[i] = (IntPtr)(*(++imagePtr));
                                }

                                r = (int)vm.systemCall(ref vm, arguments);
                            } else {
                                throw new NotImplementedException();
                                // todo: Line below
                                //r = (int)vm.systemCall(ref vm, (IntPtr*)&image[programStack + 4]);
                            }

                            opStackOfs++;
                            opStack[opStackOfs] = r;
                            programCounter = *(int*)&image[programStack];
                        } else if ((uint)programCounter >= (uint)vm.instructionCount) {
                            vm.lastError = vmErrorCode_t.VM_PC_OUT_OF_RANGE;

                            Com_Error(vm.lastError, "VM program counter out of range in OP_CALL");
                            return -1;
                        } else {
                            programCounter = (int)vm.instructionPointers[programCounter];
                        }
                        goto nextInstruction;

                    case opcode_t.OP_PUSH:
                        opStackOfs++;
                        goto nextInstruction;
                    case opcode_t.OP_POP:
                        opStackOfs--;
                        goto nextInstruction;
                    case opcode_t.OP_ENTER:

                        v1 = codeImage[programCounter];

                        programCounter += 1;
                        programStack -= v1;

                        goto nextInstruction;
                    case opcode_t.OP_LEAVE:

                        v1 = codeImage[programCounter];

                        programStack += v1;

                        programCounter = *(int*)&image[programStack];

                        if (programCounter == -1) {
                            goto done;
                        } else if ((uint)programCounter >= (uint)vm.codeLength) {
                            Com_Error(vm.lastError = vmErrorCode_t.VM_PC_OUT_OF_RANGE, "VM program counter out of range in OP_LEAVE");
                            return -1;
                        }
                        goto nextInstruction;

                    case opcode_t.OP_JUMP:
                        if ((uint)r0 >= (uint)vm.instructionCount) {
                            Com_Error(vm.lastError = vmErrorCode_t.VM_PC_OUT_OF_RANGE, "VM program counter out of range in OP_JUMP");
                            return -1;
                        }

                        programCounter = (int)vm.instructionPointers[r0];

                        opStackOfs--;
                        goto nextInstruction;
                    case opcode_t.OP_EQ:
                        opStackOfs -= 2;
                        if (r1 == r0) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_NE:
                        opStackOfs -= 2;
                        if (r1 != r0) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_LTI:
                        opStackOfs -= 2;
                        if (r1 < r0) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_LEI:
                        opStackOfs -= 2;
                        if (r1 <= r0) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_GTI:
                        opStackOfs -= 2;
                        if (r1 > r0) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_GEI:
                        opStackOfs -= 2;
                        if (r1 >= r0) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_LTU:
                        opStackOfs -= 2;
                        if (((uint)r1) < ((uint)r0)) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_LEU:
                        opStackOfs -= 2;
                        if (((uint)r1) <= ((uint)r0)) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_GTU:
                        opStackOfs -= 2;
                        if (((uint)r1) > ((uint)r0)) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_GEU:
                        opStackOfs -= 2;
                        if (((uint)r1) >= ((uint)r0)) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_EQF:
                        opStackOfs -= 2;

                        if (((float*)opStack)[(byte)(opStackOfs + 1)] == ((float*)opStack)[(byte)(opStackOfs + 2)]) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_NEF:
                        opStackOfs -= 2;

                        if (((float*)opStack)[(byte)(opStackOfs + 1)] != ((float*)opStack)[(byte)(opStackOfs + 2)]) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_LTF:
                        opStackOfs -= 2;

                        if (((float*)opStack)[(byte)(opStackOfs + 1)] < ((float*)opStack)[(byte)(opStackOfs + 2)]) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_LEF:
                        opStackOfs -= 2;

                        if (((float*)opStack)[(byte)((byte)(opStackOfs + 1))] <= ((float*)opStack)[(byte)((byte)(opStackOfs + 2))]) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_GTF:
                        opStackOfs -= 2;

                        if (((float*)opStack)[(byte)(opStackOfs + 1)] >
                            ((float*)opStack)[(byte)(opStackOfs + 2)]) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }
                    case opcode_t.OP_GEF:
                        opStackOfs -= 2;

                        if (((float*)opStack)[(byte)(opStackOfs + 1)] >=
                            ((float*)opStack)[(byte)(opStackOfs + 2)]) {
                            programCounter = codeImage[programCounter];
                            goto nextInstruction;
                        } else {
                            programCounter += 1;
                            goto nextInstruction;
                        }

                    case opcode_t.OP_NEGI:
                        opStack[opStackOfs] = -r0;
                        goto nextInstruction;
                    case opcode_t.OP_ADD:
                        opStackOfs--;
                        opStack[opStackOfs] = r1 + r0;
                        goto nextInstruction;
                    case opcode_t.OP_SUB:
                        opStackOfs--;
                        opStack[opStackOfs] = r1 - r0;
                        goto nextInstruction;
                    case opcode_t.OP_DIVI:
                        opStackOfs--;
                        opStack[opStackOfs] = r1 / r0;
                        goto nextInstruction;
                    case opcode_t.OP_DIVU:
                        opStackOfs--;
                        opStack[opStackOfs] = (int)(((uint)r1) / ((uint)r0));
                        goto nextInstruction;
                    case opcode_t.OP_MODI:
                        opStackOfs--;
                        opStack[opStackOfs] = r1 % r0;
                        goto nextInstruction;
                    case opcode_t.OP_MODU:
                        opStackOfs--;
                        opStack[opStackOfs] = (int)(((uint)r1) % ((uint)r0));
                        goto nextInstruction;
                    case opcode_t.OP_MULI:
                        opStackOfs--;
                        opStack[opStackOfs] = r1 * r0;
                        goto nextInstruction;
                    case opcode_t.OP_MULU:
                        opStackOfs--;
                        opStack[opStackOfs] = (int)(((uint)r1) * ((uint)r0));
                        goto nextInstruction;
                    case opcode_t.OP_BAND:
                        opStackOfs--;
                        opStack[opStackOfs] = (int)(((uint)r1) & ((uint)r0));
                        goto nextInstruction;
                    case opcode_t.OP_BOR:
                        opStackOfs--;
                        opStack[opStackOfs] = (int)(((uint)r1) | ((uint)r0));
                        goto nextInstruction;
                    case opcode_t.OP_BXOR:
                        opStackOfs--;
                        opStack[opStackOfs] = (int)(((uint)r1) ^ ((uint)r0));
                        goto nextInstruction;
                    case opcode_t.OP_BCOM:
                        opStack[opStackOfs] = (int)(~((uint)r0));
                        goto nextInstruction;
                    case opcode_t.OP_LSH:
                        opStackOfs--;
                        opStack[opStackOfs] = r1 << r0;
                        goto nextInstruction;
                    case opcode_t.OP_RSHI:
                        opStackOfs--;
                        opStack[opStackOfs] = r1 >> r0;
                        goto nextInstruction;
                    case opcode_t.OP_RSHU:
                        opStackOfs--;
                        opStack[opStackOfs] = (int)(((uint)r1) >> r0);
                        goto nextInstruction;
                    case opcode_t.OP_NEGF:
                        ((float*)opStack)[opStackOfs] = -((float*)opStack)[opStackOfs];
                        goto nextInstruction;
                    case opcode_t.OP_ADDF:
                        opStackOfs--;
                        ((float*)opStack)[opStackOfs] =
                            ((float*)opStack)[opStackOfs] +
                            ((float*)opStack)[(byte)(opStackOfs + 1)];
                        goto nextInstruction;
                    case opcode_t.OP_SUBF:
                        opStackOfs--;
                        ((float*)opStack)[opStackOfs] =
                            ((float*)opStack)[opStackOfs] -
                            ((float*)opStack)[(byte)(opStackOfs + 1)];
                        goto nextInstruction;
                    case opcode_t.OP_DIVF:
                        opStackOfs--;
                        ((float*)opStack)[opStackOfs] =
                            ((float*)opStack)[opStackOfs] /
                            ((float*)opStack)[(byte)(opStackOfs + 1)];
                        goto nextInstruction;
                    case opcode_t.OP_MULF:
                        opStackOfs--;
                        ((float*)opStack)[opStackOfs] =
                            ((float*)opStack)[opStackOfs] *
                            ((float*)opStack)[(byte)(opStackOfs + 1)];
                        goto nextInstruction;
                    case opcode_t.OP_CVIF:
                        ((float*)opStack)[opStackOfs] = (float)opStack[opStackOfs];
                        goto nextInstruction;
                    case opcode_t.OP_CVFI:
                        opStack[opStackOfs] = (int)(((long)(((float*)opStack)[opStackOfs])));
                        goto nextInstruction;
                    case opcode_t.OP_SEX8:
                        opStack[opStackOfs] = (sbyte)opStack[opStackOfs];
                        goto nextInstruction;
                    case opcode_t.OP_SEX16:
                        opStack[opStackOfs] = (short)opStack[opStackOfs];
                        goto nextInstruction;
                }
            }

        done:
            vm.currentlyInterpreting = 0;

            if (opStackOfs != 1 || *opStack != 0x0000BEEF) {
                Com_Error(vm.lastError = vmErrorCode_t.VM_STACK_ERROR, "Interpreter stack error");
            }

            vm.programStack = stackOnEntry;

            return opStack[opStackOfs];
        }

        static void VM_Debug(int level) {
            vm_debugLevel = level;
        }

        static void VM_VmProfile_f(ref VirtMachine vm) {
            //(void) vm;
        }

        public static IntPtr TranslateAddress(IntPtr Address, ref VirtMachine vm) {
            return (IntPtr)VM_ArgPtr(Address, ref vm);
        }

        public static IntPtr VM_VMMalloc(int size, ref VirtMachine vm, out IntPtr GlobalAddr) {
            IntPtr Addr = (IntPtr)vm.dataMallocStart;
            vm.dataMallocStart += size;

            GlobalAddr = (IntPtr)VM_ArgPtr(Addr, ref vm);
            return Addr;
        }
    }
}