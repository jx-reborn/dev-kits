#pragma once

#include <vcruntime.h>

_CRT_BEGIN_C_HEADER

typedef enum _EXCEPTION_DISPOSITION {
    ExceptionContinueExecution,
    ExceptionContinueSearch,
    ExceptionNestedException,
    ExceptionCollidedUnwind
} EXCEPTION_DISPOSITION;

struct _EXCEPTION_RECORD;
struct _CONTEXT;

#ifndef _EXCEPTION_POINTERS_DEFINED
struct _EXCEPTION_POINTERS;
#endif

unsigned long __cdecl _exception_code(void);
void* __cdecl _exception_info(void);
int __cdecl _abnormal_termination(void);

#define GetExceptionCode _exception_code
#define exception_code _exception_code
#define GetExceptionInformation() ((struct _EXCEPTION_POINTERS*)_exception_info())
#define AbnormalTermination _abnormal_termination

#define EXCEPTION_EXECUTE_HANDLER 1
#define EXCEPTION_CONTINUE_SEARCH 0
#define EXCEPTION_CONTINUE_EXECUTION (-1)

_CRT_END_C_HEADER
