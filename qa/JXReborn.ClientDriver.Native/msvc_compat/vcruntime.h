#pragma once

#define _VCRT_COMPILER_PREPROCESSOR 1
#define _CRT_PACKING 8

#ifndef _HAS_EXCEPTIONS
#define _HAS_EXCEPTIONS 0
#endif

#include <sal.h>
#include <vadefs.h>

#ifdef __cplusplus
    #define _CRT_BEGIN_C_HEADER \
        __pragma(pack(push, _CRT_PACKING)) \
        extern "C" {
    #define _CRT_END_C_HEADER \
        } \
        __pragma(pack(pop))
    #define _VCRT_NOEXCEPT noexcept
#else
    #define _CRT_BEGIN_C_HEADER \
        __pragma(pack(push, _CRT_PACKING))
    #define _CRT_END_C_HEADER \
        __pragma(pack(pop))
    #define _VCRT_NOEXCEPT
#endif

#ifndef __CRTDECL
#define __CRTDECL __cdecl
#endif

#ifndef _VCRTIMP
#define _VCRTIMP
#endif

#ifndef _CRTIMP
#define _CRTIMP
#endif

#define _VCRT_DEFINED_CRTIMP

#define __CRT_STRINGIZE(_Value) #_Value
#define _CRT_STRINGIZE(_Value) __CRT_STRINGIZE(_Value)

#define __CRT_WIDE(_String) L##_String
#define _CRT_WIDE(_String) __CRT_WIDE(_String)

#define __CRT_CONCATENATE(_Left, _Right) _Left##_Right
#define _CRT_CONCATENATE(_Left, _Right) __CRT_CONCATENATE(_Left, _Right)

#define _CRT_UNPARENTHESIZE(...) __VA_ARGS__

#ifndef _CRT_DEPRECATE_TEXT
#define _CRT_DEPRECATE_TEXT(_Text) __declspec(deprecated(_Text))
#endif

#ifndef _CRT_INSECURE_DEPRECATE
#ifdef _CRT_SECURE_NO_WARNINGS
#define _CRT_INSECURE_DEPRECATE(_Replacement)
#else
#define _CRT_INSECURE_DEPRECATE(_Replacement) _CRT_DEPRECATE_TEXT("Consider using " #_Replacement)
#endif
#endif

#ifndef _CRT_INSECURE_DEPRECATE_MEMORY
#define _CRT_INSECURE_DEPRECATE_MEMORY(_Replacement)
#endif

#ifndef _VCRT_ALIGN
#define _VCRT_ALIGN(x) __declspec(align(x))
#endif

_CRT_BEGIN_C_HEADER

#ifndef _SIZE_T_DEFINED
#define _SIZE_T_DEFINED
typedef unsigned int size_t;
#endif

#ifndef _PTRDIFF_T_DEFINED
#define _PTRDIFF_T_DEFINED
typedef int ptrdiff_t;
#endif

#ifndef _INTPTR_T_DEFINED
#define _INTPTR_T_DEFINED
typedef int intptr_t;
#endif

#ifndef _WCHAR_T_DEFINED
#define _WCHAR_T_DEFINED
#ifndef _NATIVE_WCHAR_T_DEFINED
typedef unsigned short wchar_t;
#endif
#endif

#ifdef __cplusplus
typedef bool __vcrt_bool;
#else
typedef unsigned char __vcrt_bool;
#endif

extern uintptr_t __security_cookie;
void __cdecl __security_init_cookie(void);
void __fastcall __security_check_cookie(uintptr_t _StackCookie);
__declspec(noreturn) void __cdecl __report_gsfailure(uintptr_t _StackCookie);

_CRT_END_C_HEADER
