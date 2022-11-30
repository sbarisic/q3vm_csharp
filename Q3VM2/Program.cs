using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Q3VM2 {
    internal static class Program {
        static IntPtr systemCalls(ref VirtMachine vm, params IntPtr[] args) {
            int SyscallID = -1 - (int)args[0];
            // Console.WriteLine("INT {0}", SyscallID);

            switch (SyscallID) {
                case -1:
                    string Str = Marshal.PtrToStringAnsi(VM.TranslateAddress(args[1], ref vm));
                    Console.Write(Str);
                    break;

                case -69:
                    Console.WriteLine("Nice.");
                    break;

                default:
                    throw new Exception("Bad system call");
            }

            return (IntPtr)0;
        }

        static void Main(string[] args) {
            string FName = "data/bytecode.qvm";
            VirtMachine Instance = new VirtMachine();

            if (!VM.VM_Create(ref Instance, FName, File.ReadAllBytes(FName), systemCalls))
                throw new Exception("Holy shit!");

            int Ret = (int)VM.VM_Call(ref Instance, 1, null);
            Console.WriteLine(">> {0}", Ret);

            VM.VM_Free(ref Instance);
            Console.ReadLine();
        }
    }
}
