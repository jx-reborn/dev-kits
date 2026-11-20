#pragma once

#include <vcruntime.h>

_CRT_BEGIN_C_HEADER

void _ReadWriteBarrier(void);
#pragma intrinsic(_ReadWriteBarrier)

void _mm_pause(void);
void _mm_lfence(void);
void _mm_mfence(void);
void _mm_sfence(void);
#pragma intrinsic(_mm_pause)

unsigned long __readfsdword(unsigned long _Offset);
#pragma intrinsic(__readfsdword)

void __stosb(unsigned char* _Dest, unsigned char _Data, size_t _Count);
void __movsb(unsigned char* _Dest, unsigned char const* _Source, size_t _Count);
#pragma intrinsic(__stosb)
#pragma intrinsic(__movsb)

char _InterlockedExchange8(char volatile* _Target, char _Value);
long _InterlockedExchange(long volatile* _Target, long _Value);
long _InterlockedExchangeAdd(long volatile* _Addend, long _Value);
long _InterlockedIncrement(long volatile* _Addend);
long _InterlockedDecrement(long volatile* _Addend);
long _InterlockedCompareExchange(long volatile* _Destination, long _Exchange, long _Comparand);
void* _InterlockedCompareExchangePointer(void* volatile* _Destination, void* _Exchange, void* _Comparand);

#pragma intrinsic(_InterlockedExchange8)
#pragma intrinsic(_InterlockedExchange)
#pragma intrinsic(_InterlockedExchangeAdd)
#pragma intrinsic(_InterlockedIncrement)
#pragma intrinsic(_InterlockedDecrement)
#pragma intrinsic(_InterlockedCompareExchange)
#pragma intrinsic(_InterlockedCompareExchangePointer)

_CRT_END_C_HEADER
