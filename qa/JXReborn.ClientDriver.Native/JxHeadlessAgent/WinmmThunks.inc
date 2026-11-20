// Auto-generated from C:\Windows\SysWOW64\winmm.dll (193 exports)
static FARPROC g_WinmmProcs[193] = {};

// ordinal 2 [NONAME]
extern "C" __declspec(naked) void __cdecl Proxy_Ordinal_2() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 0]
        test eax, eax
        jnz short resolved_0
        push ecx
        push edx
        push 2
        push 0
        push 0
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_0:
        jmp eax
    }
}
static const char kWinmmName_1[] = "mciExecute";
extern "C" __declspec(naked) void __cdecl Proxy_mciExecute() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 4]
        test eax, eax
        jnz short resolved_1
        push ecx
        push edx
        push 3
        push offset kWinmmName_1
        push 1
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_1:
        jmp eax
    }
}
static const char kWinmmName_2[] = "CloseDriver";
extern "C" __declspec(naked) void __cdecl Proxy_CloseDriver() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 8]
        test eax, eax
        jnz short resolved_2
        push ecx
        push edx
        push 4
        push offset kWinmmName_2
        push 2
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_2:
        jmp eax
    }
}
static const char kWinmmName_3[] = "DefDriverProc";
extern "C" __declspec(naked) void __cdecl Proxy_DefDriverProc() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 12]
        test eax, eax
        jnz short resolved_3
        push ecx
        push edx
        push 5
        push offset kWinmmName_3
        push 3
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_3:
        jmp eax
    }
}
static const char kWinmmName_4[] = "DriverCallback";
extern "C" __declspec(naked) void __cdecl Proxy_DriverCallback() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 16]
        test eax, eax
        jnz short resolved_4
        push ecx
        push edx
        push 6
        push offset kWinmmName_4
        push 4
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_4:
        jmp eax
    }
}
static const char kWinmmName_5[] = "DrvGetModuleHandle";
extern "C" __declspec(naked) void __cdecl Proxy_DrvGetModuleHandle() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 20]
        test eax, eax
        jnz short resolved_5
        push ecx
        push edx
        push 7
        push offset kWinmmName_5
        push 5
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_5:
        jmp eax
    }
}
static const char kWinmmName_6[] = "GetDriverModuleHandle";
extern "C" __declspec(naked) void __cdecl Proxy_GetDriverModuleHandle() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 24]
        test eax, eax
        jnz short resolved_6
        push ecx
        push edx
        push 8
        push offset kWinmmName_6
        push 6
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_6:
        jmp eax
    }
}
static const char kWinmmName_7[] = "NotifyCallbackData";
extern "C" __declspec(naked) void __cdecl Proxy_NotifyCallbackData() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 28]
        test eax, eax
        jnz short resolved_7
        push ecx
        push edx
        push 9
        push offset kWinmmName_7
        push 7
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_7:
        jmp eax
    }
}
static const char kWinmmName_8[] = "OpenDriver";
extern "C" __declspec(naked) void __cdecl Proxy_OpenDriver() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 32]
        test eax, eax
        jnz short resolved_8
        push ecx
        push edx
        push 10
        push offset kWinmmName_8
        push 8
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_8:
        jmp eax
    }
}
static const char kWinmmName_9[] = "PlaySound";
extern "C" __declspec(naked) void __cdecl Proxy_PlaySound() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 36]
        test eax, eax
        jnz short resolved_9
        push ecx
        push edx
        push 11
        push offset kWinmmName_9
        push 9
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_9:
        jmp eax
    }
}
static const char kWinmmName_10[] = "PlaySoundA";
extern "C" __declspec(naked) void __cdecl Proxy_PlaySoundA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 40]
        test eax, eax
        jnz short resolved_10
        push ecx
        push edx
        push 12
        push offset kWinmmName_10
        push 10
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_10:
        jmp eax
    }
}
static const char kWinmmName_11[] = "PlaySoundW";
extern "C" __declspec(naked) void __cdecl Proxy_PlaySoundW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 44]
        test eax, eax
        jnz short resolved_11
        push ecx
        push edx
        push 13
        push offset kWinmmName_11
        push 11
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_11:
        jmp eax
    }
}
static const char kWinmmName_12[] = "SendDriverMessage";
extern "C" __declspec(naked) void __cdecl Proxy_SendDriverMessage() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 48]
        test eax, eax
        jnz short resolved_12
        push ecx
        push edx
        push 14
        push offset kWinmmName_12
        push 12
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_12:
        jmp eax
    }
}
static const char kWinmmName_13[] = "WOW32DriverCallback";
extern "C" __declspec(naked) void __cdecl Proxy_WOW32DriverCallback() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 52]
        test eax, eax
        jnz short resolved_13
        push ecx
        push edx
        push 15
        push offset kWinmmName_13
        push 13
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_13:
        jmp eax
    }
}
static const char kWinmmName_14[] = "WOW32ResolveMultiMediaHandle";
extern "C" __declspec(naked) void __cdecl Proxy_WOW32ResolveMultiMediaHandle() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 56]
        test eax, eax
        jnz short resolved_14
        push ecx
        push edx
        push 16
        push offset kWinmmName_14
        push 14
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_14:
        jmp eax
    }
}
static const char kWinmmName_15[] = "WOWAppExit";
extern "C" __declspec(naked) void __cdecl Proxy_WOWAppExit() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 60]
        test eax, eax
        jnz short resolved_15
        push ecx
        push edx
        push 17
        push offset kWinmmName_15
        push 15
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_15:
        jmp eax
    }
}
static const char kWinmmName_16[] = "aux32Message";
extern "C" __declspec(naked) void __cdecl Proxy_aux32Message() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 64]
        test eax, eax
        jnz short resolved_16
        push ecx
        push edx
        push 18
        push offset kWinmmName_16
        push 16
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_16:
        jmp eax
    }
}
static const char kWinmmName_17[] = "auxGetDevCapsA";
extern "C" __declspec(naked) void __cdecl Proxy_auxGetDevCapsA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 68]
        test eax, eax
        jnz short resolved_17
        push ecx
        push edx
        push 19
        push offset kWinmmName_17
        push 17
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_17:
        jmp eax
    }
}
static const char kWinmmName_18[] = "auxGetDevCapsW";
extern "C" __declspec(naked) void __cdecl Proxy_auxGetDevCapsW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 72]
        test eax, eax
        jnz short resolved_18
        push ecx
        push edx
        push 20
        push offset kWinmmName_18
        push 18
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_18:
        jmp eax
    }
}
static const char kWinmmName_19[] = "auxGetNumDevs";
extern "C" __declspec(naked) void __cdecl Proxy_auxGetNumDevs() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 76]
        test eax, eax
        jnz short resolved_19
        push ecx
        push edx
        push 21
        push offset kWinmmName_19
        push 19
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_19:
        jmp eax
    }
}
static const char kWinmmName_20[] = "auxGetVolume";
extern "C" __declspec(naked) void __cdecl Proxy_auxGetVolume() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 80]
        test eax, eax
        jnz short resolved_20
        push ecx
        push edx
        push 22
        push offset kWinmmName_20
        push 20
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_20:
        jmp eax
    }
}
static const char kWinmmName_21[] = "auxOutMessage";
extern "C" __declspec(naked) void __cdecl Proxy_auxOutMessage() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 84]
        test eax, eax
        jnz short resolved_21
        push ecx
        push edx
        push 23
        push offset kWinmmName_21
        push 21
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_21:
        jmp eax
    }
}
static const char kWinmmName_22[] = "auxSetVolume";
extern "C" __declspec(naked) void __cdecl Proxy_auxSetVolume() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 88]
        test eax, eax
        jnz short resolved_22
        push ecx
        push edx
        push 24
        push offset kWinmmName_22
        push 22
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_22:
        jmp eax
    }
}
static const char kWinmmName_23[] = "joy32Message";
extern "C" __declspec(naked) void __cdecl Proxy_joy32Message() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 92]
        test eax, eax
        jnz short resolved_23
        push ecx
        push edx
        push 25
        push offset kWinmmName_23
        push 23
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_23:
        jmp eax
    }
}
static const char kWinmmName_24[] = "joyConfigChanged";
extern "C" __declspec(naked) void __cdecl Proxy_joyConfigChanged() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 96]
        test eax, eax
        jnz short resolved_24
        push ecx
        push edx
        push 26
        push offset kWinmmName_24
        push 24
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_24:
        jmp eax
    }
}
static const char kWinmmName_25[] = "joyGetDevCapsA";
extern "C" __declspec(naked) void __cdecl Proxy_joyGetDevCapsA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 100]
        test eax, eax
        jnz short resolved_25
        push ecx
        push edx
        push 27
        push offset kWinmmName_25
        push 25
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_25:
        jmp eax
    }
}
static const char kWinmmName_26[] = "joyGetDevCapsW";
extern "C" __declspec(naked) void __cdecl Proxy_joyGetDevCapsW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 104]
        test eax, eax
        jnz short resolved_26
        push ecx
        push edx
        push 28
        push offset kWinmmName_26
        push 26
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_26:
        jmp eax
    }
}
static const char kWinmmName_27[] = "joyGetNumDevs";
extern "C" __declspec(naked) void __cdecl Proxy_joyGetNumDevs() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 108]
        test eax, eax
        jnz short resolved_27
        push ecx
        push edx
        push 29
        push offset kWinmmName_27
        push 27
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_27:
        jmp eax
    }
}
static const char kWinmmName_28[] = "joyGetPos";
extern "C" __declspec(naked) void __cdecl Proxy_joyGetPos() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 112]
        test eax, eax
        jnz short resolved_28
        push ecx
        push edx
        push 30
        push offset kWinmmName_28
        push 28
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_28:
        jmp eax
    }
}
static const char kWinmmName_29[] = "joyGetPosEx";
extern "C" __declspec(naked) void __cdecl Proxy_joyGetPosEx() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 116]
        test eax, eax
        jnz short resolved_29
        push ecx
        push edx
        push 31
        push offset kWinmmName_29
        push 29
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_29:
        jmp eax
    }
}
static const char kWinmmName_30[] = "joyGetThreshold";
extern "C" __declspec(naked) void __cdecl Proxy_joyGetThreshold() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 120]
        test eax, eax
        jnz short resolved_30
        push ecx
        push edx
        push 32
        push offset kWinmmName_30
        push 30
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_30:
        jmp eax
    }
}
static const char kWinmmName_31[] = "joyReleaseCapture";
extern "C" __declspec(naked) void __cdecl Proxy_joyReleaseCapture() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 124]
        test eax, eax
        jnz short resolved_31
        push ecx
        push edx
        push 33
        push offset kWinmmName_31
        push 31
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_31:
        jmp eax
    }
}
static const char kWinmmName_32[] = "joySetCapture";
extern "C" __declspec(naked) void __cdecl Proxy_joySetCapture() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 128]
        test eax, eax
        jnz short resolved_32
        push ecx
        push edx
        push 34
        push offset kWinmmName_32
        push 32
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_32:
        jmp eax
    }
}
static const char kWinmmName_33[] = "joySetThreshold";
extern "C" __declspec(naked) void __cdecl Proxy_joySetThreshold() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 132]
        test eax, eax
        jnz short resolved_33
        push ecx
        push edx
        push 35
        push offset kWinmmName_33
        push 33
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_33:
        jmp eax
    }
}
static const char kWinmmName_34[] = "mci32Message";
extern "C" __declspec(naked) void __cdecl Proxy_mci32Message() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 136]
        test eax, eax
        jnz short resolved_34
        push ecx
        push edx
        push 36
        push offset kWinmmName_34
        push 34
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_34:
        jmp eax
    }
}
static const char kWinmmName_35[] = "mciDriverNotify";
extern "C" __declspec(naked) void __cdecl Proxy_mciDriverNotify() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 140]
        test eax, eax
        jnz short resolved_35
        push ecx
        push edx
        push 37
        push offset kWinmmName_35
        push 35
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_35:
        jmp eax
    }
}
static const char kWinmmName_36[] = "mciDriverYield";
extern "C" __declspec(naked) void __cdecl Proxy_mciDriverYield() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 144]
        test eax, eax
        jnz short resolved_36
        push ecx
        push edx
        push 38
        push offset kWinmmName_36
        push 36
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_36:
        jmp eax
    }
}
static const char kWinmmName_37[] = "mciFreeCommandResource";
extern "C" __declspec(naked) void __cdecl Proxy_mciFreeCommandResource() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 148]
        test eax, eax
        jnz short resolved_37
        push ecx
        push edx
        push 39
        push offset kWinmmName_37
        push 37
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_37:
        jmp eax
    }
}
static const char kWinmmName_38[] = "mciGetCreatorTask";
extern "C" __declspec(naked) void __cdecl Proxy_mciGetCreatorTask() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 152]
        test eax, eax
        jnz short resolved_38
        push ecx
        push edx
        push 40
        push offset kWinmmName_38
        push 38
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_38:
        jmp eax
    }
}
static const char kWinmmName_39[] = "mciGetDeviceIDA";
extern "C" __declspec(naked) void __cdecl Proxy_mciGetDeviceIDA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 156]
        test eax, eax
        jnz short resolved_39
        push ecx
        push edx
        push 41
        push offset kWinmmName_39
        push 39
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_39:
        jmp eax
    }
}
static const char kWinmmName_40[] = "mciGetDeviceIDFromElementIDA";
extern "C" __declspec(naked) void __cdecl Proxy_mciGetDeviceIDFromElementIDA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 160]
        test eax, eax
        jnz short resolved_40
        push ecx
        push edx
        push 42
        push offset kWinmmName_40
        push 40
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_40:
        jmp eax
    }
}
static const char kWinmmName_41[] = "mciGetDeviceIDFromElementIDW";
extern "C" __declspec(naked) void __cdecl Proxy_mciGetDeviceIDFromElementIDW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 164]
        test eax, eax
        jnz short resolved_41
        push ecx
        push edx
        push 43
        push offset kWinmmName_41
        push 41
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_41:
        jmp eax
    }
}
static const char kWinmmName_42[] = "mciGetDeviceIDW";
extern "C" __declspec(naked) void __cdecl Proxy_mciGetDeviceIDW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 168]
        test eax, eax
        jnz short resolved_42
        push ecx
        push edx
        push 44
        push offset kWinmmName_42
        push 42
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_42:
        jmp eax
    }
}
static const char kWinmmName_43[] = "mciGetDriverData";
extern "C" __declspec(naked) void __cdecl Proxy_mciGetDriverData() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 172]
        test eax, eax
        jnz short resolved_43
        push ecx
        push edx
        push 45
        push offset kWinmmName_43
        push 43
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_43:
        jmp eax
    }
}
static const char kWinmmName_44[] = "mciGetErrorStringA";
extern "C" __declspec(naked) void __cdecl Proxy_mciGetErrorStringA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 176]
        test eax, eax
        jnz short resolved_44
        push ecx
        push edx
        push 46
        push offset kWinmmName_44
        push 44
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_44:
        jmp eax
    }
}
static const char kWinmmName_45[] = "mciGetErrorStringW";
extern "C" __declspec(naked) void __cdecl Proxy_mciGetErrorStringW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 180]
        test eax, eax
        jnz short resolved_45
        push ecx
        push edx
        push 47
        push offset kWinmmName_45
        push 45
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_45:
        jmp eax
    }
}
static const char kWinmmName_46[] = "mciGetYieldProc";
extern "C" __declspec(naked) void __cdecl Proxy_mciGetYieldProc() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 184]
        test eax, eax
        jnz short resolved_46
        push ecx
        push edx
        push 48
        push offset kWinmmName_46
        push 46
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_46:
        jmp eax
    }
}
static const char kWinmmName_47[] = "mciLoadCommandResource";
extern "C" __declspec(naked) void __cdecl Proxy_mciLoadCommandResource() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 188]
        test eax, eax
        jnz short resolved_47
        push ecx
        push edx
        push 49
        push offset kWinmmName_47
        push 47
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_47:
        jmp eax
    }
}
static const char kWinmmName_48[] = "mciSendCommandA";
extern "C" __declspec(naked) void __cdecl Proxy_mciSendCommandA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 192]
        test eax, eax
        jnz short resolved_48
        push ecx
        push edx
        push 50
        push offset kWinmmName_48
        push 48
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_48:
        jmp eax
    }
}
static const char kWinmmName_49[] = "mciSendCommandW";
extern "C" __declspec(naked) void __cdecl Proxy_mciSendCommandW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 196]
        test eax, eax
        jnz short resolved_49
        push ecx
        push edx
        push 51
        push offset kWinmmName_49
        push 49
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_49:
        jmp eax
    }
}
static const char kWinmmName_50[] = "mciSendStringA";
extern "C" __declspec(naked) void __cdecl Proxy_mciSendStringA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 200]
        test eax, eax
        jnz short resolved_50
        push ecx
        push edx
        push 52
        push offset kWinmmName_50
        push 50
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_50:
        jmp eax
    }
}
static const char kWinmmName_51[] = "mciSendStringW";
extern "C" __declspec(naked) void __cdecl Proxy_mciSendStringW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 204]
        test eax, eax
        jnz short resolved_51
        push ecx
        push edx
        push 53
        push offset kWinmmName_51
        push 51
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_51:
        jmp eax
    }
}
static const char kWinmmName_52[] = "mciSetDriverData";
extern "C" __declspec(naked) void __cdecl Proxy_mciSetDriverData() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 208]
        test eax, eax
        jnz short resolved_52
        push ecx
        push edx
        push 54
        push offset kWinmmName_52
        push 52
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_52:
        jmp eax
    }
}
static const char kWinmmName_53[] = "mciSetYieldProc";
extern "C" __declspec(naked) void __cdecl Proxy_mciSetYieldProc() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 212]
        test eax, eax
        jnz short resolved_53
        push ecx
        push edx
        push 55
        push offset kWinmmName_53
        push 53
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_53:
        jmp eax
    }
}
static const char kWinmmName_54[] = "mid32Message";
extern "C" __declspec(naked) void __cdecl Proxy_mid32Message() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 216]
        test eax, eax
        jnz short resolved_54
        push ecx
        push edx
        push 56
        push offset kWinmmName_54
        push 54
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_54:
        jmp eax
    }
}
static const char kWinmmName_55[] = "midiConnect";
extern "C" __declspec(naked) void __cdecl Proxy_midiConnect() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 220]
        test eax, eax
        jnz short resolved_55
        push ecx
        push edx
        push 57
        push offset kWinmmName_55
        push 55
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_55:
        jmp eax
    }
}
static const char kWinmmName_56[] = "midiDisconnect";
extern "C" __declspec(naked) void __cdecl Proxy_midiDisconnect() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 224]
        test eax, eax
        jnz short resolved_56
        push ecx
        push edx
        push 58
        push offset kWinmmName_56
        push 56
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_56:
        jmp eax
    }
}
static const char kWinmmName_57[] = "midiInAddBuffer";
extern "C" __declspec(naked) void __cdecl Proxy_midiInAddBuffer() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 228]
        test eax, eax
        jnz short resolved_57
        push ecx
        push edx
        push 59
        push offset kWinmmName_57
        push 57
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_57:
        jmp eax
    }
}
static const char kWinmmName_58[] = "midiInClose";
extern "C" __declspec(naked) void __cdecl Proxy_midiInClose() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 232]
        test eax, eax
        jnz short resolved_58
        push ecx
        push edx
        push 60
        push offset kWinmmName_58
        push 58
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_58:
        jmp eax
    }
}
static const char kWinmmName_59[] = "midiInGetDevCapsA";
extern "C" __declspec(naked) void __cdecl Proxy_midiInGetDevCapsA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 236]
        test eax, eax
        jnz short resolved_59
        push ecx
        push edx
        push 61
        push offset kWinmmName_59
        push 59
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_59:
        jmp eax
    }
}
static const char kWinmmName_60[] = "midiInGetDevCapsW";
extern "C" __declspec(naked) void __cdecl Proxy_midiInGetDevCapsW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 240]
        test eax, eax
        jnz short resolved_60
        push ecx
        push edx
        push 62
        push offset kWinmmName_60
        push 60
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_60:
        jmp eax
    }
}
static const char kWinmmName_61[] = "midiInGetErrorTextA";
extern "C" __declspec(naked) void __cdecl Proxy_midiInGetErrorTextA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 244]
        test eax, eax
        jnz short resolved_61
        push ecx
        push edx
        push 63
        push offset kWinmmName_61
        push 61
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_61:
        jmp eax
    }
}
static const char kWinmmName_62[] = "midiInGetErrorTextW";
extern "C" __declspec(naked) void __cdecl Proxy_midiInGetErrorTextW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 248]
        test eax, eax
        jnz short resolved_62
        push ecx
        push edx
        push 64
        push offset kWinmmName_62
        push 62
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_62:
        jmp eax
    }
}
static const char kWinmmName_63[] = "midiInGetID";
extern "C" __declspec(naked) void __cdecl Proxy_midiInGetID() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 252]
        test eax, eax
        jnz short resolved_63
        push ecx
        push edx
        push 65
        push offset kWinmmName_63
        push 63
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_63:
        jmp eax
    }
}
static const char kWinmmName_64[] = "midiInGetNumDevs";
extern "C" __declspec(naked) void __cdecl Proxy_midiInGetNumDevs() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 256]
        test eax, eax
        jnz short resolved_64
        push ecx
        push edx
        push 66
        push offset kWinmmName_64
        push 64
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_64:
        jmp eax
    }
}
static const char kWinmmName_65[] = "midiInMessage";
extern "C" __declspec(naked) void __cdecl Proxy_midiInMessage() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 260]
        test eax, eax
        jnz short resolved_65
        push ecx
        push edx
        push 67
        push offset kWinmmName_65
        push 65
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_65:
        jmp eax
    }
}
static const char kWinmmName_66[] = "midiInOpen";
extern "C" __declspec(naked) void __cdecl Proxy_midiInOpen() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 264]
        test eax, eax
        jnz short resolved_66
        push ecx
        push edx
        push 68
        push offset kWinmmName_66
        push 66
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_66:
        jmp eax
    }
}
static const char kWinmmName_67[] = "midiInPrepareHeader";
extern "C" __declspec(naked) void __cdecl Proxy_midiInPrepareHeader() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 268]
        test eax, eax
        jnz short resolved_67
        push ecx
        push edx
        push 69
        push offset kWinmmName_67
        push 67
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_67:
        jmp eax
    }
}
static const char kWinmmName_68[] = "midiInReset";
extern "C" __declspec(naked) void __cdecl Proxy_midiInReset() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 272]
        test eax, eax
        jnz short resolved_68
        push ecx
        push edx
        push 70
        push offset kWinmmName_68
        push 68
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_68:
        jmp eax
    }
}
static const char kWinmmName_69[] = "midiInStart";
extern "C" __declspec(naked) void __cdecl Proxy_midiInStart() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 276]
        test eax, eax
        jnz short resolved_69
        push ecx
        push edx
        push 71
        push offset kWinmmName_69
        push 69
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_69:
        jmp eax
    }
}
static const char kWinmmName_70[] = "midiInStop";
extern "C" __declspec(naked) void __cdecl Proxy_midiInStop() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 280]
        test eax, eax
        jnz short resolved_70
        push ecx
        push edx
        push 72
        push offset kWinmmName_70
        push 70
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_70:
        jmp eax
    }
}
static const char kWinmmName_71[] = "midiInUnprepareHeader";
extern "C" __declspec(naked) void __cdecl Proxy_midiInUnprepareHeader() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 284]
        test eax, eax
        jnz short resolved_71
        push ecx
        push edx
        push 73
        push offset kWinmmName_71
        push 71
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_71:
        jmp eax
    }
}
static const char kWinmmName_72[] = "midiOutCacheDrumPatches";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutCacheDrumPatches() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 288]
        test eax, eax
        jnz short resolved_72
        push ecx
        push edx
        push 74
        push offset kWinmmName_72
        push 72
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_72:
        jmp eax
    }
}
static const char kWinmmName_73[] = "midiOutCachePatches";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutCachePatches() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 292]
        test eax, eax
        jnz short resolved_73
        push ecx
        push edx
        push 75
        push offset kWinmmName_73
        push 73
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_73:
        jmp eax
    }
}
static const char kWinmmName_74[] = "midiOutClose";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutClose() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 296]
        test eax, eax
        jnz short resolved_74
        push ecx
        push edx
        push 76
        push offset kWinmmName_74
        push 74
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_74:
        jmp eax
    }
}
static const char kWinmmName_75[] = "midiOutGetDevCapsA";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutGetDevCapsA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 300]
        test eax, eax
        jnz short resolved_75
        push ecx
        push edx
        push 77
        push offset kWinmmName_75
        push 75
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_75:
        jmp eax
    }
}
static const char kWinmmName_76[] = "midiOutGetDevCapsW";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutGetDevCapsW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 304]
        test eax, eax
        jnz short resolved_76
        push ecx
        push edx
        push 78
        push offset kWinmmName_76
        push 76
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_76:
        jmp eax
    }
}
static const char kWinmmName_77[] = "midiOutGetErrorTextA";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutGetErrorTextA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 308]
        test eax, eax
        jnz short resolved_77
        push ecx
        push edx
        push 79
        push offset kWinmmName_77
        push 77
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_77:
        jmp eax
    }
}
static const char kWinmmName_78[] = "midiOutGetErrorTextW";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutGetErrorTextW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 312]
        test eax, eax
        jnz short resolved_78
        push ecx
        push edx
        push 80
        push offset kWinmmName_78
        push 78
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_78:
        jmp eax
    }
}
static const char kWinmmName_79[] = "midiOutGetID";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutGetID() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 316]
        test eax, eax
        jnz short resolved_79
        push ecx
        push edx
        push 81
        push offset kWinmmName_79
        push 79
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_79:
        jmp eax
    }
}
static const char kWinmmName_80[] = "midiOutGetNumDevs";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutGetNumDevs() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 320]
        test eax, eax
        jnz short resolved_80
        push ecx
        push edx
        push 82
        push offset kWinmmName_80
        push 80
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_80:
        jmp eax
    }
}
static const char kWinmmName_81[] = "midiOutGetVolume";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutGetVolume() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 324]
        test eax, eax
        jnz short resolved_81
        push ecx
        push edx
        push 83
        push offset kWinmmName_81
        push 81
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_81:
        jmp eax
    }
}
static const char kWinmmName_82[] = "midiOutLongMsg";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutLongMsg() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 328]
        test eax, eax
        jnz short resolved_82
        push ecx
        push edx
        push 84
        push offset kWinmmName_82
        push 82
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_82:
        jmp eax
    }
}
static const char kWinmmName_83[] = "midiOutMessage";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutMessage() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 332]
        test eax, eax
        jnz short resolved_83
        push ecx
        push edx
        push 85
        push offset kWinmmName_83
        push 83
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_83:
        jmp eax
    }
}
static const char kWinmmName_84[] = "midiOutOpen";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutOpen() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 336]
        test eax, eax
        jnz short resolved_84
        push ecx
        push edx
        push 86
        push offset kWinmmName_84
        push 84
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_84:
        jmp eax
    }
}
static const char kWinmmName_85[] = "midiOutPrepareHeader";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutPrepareHeader() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 340]
        test eax, eax
        jnz short resolved_85
        push ecx
        push edx
        push 87
        push offset kWinmmName_85
        push 85
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_85:
        jmp eax
    }
}
static const char kWinmmName_86[] = "midiOutReset";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutReset() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 344]
        test eax, eax
        jnz short resolved_86
        push ecx
        push edx
        push 88
        push offset kWinmmName_86
        push 86
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_86:
        jmp eax
    }
}
static const char kWinmmName_87[] = "midiOutSetVolume";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutSetVolume() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 348]
        test eax, eax
        jnz short resolved_87
        push ecx
        push edx
        push 89
        push offset kWinmmName_87
        push 87
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_87:
        jmp eax
    }
}
static const char kWinmmName_88[] = "midiOutShortMsg";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutShortMsg() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 352]
        test eax, eax
        jnz short resolved_88
        push ecx
        push edx
        push 90
        push offset kWinmmName_88
        push 88
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_88:
        jmp eax
    }
}
static const char kWinmmName_89[] = "midiOutUnprepareHeader";
extern "C" __declspec(naked) void __cdecl Proxy_midiOutUnprepareHeader() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 356]
        test eax, eax
        jnz short resolved_89
        push ecx
        push edx
        push 91
        push offset kWinmmName_89
        push 89
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_89:
        jmp eax
    }
}
static const char kWinmmName_90[] = "midiStreamClose";
extern "C" __declspec(naked) void __cdecl Proxy_midiStreamClose() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 360]
        test eax, eax
        jnz short resolved_90
        push ecx
        push edx
        push 92
        push offset kWinmmName_90
        push 90
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_90:
        jmp eax
    }
}
static const char kWinmmName_91[] = "midiStreamOpen";
extern "C" __declspec(naked) void __cdecl Proxy_midiStreamOpen() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 364]
        test eax, eax
        jnz short resolved_91
        push ecx
        push edx
        push 93
        push offset kWinmmName_91
        push 91
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_91:
        jmp eax
    }
}
static const char kWinmmName_92[] = "midiStreamOut";
extern "C" __declspec(naked) void __cdecl Proxy_midiStreamOut() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 368]
        test eax, eax
        jnz short resolved_92
        push ecx
        push edx
        push 94
        push offset kWinmmName_92
        push 92
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_92:
        jmp eax
    }
}
static const char kWinmmName_93[] = "midiStreamPause";
extern "C" __declspec(naked) void __cdecl Proxy_midiStreamPause() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 372]
        test eax, eax
        jnz short resolved_93
        push ecx
        push edx
        push 95
        push offset kWinmmName_93
        push 93
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_93:
        jmp eax
    }
}
static const char kWinmmName_94[] = "midiStreamPosition";
extern "C" __declspec(naked) void __cdecl Proxy_midiStreamPosition() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 376]
        test eax, eax
        jnz short resolved_94
        push ecx
        push edx
        push 96
        push offset kWinmmName_94
        push 94
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_94:
        jmp eax
    }
}
static const char kWinmmName_95[] = "midiStreamProperty";
extern "C" __declspec(naked) void __cdecl Proxy_midiStreamProperty() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 380]
        test eax, eax
        jnz short resolved_95
        push ecx
        push edx
        push 97
        push offset kWinmmName_95
        push 95
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_95:
        jmp eax
    }
}
static const char kWinmmName_96[] = "midiStreamRestart";
extern "C" __declspec(naked) void __cdecl Proxy_midiStreamRestart() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 384]
        test eax, eax
        jnz short resolved_96
        push ecx
        push edx
        push 98
        push offset kWinmmName_96
        push 96
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_96:
        jmp eax
    }
}
static const char kWinmmName_97[] = "midiStreamStop";
extern "C" __declspec(naked) void __cdecl Proxy_midiStreamStop() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 388]
        test eax, eax
        jnz short resolved_97
        push ecx
        push edx
        push 99
        push offset kWinmmName_97
        push 97
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_97:
        jmp eax
    }
}
static const char kWinmmName_98[] = "mixerClose";
extern "C" __declspec(naked) void __cdecl Proxy_mixerClose() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 392]
        test eax, eax
        jnz short resolved_98
        push ecx
        push edx
        push 100
        push offset kWinmmName_98
        push 98
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_98:
        jmp eax
    }
}
static const char kWinmmName_99[] = "mixerGetControlDetailsA";
extern "C" __declspec(naked) void __cdecl Proxy_mixerGetControlDetailsA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 396]
        test eax, eax
        jnz short resolved_99
        push ecx
        push edx
        push 101
        push offset kWinmmName_99
        push 99
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_99:
        jmp eax
    }
}
static const char kWinmmName_100[] = "mixerGetControlDetailsW";
extern "C" __declspec(naked) void __cdecl Proxy_mixerGetControlDetailsW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 400]
        test eax, eax
        jnz short resolved_100
        push ecx
        push edx
        push 102
        push offset kWinmmName_100
        push 100
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_100:
        jmp eax
    }
}
static const char kWinmmName_101[] = "mixerGetDevCapsA";
extern "C" __declspec(naked) void __cdecl Proxy_mixerGetDevCapsA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 404]
        test eax, eax
        jnz short resolved_101
        push ecx
        push edx
        push 103
        push offset kWinmmName_101
        push 101
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_101:
        jmp eax
    }
}
static const char kWinmmName_102[] = "mixerGetDevCapsW";
extern "C" __declspec(naked) void __cdecl Proxy_mixerGetDevCapsW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 408]
        test eax, eax
        jnz short resolved_102
        push ecx
        push edx
        push 104
        push offset kWinmmName_102
        push 102
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_102:
        jmp eax
    }
}
static const char kWinmmName_103[] = "mixerGetID";
extern "C" __declspec(naked) void __cdecl Proxy_mixerGetID() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 412]
        test eax, eax
        jnz short resolved_103
        push ecx
        push edx
        push 105
        push offset kWinmmName_103
        push 103
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_103:
        jmp eax
    }
}
static const char kWinmmName_104[] = "mixerGetLineControlsA";
extern "C" __declspec(naked) void __cdecl Proxy_mixerGetLineControlsA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 416]
        test eax, eax
        jnz short resolved_104
        push ecx
        push edx
        push 106
        push offset kWinmmName_104
        push 104
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_104:
        jmp eax
    }
}
static const char kWinmmName_105[] = "mixerGetLineControlsW";
extern "C" __declspec(naked) void __cdecl Proxy_mixerGetLineControlsW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 420]
        test eax, eax
        jnz short resolved_105
        push ecx
        push edx
        push 107
        push offset kWinmmName_105
        push 105
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_105:
        jmp eax
    }
}
static const char kWinmmName_106[] = "mixerGetLineInfoA";
extern "C" __declspec(naked) void __cdecl Proxy_mixerGetLineInfoA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 424]
        test eax, eax
        jnz short resolved_106
        push ecx
        push edx
        push 108
        push offset kWinmmName_106
        push 106
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_106:
        jmp eax
    }
}
static const char kWinmmName_107[] = "mixerGetLineInfoW";
extern "C" __declspec(naked) void __cdecl Proxy_mixerGetLineInfoW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 428]
        test eax, eax
        jnz short resolved_107
        push ecx
        push edx
        push 109
        push offset kWinmmName_107
        push 107
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_107:
        jmp eax
    }
}
static const char kWinmmName_108[] = "mixerGetNumDevs";
extern "C" __declspec(naked) void __cdecl Proxy_mixerGetNumDevs() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 432]
        test eax, eax
        jnz short resolved_108
        push ecx
        push edx
        push 110
        push offset kWinmmName_108
        push 108
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_108:
        jmp eax
    }
}
static const char kWinmmName_109[] = "mixerMessage";
extern "C" __declspec(naked) void __cdecl Proxy_mixerMessage() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 436]
        test eax, eax
        jnz short resolved_109
        push ecx
        push edx
        push 111
        push offset kWinmmName_109
        push 109
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_109:
        jmp eax
    }
}
static const char kWinmmName_110[] = "mixerOpen";
extern "C" __declspec(naked) void __cdecl Proxy_mixerOpen() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 440]
        test eax, eax
        jnz short resolved_110
        push ecx
        push edx
        push 112
        push offset kWinmmName_110
        push 110
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_110:
        jmp eax
    }
}
static const char kWinmmName_111[] = "mixerSetControlDetails";
extern "C" __declspec(naked) void __cdecl Proxy_mixerSetControlDetails() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 444]
        test eax, eax
        jnz short resolved_111
        push ecx
        push edx
        push 113
        push offset kWinmmName_111
        push 111
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_111:
        jmp eax
    }
}
static const char kWinmmName_112[] = "mmDrvInstall";
extern "C" __declspec(naked) void __cdecl Proxy_mmDrvInstall() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 448]
        test eax, eax
        jnz short resolved_112
        push ecx
        push edx
        push 114
        push offset kWinmmName_112
        push 112
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_112:
        jmp eax
    }
}
static const char kWinmmName_113[] = "mmGetCurrentTask";
extern "C" __declspec(naked) void __cdecl Proxy_mmGetCurrentTask() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 452]
        test eax, eax
        jnz short resolved_113
        push ecx
        push edx
        push 115
        push offset kWinmmName_113
        push 113
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_113:
        jmp eax
    }
}
static const char kWinmmName_114[] = "mmTaskBlock";
extern "C" __declspec(naked) void __cdecl Proxy_mmTaskBlock() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 456]
        test eax, eax
        jnz short resolved_114
        push ecx
        push edx
        push 116
        push offset kWinmmName_114
        push 114
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_114:
        jmp eax
    }
}
static const char kWinmmName_115[] = "mmTaskCreate";
extern "C" __declspec(naked) void __cdecl Proxy_mmTaskCreate() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 460]
        test eax, eax
        jnz short resolved_115
        push ecx
        push edx
        push 117
        push offset kWinmmName_115
        push 115
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_115:
        jmp eax
    }
}
static const char kWinmmName_116[] = "mmTaskSignal";
extern "C" __declspec(naked) void __cdecl Proxy_mmTaskSignal() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 464]
        test eax, eax
        jnz short resolved_116
        push ecx
        push edx
        push 118
        push offset kWinmmName_116
        push 116
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_116:
        jmp eax
    }
}
static const char kWinmmName_117[] = "mmTaskYield";
extern "C" __declspec(naked) void __cdecl Proxy_mmTaskYield() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 468]
        test eax, eax
        jnz short resolved_117
        push ecx
        push edx
        push 119
        push offset kWinmmName_117
        push 117
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_117:
        jmp eax
    }
}
static const char kWinmmName_118[] = "mmioAdvance";
extern "C" __declspec(naked) void __cdecl Proxy_mmioAdvance() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 472]
        test eax, eax
        jnz short resolved_118
        push ecx
        push edx
        push 120
        push offset kWinmmName_118
        push 118
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_118:
        jmp eax
    }
}
static const char kWinmmName_119[] = "mmioAscend";
extern "C" __declspec(naked) void __cdecl Proxy_mmioAscend() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 476]
        test eax, eax
        jnz short resolved_119
        push ecx
        push edx
        push 121
        push offset kWinmmName_119
        push 119
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_119:
        jmp eax
    }
}
static const char kWinmmName_120[] = "mmioClose";
extern "C" __declspec(naked) void __cdecl Proxy_mmioClose() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 480]
        test eax, eax
        jnz short resolved_120
        push ecx
        push edx
        push 122
        push offset kWinmmName_120
        push 120
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_120:
        jmp eax
    }
}
static const char kWinmmName_121[] = "mmioCreateChunk";
extern "C" __declspec(naked) void __cdecl Proxy_mmioCreateChunk() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 484]
        test eax, eax
        jnz short resolved_121
        push ecx
        push edx
        push 123
        push offset kWinmmName_121
        push 121
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_121:
        jmp eax
    }
}
static const char kWinmmName_122[] = "mmioDescend";
extern "C" __declspec(naked) void __cdecl Proxy_mmioDescend() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 488]
        test eax, eax
        jnz short resolved_122
        push ecx
        push edx
        push 124
        push offset kWinmmName_122
        push 122
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_122:
        jmp eax
    }
}
static const char kWinmmName_123[] = "mmioFlush";
extern "C" __declspec(naked) void __cdecl Proxy_mmioFlush() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 492]
        test eax, eax
        jnz short resolved_123
        push ecx
        push edx
        push 125
        push offset kWinmmName_123
        push 123
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_123:
        jmp eax
    }
}
static const char kWinmmName_124[] = "mmioGetInfo";
extern "C" __declspec(naked) void __cdecl Proxy_mmioGetInfo() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 496]
        test eax, eax
        jnz short resolved_124
        push ecx
        push edx
        push 126
        push offset kWinmmName_124
        push 124
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_124:
        jmp eax
    }
}
static const char kWinmmName_125[] = "mmioInstallIOProcA";
extern "C" __declspec(naked) void __cdecl Proxy_mmioInstallIOProcA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 500]
        test eax, eax
        jnz short resolved_125
        push ecx
        push edx
        push 127
        push offset kWinmmName_125
        push 125
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_125:
        jmp eax
    }
}
static const char kWinmmName_126[] = "mmioInstallIOProcW";
extern "C" __declspec(naked) void __cdecl Proxy_mmioInstallIOProcW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 504]
        test eax, eax
        jnz short resolved_126
        push ecx
        push edx
        push 128
        push offset kWinmmName_126
        push 126
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_126:
        jmp eax
    }
}
static const char kWinmmName_127[] = "mmioOpenA";
extern "C" __declspec(naked) void __cdecl Proxy_mmioOpenA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 508]
        test eax, eax
        jnz short resolved_127
        push ecx
        push edx
        push 129
        push offset kWinmmName_127
        push 127
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_127:
        jmp eax
    }
}
static const char kWinmmName_128[] = "mmioOpenW";
extern "C" __declspec(naked) void __cdecl Proxy_mmioOpenW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 512]
        test eax, eax
        jnz short resolved_128
        push ecx
        push edx
        push 130
        push offset kWinmmName_128
        push 128
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_128:
        jmp eax
    }
}
static const char kWinmmName_129[] = "mmioRead";
extern "C" __declspec(naked) void __cdecl Proxy_mmioRead() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 516]
        test eax, eax
        jnz short resolved_129
        push ecx
        push edx
        push 131
        push offset kWinmmName_129
        push 129
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_129:
        jmp eax
    }
}
static const char kWinmmName_130[] = "mmioRenameA";
extern "C" __declspec(naked) void __cdecl Proxy_mmioRenameA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 520]
        test eax, eax
        jnz short resolved_130
        push ecx
        push edx
        push 132
        push offset kWinmmName_130
        push 130
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_130:
        jmp eax
    }
}
static const char kWinmmName_131[] = "mmioRenameW";
extern "C" __declspec(naked) void __cdecl Proxy_mmioRenameW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 524]
        test eax, eax
        jnz short resolved_131
        push ecx
        push edx
        push 133
        push offset kWinmmName_131
        push 131
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_131:
        jmp eax
    }
}
static const char kWinmmName_132[] = "mmioSeek";
extern "C" __declspec(naked) void __cdecl Proxy_mmioSeek() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 528]
        test eax, eax
        jnz short resolved_132
        push ecx
        push edx
        push 134
        push offset kWinmmName_132
        push 132
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_132:
        jmp eax
    }
}
static const char kWinmmName_133[] = "mmioSendMessage";
extern "C" __declspec(naked) void __cdecl Proxy_mmioSendMessage() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 532]
        test eax, eax
        jnz short resolved_133
        push ecx
        push edx
        push 135
        push offset kWinmmName_133
        push 133
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_133:
        jmp eax
    }
}
static const char kWinmmName_134[] = "mmioSetBuffer";
extern "C" __declspec(naked) void __cdecl Proxy_mmioSetBuffer() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 536]
        test eax, eax
        jnz short resolved_134
        push ecx
        push edx
        push 136
        push offset kWinmmName_134
        push 134
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_134:
        jmp eax
    }
}
static const char kWinmmName_135[] = "mmioSetInfo";
extern "C" __declspec(naked) void __cdecl Proxy_mmioSetInfo() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 540]
        test eax, eax
        jnz short resolved_135
        push ecx
        push edx
        push 137
        push offset kWinmmName_135
        push 135
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_135:
        jmp eax
    }
}
static const char kWinmmName_136[] = "mmioStringToFOURCCA";
extern "C" __declspec(naked) void __cdecl Proxy_mmioStringToFOURCCA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 544]
        test eax, eax
        jnz short resolved_136
        push ecx
        push edx
        push 138
        push offset kWinmmName_136
        push 136
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_136:
        jmp eax
    }
}
static const char kWinmmName_137[] = "mmioStringToFOURCCW";
extern "C" __declspec(naked) void __cdecl Proxy_mmioStringToFOURCCW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 548]
        test eax, eax
        jnz short resolved_137
        push ecx
        push edx
        push 139
        push offset kWinmmName_137
        push 137
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_137:
        jmp eax
    }
}
static const char kWinmmName_138[] = "mmioWrite";
extern "C" __declspec(naked) void __cdecl Proxy_mmioWrite() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 552]
        test eax, eax
        jnz short resolved_138
        push ecx
        push edx
        push 140
        push offset kWinmmName_138
        push 138
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_138:
        jmp eax
    }
}
static const char kWinmmName_139[] = "mmsystemGetVersion";
extern "C" __declspec(naked) void __cdecl Proxy_mmsystemGetVersion() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 556]
        test eax, eax
        jnz short resolved_139
        push ecx
        push edx
        push 141
        push offset kWinmmName_139
        push 139
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_139:
        jmp eax
    }
}
static const char kWinmmName_140[] = "mod32Message";
extern "C" __declspec(naked) void __cdecl Proxy_mod32Message() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 560]
        test eax, eax
        jnz short resolved_140
        push ecx
        push edx
        push 142
        push offset kWinmmName_140
        push 140
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_140:
        jmp eax
    }
}
static const char kWinmmName_141[] = "mxd32Message";
extern "C" __declspec(naked) void __cdecl Proxy_mxd32Message() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 564]
        test eax, eax
        jnz short resolved_141
        push ecx
        push edx
        push 143
        push offset kWinmmName_141
        push 141
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_141:
        jmp eax
    }
}
static const char kWinmmName_142[] = "sndPlaySoundA";
extern "C" __declspec(naked) void __cdecl Proxy_sndPlaySoundA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 568]
        test eax, eax
        jnz short resolved_142
        push ecx
        push edx
        push 144
        push offset kWinmmName_142
        push 142
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_142:
        jmp eax
    }
}
static const char kWinmmName_143[] = "sndPlaySoundW";
extern "C" __declspec(naked) void __cdecl Proxy_sndPlaySoundW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 572]
        test eax, eax
        jnz short resolved_143
        push ecx
        push edx
        push 145
        push offset kWinmmName_143
        push 143
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_143:
        jmp eax
    }
}
static const char kWinmmName_144[] = "tid32Message";
extern "C" __declspec(naked) void __cdecl Proxy_tid32Message() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 576]
        test eax, eax
        jnz short resolved_144
        push ecx
        push edx
        push 146
        push offset kWinmmName_144
        push 144
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_144:
        jmp eax
    }
}
static const char kWinmmName_145[] = "timeBeginPeriod";
extern "C" __declspec(naked) void __cdecl Proxy_timeBeginPeriod() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 580]
        test eax, eax
        jnz short resolved_145
        push ecx
        push edx
        push 147
        push offset kWinmmName_145
        push 145
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_145:
        jmp eax
    }
}
static const char kWinmmName_146[] = "timeEndPeriod";
extern "C" __declspec(naked) void __cdecl Proxy_timeEndPeriod() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 584]
        test eax, eax
        jnz short resolved_146
        push ecx
        push edx
        push 148
        push offset kWinmmName_146
        push 146
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_146:
        jmp eax
    }
}
static const char kWinmmName_147[] = "timeGetDevCaps";
extern "C" __declspec(naked) void __cdecl Proxy_timeGetDevCaps() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 588]
        test eax, eax
        jnz short resolved_147
        push ecx
        push edx
        push 149
        push offset kWinmmName_147
        push 147
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_147:
        jmp eax
    }
}
static const char kWinmmName_148[] = "timeGetSystemTime";
extern "C" __declspec(naked) void __cdecl Proxy_timeGetSystemTime() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 592]
        test eax, eax
        jnz short resolved_148
        push ecx
        push edx
        push 150
        push offset kWinmmName_148
        push 148
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_148:
        jmp eax
    }
}
static const char kWinmmName_149[] = "timeGetTime";
extern "C" __declspec(naked) void __cdecl Proxy_timeGetTime() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 596]
        test eax, eax
        jnz short resolved_149
        push ecx
        push edx
        push 151
        push offset kWinmmName_149
        push 149
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_149:
        jmp eax
    }
}
static const char kWinmmName_150[] = "timeKillEvent";
extern "C" __declspec(naked) void __cdecl Proxy_timeKillEvent() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 600]
        test eax, eax
        jnz short resolved_150
        push ecx
        push edx
        push 152
        push offset kWinmmName_150
        push 150
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_150:
        jmp eax
    }
}
static const char kWinmmName_151[] = "timeSetEvent";
extern "C" __declspec(naked) void __cdecl Proxy_timeSetEvent() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 604]
        test eax, eax
        jnz short resolved_151
        push ecx
        push edx
        push 153
        push offset kWinmmName_151
        push 151
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_151:
        jmp eax
    }
}
static const char kWinmmName_152[] = "waveInAddBuffer";
extern "C" __declspec(naked) void __cdecl Proxy_waveInAddBuffer() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 608]
        test eax, eax
        jnz short resolved_152
        push ecx
        push edx
        push 154
        push offset kWinmmName_152
        push 152
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_152:
        jmp eax
    }
}
static const char kWinmmName_153[] = "waveInClose";
extern "C" __declspec(naked) void __cdecl Proxy_waveInClose() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 612]
        test eax, eax
        jnz short resolved_153
        push ecx
        push edx
        push 155
        push offset kWinmmName_153
        push 153
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_153:
        jmp eax
    }
}
static const char kWinmmName_154[] = "waveInGetDevCapsA";
extern "C" __declspec(naked) void __cdecl Proxy_waveInGetDevCapsA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 616]
        test eax, eax
        jnz short resolved_154
        push ecx
        push edx
        push 156
        push offset kWinmmName_154
        push 154
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_154:
        jmp eax
    }
}
static const char kWinmmName_155[] = "waveInGetDevCapsW";
extern "C" __declspec(naked) void __cdecl Proxy_waveInGetDevCapsW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 620]
        test eax, eax
        jnz short resolved_155
        push ecx
        push edx
        push 157
        push offset kWinmmName_155
        push 155
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_155:
        jmp eax
    }
}
static const char kWinmmName_156[] = "waveInGetErrorTextA";
extern "C" __declspec(naked) void __cdecl Proxy_waveInGetErrorTextA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 624]
        test eax, eax
        jnz short resolved_156
        push ecx
        push edx
        push 158
        push offset kWinmmName_156
        push 156
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_156:
        jmp eax
    }
}
static const char kWinmmName_157[] = "waveInGetErrorTextW";
extern "C" __declspec(naked) void __cdecl Proxy_waveInGetErrorTextW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 628]
        test eax, eax
        jnz short resolved_157
        push ecx
        push edx
        push 159
        push offset kWinmmName_157
        push 157
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_157:
        jmp eax
    }
}
static const char kWinmmName_158[] = "waveInGetID";
extern "C" __declspec(naked) void __cdecl Proxy_waveInGetID() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 632]
        test eax, eax
        jnz short resolved_158
        push ecx
        push edx
        push 160
        push offset kWinmmName_158
        push 158
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_158:
        jmp eax
    }
}
static const char kWinmmName_159[] = "waveInGetNumDevs";
extern "C" __declspec(naked) void __cdecl Proxy_waveInGetNumDevs() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 636]
        test eax, eax
        jnz short resolved_159
        push ecx
        push edx
        push 161
        push offset kWinmmName_159
        push 159
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_159:
        jmp eax
    }
}
static const char kWinmmName_160[] = "waveInGetPosition";
extern "C" __declspec(naked) void __cdecl Proxy_waveInGetPosition() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 640]
        test eax, eax
        jnz short resolved_160
        push ecx
        push edx
        push 162
        push offset kWinmmName_160
        push 160
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_160:
        jmp eax
    }
}
static const char kWinmmName_161[] = "waveInMessage";
extern "C" __declspec(naked) void __cdecl Proxy_waveInMessage() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 644]
        test eax, eax
        jnz short resolved_161
        push ecx
        push edx
        push 163
        push offset kWinmmName_161
        push 161
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_161:
        jmp eax
    }
}
static const char kWinmmName_162[] = "waveInOpen";
extern "C" __declspec(naked) void __cdecl Proxy_waveInOpen() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 648]
        test eax, eax
        jnz short resolved_162
        push ecx
        push edx
        push 164
        push offset kWinmmName_162
        push 162
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_162:
        jmp eax
    }
}
static const char kWinmmName_163[] = "waveInPrepareHeader";
extern "C" __declspec(naked) void __cdecl Proxy_waveInPrepareHeader() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 652]
        test eax, eax
        jnz short resolved_163
        push ecx
        push edx
        push 165
        push offset kWinmmName_163
        push 163
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_163:
        jmp eax
    }
}
static const char kWinmmName_164[] = "waveInReset";
extern "C" __declspec(naked) void __cdecl Proxy_waveInReset() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 656]
        test eax, eax
        jnz short resolved_164
        push ecx
        push edx
        push 166
        push offset kWinmmName_164
        push 164
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_164:
        jmp eax
    }
}
static const char kWinmmName_165[] = "waveInStart";
extern "C" __declspec(naked) void __cdecl Proxy_waveInStart() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 660]
        test eax, eax
        jnz short resolved_165
        push ecx
        push edx
        push 167
        push offset kWinmmName_165
        push 165
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_165:
        jmp eax
    }
}
static const char kWinmmName_166[] = "waveInStop";
extern "C" __declspec(naked) void __cdecl Proxy_waveInStop() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 664]
        test eax, eax
        jnz short resolved_166
        push ecx
        push edx
        push 168
        push offset kWinmmName_166
        push 166
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_166:
        jmp eax
    }
}
static const char kWinmmName_167[] = "waveInUnprepareHeader";
extern "C" __declspec(naked) void __cdecl Proxy_waveInUnprepareHeader() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 668]
        test eax, eax
        jnz short resolved_167
        push ecx
        push edx
        push 169
        push offset kWinmmName_167
        push 167
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_167:
        jmp eax
    }
}
static const char kWinmmName_168[] = "waveOutBreakLoop";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutBreakLoop() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 672]
        test eax, eax
        jnz short resolved_168
        push ecx
        push edx
        push 170
        push offset kWinmmName_168
        push 168
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_168:
        jmp eax
    }
}
static const char kWinmmName_169[] = "waveOutClose";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutClose() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 676]
        test eax, eax
        jnz short resolved_169
        push ecx
        push edx
        push 171
        push offset kWinmmName_169
        push 169
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_169:
        jmp eax
    }
}
static const char kWinmmName_170[] = "waveOutGetDevCapsA";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutGetDevCapsA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 680]
        test eax, eax
        jnz short resolved_170
        push ecx
        push edx
        push 172
        push offset kWinmmName_170
        push 170
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_170:
        jmp eax
    }
}
static const char kWinmmName_171[] = "waveOutGetDevCapsW";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutGetDevCapsW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 684]
        test eax, eax
        jnz short resolved_171
        push ecx
        push edx
        push 173
        push offset kWinmmName_171
        push 171
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_171:
        jmp eax
    }
}
static const char kWinmmName_172[] = "waveOutGetErrorTextA";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutGetErrorTextA() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 688]
        test eax, eax
        jnz short resolved_172
        push ecx
        push edx
        push 174
        push offset kWinmmName_172
        push 172
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_172:
        jmp eax
    }
}
static const char kWinmmName_173[] = "waveOutGetErrorTextW";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutGetErrorTextW() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 692]
        test eax, eax
        jnz short resolved_173
        push ecx
        push edx
        push 175
        push offset kWinmmName_173
        push 173
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_173:
        jmp eax
    }
}
static const char kWinmmName_174[] = "waveOutGetID";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutGetID() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 696]
        test eax, eax
        jnz short resolved_174
        push ecx
        push edx
        push 176
        push offset kWinmmName_174
        push 174
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_174:
        jmp eax
    }
}
static const char kWinmmName_175[] = "waveOutGetNumDevs";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutGetNumDevs() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 700]
        test eax, eax
        jnz short resolved_175
        push ecx
        push edx
        push 177
        push offset kWinmmName_175
        push 175
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_175:
        jmp eax
    }
}
static const char kWinmmName_176[] = "waveOutGetPitch";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutGetPitch() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 704]
        test eax, eax
        jnz short resolved_176
        push ecx
        push edx
        push 178
        push offset kWinmmName_176
        push 176
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_176:
        jmp eax
    }
}
static const char kWinmmName_177[] = "waveOutGetPlaybackRate";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutGetPlaybackRate() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 708]
        test eax, eax
        jnz short resolved_177
        push ecx
        push edx
        push 179
        push offset kWinmmName_177
        push 177
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_177:
        jmp eax
    }
}
static const char kWinmmName_178[] = "waveOutGetPosition";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutGetPosition() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 712]
        test eax, eax
        jnz short resolved_178
        push ecx
        push edx
        push 180
        push offset kWinmmName_178
        push 178
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_178:
        jmp eax
    }
}
static const char kWinmmName_179[] = "waveOutGetVolume";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutGetVolume() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 716]
        test eax, eax
        jnz short resolved_179
        push ecx
        push edx
        push 181
        push offset kWinmmName_179
        push 179
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_179:
        jmp eax
    }
}
static const char kWinmmName_180[] = "waveOutMessage";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutMessage() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 720]
        test eax, eax
        jnz short resolved_180
        push ecx
        push edx
        push 182
        push offset kWinmmName_180
        push 180
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_180:
        jmp eax
    }
}
static const char kWinmmName_181[] = "waveOutOpen";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutOpen() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 724]
        test eax, eax
        jnz short resolved_181
        push ecx
        push edx
        push 183
        push offset kWinmmName_181
        push 181
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_181:
        jmp eax
    }
}
static const char kWinmmName_182[] = "waveOutPause";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutPause() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 728]
        test eax, eax
        jnz short resolved_182
        push ecx
        push edx
        push 184
        push offset kWinmmName_182
        push 182
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_182:
        jmp eax
    }
}
static const char kWinmmName_183[] = "waveOutPrepareHeader";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutPrepareHeader() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 732]
        test eax, eax
        jnz short resolved_183
        push ecx
        push edx
        push 185
        push offset kWinmmName_183
        push 183
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_183:
        jmp eax
    }
}
static const char kWinmmName_184[] = "waveOutReset";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutReset() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 736]
        test eax, eax
        jnz short resolved_184
        push ecx
        push edx
        push 186
        push offset kWinmmName_184
        push 184
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_184:
        jmp eax
    }
}
static const char kWinmmName_185[] = "waveOutRestart";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutRestart() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 740]
        test eax, eax
        jnz short resolved_185
        push ecx
        push edx
        push 187
        push offset kWinmmName_185
        push 185
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_185:
        jmp eax
    }
}
static const char kWinmmName_186[] = "waveOutSetPitch";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutSetPitch() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 744]
        test eax, eax
        jnz short resolved_186
        push ecx
        push edx
        push 188
        push offset kWinmmName_186
        push 186
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_186:
        jmp eax
    }
}
static const char kWinmmName_187[] = "waveOutSetPlaybackRate";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutSetPlaybackRate() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 748]
        test eax, eax
        jnz short resolved_187
        push ecx
        push edx
        push 189
        push offset kWinmmName_187
        push 187
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_187:
        jmp eax
    }
}
static const char kWinmmName_188[] = "waveOutSetVolume";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutSetVolume() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 752]
        test eax, eax
        jnz short resolved_188
        push ecx
        push edx
        push 190
        push offset kWinmmName_188
        push 188
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_188:
        jmp eax
    }
}
static const char kWinmmName_189[] = "waveOutUnprepareHeader";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutUnprepareHeader() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 756]
        test eax, eax
        jnz short resolved_189
        push ecx
        push edx
        push 191
        push offset kWinmmName_189
        push 189
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_189:
        jmp eax
    }
}
static const char kWinmmName_190[] = "waveOutWrite";
extern "C" __declspec(naked) void __cdecl Proxy_waveOutWrite() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 760]
        test eax, eax
        jnz short resolved_190
        push ecx
        push edx
        push 192
        push offset kWinmmName_190
        push 190
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_190:
        jmp eax
    }
}
static const char kWinmmName_191[] = "wid32Message";
extern "C" __declspec(naked) void __cdecl Proxy_wid32Message() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 764]
        test eax, eax
        jnz short resolved_191
        push ecx
        push edx
        push 193
        push offset kWinmmName_191
        push 191
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_191:
        jmp eax
    }
}
static const char kWinmmName_192[] = "wod32Message";
extern "C" __declspec(naked) void __cdecl Proxy_wod32Message() {
    __asm {
        mov eax, dword ptr [g_WinmmProcs + 768]
        test eax, eax
        jnz short resolved_192
        push ecx
        push edx
        push 194
        push offset kWinmmName_192
        push 192
        call EnsureWinmmProc
        pop edx
        pop ecx
    resolved_192:
        jmp eax
    }
}
