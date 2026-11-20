#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <cstdint>
#include <cstring>

namespace {

inline bool IsWritablePtr(const void* p, size_t bytes) {
    return reinterpret_cast<uintptr_t>(p) > 0x10000u && !IsBadWritePtr(const_cast<void*>(p), static_cast<UINT_PTR>(bytes));
}

// 38-slot iRepresentShell implementation backed by zero GPU/GDI state.
// Every non-variadic virtual method uses MSVC x86 __thiscall (`this` in ECX, callee stack cleanup RET N).
class NullRepresentShell {
public:
    // slot 00: RET 0
    virtual void Release() {
        delete this;
    }

    // slot 01: RET 0x0c
    virtual int Create(int nWidth, int nHeight, int bFullScreen) {
        width_ = nWidth > 0 ? nWidth : 800;
        height_ = nHeight > 0 ? nHeight : 600;
        (void)bFullScreen;
        return 1;
    }

    // slot 02: RET 0x0c
    virtual int Reset(int nWidth, int nHeight, int bFullScreen) {
        width_ = nWidth > 0 ? nWidth : 800;
        height_ = nHeight > 0 ? nHeight : 600;
        (void)bFullScreen;
        return 1;
    }

    // slot 03: RET 0x0c
    virtual int CreateAFont(const char* pszFontFile, int nCharacterSet, int nId) {
        (void)pszFontFile;
        (void)nCharacterSet;
        (void)nId;
        return 1;
    }

    // slot 04: RET 0x24 (9 args)
    virtual void OutputText(int nFontId, const char* psText, int nCount, int nX, int nY,
                            uint32_t uColor, int nLineWidth, int nZ, uint32_t uBorderColor) {
        (void)nFontId; (void)psText; (void)nCount; (void)nX; (void)nY;
        (void)uColor; (void)nLineWidth; (void)nZ; (void)uBorderColor;
    }

    // slot 05: RET 0x14 (5 args)
    virtual int OutputRichText(int nFontId, void* pParam, const char* psText, int nCount, int nLineWidth) {
        (void)nFontId; (void)pParam; (void)psText; (void)nCount; (void)nLineWidth;
        return 0;
    }

    // slot 06: RET 0x1c (7 args)
    virtual int LocateRichText(int nX, int nY, int nFontId, void* pParam, const char* psText, int nCount, int nLineWidth) {
        (void)nX; (void)nY; (void)nFontId; (void)pParam; (void)psText; (void)nCount; (void)nLineWidth;
        return -1;
    }

    // slot 07: RET 0x04
    virtual void ReleaseAFont(int nId) {
        (void)nId;
    }

    // slot 08: RET 0x10 (4 args)
    virtual uint32_t CreateImage(const char* pszName, int nWidth, int nHeight, int nType) {
        (void)pszName; (void)nWidth; (void)nHeight; (void)nType;
        return 1;
    }

    // slot 09: RET 0x04
    virtual void FreeImage(const char* pszImage) {
        (void)pszImage;
    }

    // slot 10: RET 0
    virtual void FreeAllImage() {
    }

    // slot 11: RET 0x08
    virtual void* GetBitmapDataBuffer(const char* pszImage, void* pInfo) {
        (void)pszImage; (void)pInfo;
        return nullptr;
    }

    // slot 12: RET 0x08
    virtual void ReleaseBitmapDataBuffer(const char* pszImage, void* pBuffer) {
        (void)pszImage; (void)pBuffer;
    }

    // slot 13 (+0x34): RET 0x0c — KImageParam is 7 WORDs (14 bytes) in game.exe
    virtual int GetImageParam(const char* pszImage, uint16_t* pParam7Words, int nType) {
        (void)pszImage; (void)nType;
        if (IsWritablePtr(pParam7Words, 14)) {
            pParam7Words[0] = 1;  // nNumFrames
            pParam7Words[1] = 0;  // nInterval
            pParam7Words[2] = 32; // nWidth
            pParam7Words[3] = 32; // nHeight
            pParam7Words[4] = 1;  // nNumFramesPerDirection / ref point
            pParam7Words[5] = 0;
            pParam7Words[6] = 0;
        }
        return 1;
    }

    // slot 14 (+0x38): RET 0x14 (5 args)
    virtual int GetImageFrameParam(const char* pszImage, int nFrame, int* pOffset2Ints, int* pSize2Ints, int nType) {
        (void)pszImage; (void)nFrame; (void)nType;
        if (IsWritablePtr(pOffset2Ints, 8)) {
            pOffset2Ints[0] = 0;
            pOffset2Ints[1] = 0;
        }
        if (IsWritablePtr(pSize2Ints, 8)) {
            pSize2Ints[0] = 32;
            pSize2Ints[1] = 32;
        }
        return 1;
    }

    // slot 15 (+0x3c): RET 0x14 (5 args)
    virtual int GetImagePixelAlpha(const char* pszImage, int nFrame, int nX, int nY, int nType) {
        (void)pszImage; (void)nFrame; (void)nX; (void)nY; (void)nType;
        return 0;
    }

