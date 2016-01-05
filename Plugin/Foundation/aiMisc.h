#ifndef aiMisc_h
#define aiMisc_h

template<class IntType> inline IntType ceildiv(IntType a, IntType b) { return a / b + (a%b == 0 ? 0 : 1); }
template<class IntType> inline IntType ceilup(IntType a, IntType b) { return ceildiv(a, b) * b; }

template<class T>
struct RGBA
{
    T r, g, b, a;
};

template<class T>
inline void BGRA2RGBA(RGBA<T> *data, int numPixels)
{
    for (int i = 0; i < numPixels; ++i) {
        std::swap(data[i].r, data[i].b);
    }
}

template<class T>
inline void CopyWithBGRA2RGBA(RGBA<T> *dst, const RGBA<T> *src, int numPixels)
{
    for (int i = 0; i < numPixels; ++i) {
        RGBA<T> &d = dst[i];
        const RGBA<T> &s = src[i];
        d.r = s.b;
        d.g = s.g;
        d.b = s.r;
        d.a = s.a;
    }
}


#endif // aiMisc_h
