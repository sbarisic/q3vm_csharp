using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Q3VM2 {
    internal unsafe static class Program {
        static IntPtr systemCalls(ref VirtMachine vm, params IntPtr[] args) {
            int SyscallID = -1 - (int)args[0];
            // Console.WriteLine("INT {0}", SyscallID);

            switch (SyscallID) {
                // Print
                case -1:
                    string Str = Marshal.PtrToStringAnsi(VM.TranslateAddress(args[1], ref vm));
                    Console.Write(Str);
                    break;

                // MEMSET
                case -3:
                    if (VM.VM_MemoryRangeValid(args[1], (uint)args[3], ref vm) == 0) {
                        IntPtr Arg1 = VM.TranslateAddress(args[1], ref vm);
                        VM.memset((void*)Arg1, (int)args[2], (uint)args[3]);
                    }

                    return args[1];

                // MEMCPY
                case -4:
                    if (VM.VM_MemoryRangeValid(args[1], (uint)args[3], ref vm) == 0 && VM.VM_MemoryRangeValid(args[2], (uint)args[3], ref vm) == 0) {
                        IntPtr Arg1 = VM.TranslateAddress(args[1], ref vm);
                        IntPtr Arg2 = VM.TranslateAddress(args[2], ref vm);

                        VM.memcpy((void*)Arg1, (void*)Arg2, (uint)args[3]);
                    }

                    return args[1];

                case -69:
                    Console.WriteLine("Nice.");
                    break;

                default:
                    throw new Exception("Bad system call");
            }

            return (IntPtr)0;
        }

        static void Main(string[] args) {
            string FName = "data/lmao.qvm";
            VirtMachine Instance = new VirtMachine();

            Console.WriteLine("Starting virtual machine");
            Console.WriteLine("Running {0}", FName);
            Console.WriteLine();

            if (!VM.VM_Create(ref Instance, FName, File.ReadAllBytes(FName), systemCalls))
                throw new Exception("Holy shit!");

            byte[] Bytecode = File.ReadAllBytes("data/bytecode.qvm");


            int Ret = (int)VM.VM_Call(ref Instance, 1, null);

            Console.WriteLine();
            Console.WriteLine(">> {0}", Ret);

            VM.VM_Free(ref Instance);
            Console.ReadLine();
        }
    }
}
