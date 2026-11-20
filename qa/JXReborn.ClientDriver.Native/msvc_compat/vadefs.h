#pragma once

#ifndef _VADEFS_H
#define _VADEFS_H

#ifndef _UINTPTR_T_DEFINED
#define _UINTPTR_T_DEFINED
typedef unsigned int uintptr_t;
#endif

#ifndef _VA_LIST_DEFINED
#define _VA_LIST_DEFINED
typedef char* va_list;
#endif

#ifdef __cplusplus
    #define _ADDRESSOF(v) (&const_cast<char&>(reinterpret_cast<const volatile char&>(v)))
#else
    #define _ADDRESSOF(v) (&(v))
#endif

#define _INTSIZEOF(n) ((sizeof(n) + sizeof(int) - 1) & ~(sizeof(int) - 1))
#define __crt_va_start_a(ap, v) ((void)(ap = (va_list)_ADDRESSOF(v) + _INTSIZEOF(v)))
#define __crt_va_arg(ap, t) (*(t*)((ap += _INTSIZEOF(t)) - _INTSIZEOF(t)))
#define __crt_va_end(ap) ((void)(ap = (va_list)0))
#define __crt_va_start(ap, x) __crt_va_start_a(ap, x)

#ifndef __unaligned
#define __unaligned
#endif

#ifndef _UNALIGNED
#define _UNALIGNED
#endif

#endif
