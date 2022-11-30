#include "bg_lib.h"

void printf(const char* fmt, ...);
int  fib(int n);

int main(int command, int arg0, int arg1) {
	int i;
	
    if (command == 0) {
        printf("Hello World! - fib(5) = %i\n", fib(5));
    } else if (command == 1) {
		
		for (i = 0; i < 10; i++) {
			printf("%i ", fib(i));
		}
		
		printf("\n");
		
	} else {
        printf("Unknown command.\n");
    }
	
    trap_Nice();
    return 42;
}

void printf(const char* fmt, ...) {
    va_list argptr;
    char text[1024];

    va_start(argptr, fmt);
    vsprintf(text, fmt, argptr);
    va_end(argptr);

    trap_Printf(text);
}

int fib(int n) {
    if (n <= 2) {
        return 1;
    } else {
        return fib(n - 1) + fib(n - 2);
    }
}