    // slot 16 (+0x40): RET 0x08
    virtual void SetAdjustColorList(uint32_t* puColorList, uint32_t uCount) {
        (void)puColorList; (void)uCount;
    }

    // slot 17 (+0x44): RET 0x0c (3 args)
    virtual int Slot17(int a1, int a2, int a3) {
        (void)a1; (void)a2; (void)a3;
        return 0;
    }

    // slot 18 (+0x48): RET 0x10 (4 args)
    virtual void DrawPrimitives(int nPrimitiveCount, void* pPrimitives, uint32_t uGenre, int bSinglePlaneCoord) {
        (void)nPrimitiveCount; (void)pPrimitives; (void)uGenre; (void)bSinglePlaneCoord;
    }

    // slot 19 (+0x4c): RET 0x18 (6 args)
    virtual void DrawPrimitivesOnImage(int nPrimitiveCount, void* pPrimitives, uint32_t uGenre,
                                       const char* pszImage, uint32_t uImage, void* pImageData) {
        (void)nPrimitiveCount; (void)pPrimitives; (void)uGenre;
        (void)pszImage; (void)uImage; (void)pImageData;
    }

    // slot 20 (+0x50): RET 0x0c (3 args)
    virtual void ClearImageData(const char* pszImage, uint32_t uImage, int nImagePosition) {
        (void)pszImage; (void)uImage; (void)nImagePosition;
    }

    // slot 21 (+0x54): RET 0x0c (3 args)
    virtual void LookAt(int nX, int nY, int nZ) {
        (void)nX; (void)nY; (void)nZ;
    }

    // slot 22 (+0x58): RET 0x1c (7 args)
    virtual int CopyDeviceImageToImage(const char* pszName, int nDeviceX, int nDeviceY,
                                       int nImageX, int nImageY, int nWidth, int nHeight) {
        (void)pszName; (void)nDeviceX; (void)nDeviceY;
        (void)nImageX; (void)nImageY; (void)nWidth; (void)nHeight;
        return 1;
    }

    // slot 23 (+0x5c): RET 0x08
    virtual int RepresentBegin(int bClear, uint32_t uColor) {
        (void)bClear; (void)uColor;
        return 1;
    }

    // slot 24 (+0x60): RET 0
    virtual void RepresentEnd() {
    }

    // slot 25 (+0x64): RET 0x0c (3 args)
    virtual void ViewPortCoordToSpaceCoord(int* pX, int* pY, int nZ) {
        (void)pX; (void)pY; (void)nZ;
    }

    // slot 26 (+0x68): RET 0x0c (3 args)
    virtual void Slot26(int a1, int a2, int a3) {
        (void)a1; (void)a2; (void)a3;
    }

    // slot 27 (+0x6c): RET 0x08
    virtual void SetOption(int nOption, int nValue) {
        (void)nOption; (void)nValue;
    }

    // slot 28 (+0x70): RET 0x08 — MUST return non-zero (100 for option 0) to prevent divide-by-zero in KUiGameSpace
    virtual int GetOption(int nOption, int* pnValue) {
        int val = (nOption == 0) ? 100 : 1;
        if (IsWritablePtr(pnValue, sizeof(int))) {
            *pnValue = val;
        }
        return val;
    }

    // slot 29 (+0x74): RET 0x0c (3 args)
    virtual int SaveScreenToFile(const char* pszName, int eType, int nQuality) {
        (void)pszName; (void)eType; (void)nQuality;
        return 1;
    }

    // slot 30 (+0x78): RET 0x04
    virtual int Slot30(int a1) {
        (void)a1;
        return 1;
    }

    // slot 31 (+0x7c): RET 0x08
    virtual int Slot31(int a1, int a2) {
        (void)a1; (void)a2;
        return 0;
    }

    // slot 32 (+0x80): RET 0x08
    virtual void Slot32(int a1, int a2) {
        (void)a1; (void)a2;
    }

    // slot 33 (+0x84): RET 0x04
    virtual void Slot33(int a1) {
        (void)a1;
    }

    // slot 34 (+0x88): RET 0x04
    virtual void Slot34(int a1) {
        (void)a1;
    }

    // slot 35 (+0x8c): RET 0x0c (3 args)
    virtual int Slot35(int a1, int a2, int a3) {
        (void)a1; (void)a2; (void)a3;
        return 0;
    }

    // slot 36 (+0x90): RET 0x04 — callers dereference returned RECT* in EAX!
    virtual RECT* GetViewport(RECT* pRect) {
        if (IsWritablePtr(pRect, sizeof(RECT))) {
            pRect->left = 0;
            pRect->top = 0;
            pRect->right = width_;
            pRect->bottom = height_;
        }
        return pRect;
    }

    // slot 37 (+0x94): RET 0x04
    virtual int Slot37(int a1) {
        (void)a1;
        return 0;
    }

private:
    int width_ = 800;
    int height_ = 600;
};

} // namespace

extern "C" void* __cdecl CreateRepresentShell() {
    return new NullRepresentShell();
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID reserved) {
    (void)reserved;
    if (reason == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hModule);
    }
    return TRUE;
}
