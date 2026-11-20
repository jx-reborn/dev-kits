#pragma once

#include <vcruntime.h>

_CRT_BEGIN_C_HEADER

typedef enum _crt_argv_mode {
    _crt_argv_no_arguments,
    _crt_argv_unexpanded_arguments,
    _crt_argv_expanded_arguments,
} _crt_argv_mode;

__vcrt_bool __cdecl __vcrt_initialize(void);
__vcrt_bool __cdecl __vcrt_uninitialize(__vcrt_bool _Terminating);
__vcrt_bool __cdecl __vcrt_uninitialize_critical(void);
__vcrt_bool __cdecl __vcrt_thread_attach(void);
__vcrt_bool __cdecl __vcrt_thread_detach(void);

int __cdecl __isa_available_init(void);
int __cdecl __scrt_initialize_crt(int _ModuleType);

_CRT_END_C_HEADER
