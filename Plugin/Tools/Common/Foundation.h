#ifndef tFoundation_h
#define tFoundation_h

#define tCLinkage extern "C"
#ifdef _MSC_VER
    #define tExport __declspec(dllexport)
#else
    #define tExport __attribute__((visibility("default")))
#endif



typedef void(__stdcall *tLogCallback)(const char *);
void tLogSetCallback(tLogCallback cb);
void tLog(const char *format, ...);

void tAddDLLSearchPath(const char *path_to_add);
const char* tGetDirectoryOfCurrentModule();

double tGetTime(); // in milliseconds



template<class T> struct tvec1;
template<class T> struct tvec2;
template<class T> struct tvec3;
template<class T> struct tvec4;

// i know "vec1" seems silly. but this is convenient for some cases (e.g. conversion. see TextureWriter)
template<class T> struct tvec1
{
    T x;
    tvec1() {} // !not clear members!
    tvec1(T a) : x(a) {}
    operator T&() { return x; }
    template<class U> tvec1(const tvec1<U>& src);
    template<class U> tvec1(const tvec2<U>& src);
    template<class U> tvec1(const tvec3<U>& src);
    template<class U> tvec1(const tvec4<U>& src);
};

template<class T> struct tvec2
{
    T x, y;
    tvec2() {} // !not clear members!
    tvec2(T a) : x(a), y(a) {}
    tvec2(T a, T b) : x(a), y(b) {}
    template<class U> tvec2(const tvec1<U>& src);
    template<class U> tvec2(const tvec2<U>& src);
    template<class U> tvec2(const tvec3<U>& src);
    template<class U> tvec2(const tvec4<U>& src);
};

template<class T> struct tvec3
{
    T x, y, z;
    tvec3() {} // !not clear members!
    tvec3(T a) : x(a), y(a), z(a) {}
    tvec3(T a, T b, T c) : x(a), y(b), z(c) {}
    template<class U> tvec3(const tvec1<U>& src);
    template<class U> tvec3(const tvec2<U>& src);
    template<class U> tvec3(const tvec3<U>& src);
    template<class U> tvec3(const tvec4<U>& src);
};

template<class T> struct tvec4
{
    T x, y, z, w;
    tvec4() {} // !not clear members!
    tvec4(T a) : x(a), y(a), z(a), w(a) {}
    tvec4(T a, T b, T c, T d) : x(a), y(b), z(c), w(d) {}
    template<class U> tvec4(const tvec1<U>& src);
    template<class U> tvec4(const tvec2<U>& src);
    template<class U> tvec4(const tvec3<U>& src);
    template<class U> tvec4(const tvec4<U>& src);
};

template<class DstType, class SrcType> inline DstType tScalar(SrcType src) { return DstType(src); }

template<class T> template<class U> tvec1<T>::tvec1(const tvec1<U>& src) : x(tScalar<T>(src.x)) {}
template<class T> template<class U> tvec1<T>::tvec1(const tvec2<U>& src) : x(tScalar<T>(src.x)) {}
template<class T> template<class U> tvec1<T>::tvec1(const tvec3<U>& src) : x(tScalar<T>(src.x)) {}
template<class T> template<class U> tvec1<T>::tvec1(const tvec4<U>& src) : x(tScalar<T>(src.x)) {}

template<class T> template<class U> tvec2<T>::tvec2(const tvec1<U>& src) : x(tScalar<T>(src.x)), y() {}
template<class T> template<class U> tvec2<T>::tvec2(const tvec2<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)) {}
template<class T> template<class U> tvec2<T>::tvec2(const tvec3<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)) {}
template<class T> template<class U> tvec2<T>::tvec2(const tvec4<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)) {}

template<class T> template<class U> tvec3<T>::tvec3(const tvec1<U>& src) : x(tScalar<T>(src.x)), y(), z() {}
template<class T> template<class U> tvec3<T>::tvec3(const tvec2<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)), z() {}
template<class T> template<class U> tvec3<T>::tvec3(const tvec3<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)), z(tScalar<T>(src.z)) {}
template<class T> template<class U> tvec3<T>::tvec3(const tvec4<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)), z(tScalar<T>(src.z)) {}

template<class T> template<class U> tvec4<T>::tvec4(const tvec1<U>& src) : x(tScalar<T>(src.x)), y(), z(), w() {}
template<class T> template<class U> tvec4<T>::tvec4(const tvec2<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)), z(), w() {}
template<class T> template<class U> tvec4<T>::tvec4(const tvec3<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)), z(tScalar<T>(src.z)), w() {}
template<class T> template<class U> tvec4<T>::tvec4(const tvec4<U>& src) : x(tScalar<T>(src.x)), y(tScalar<T>(src.y)), z(tScalar<T>(src.z)), w(tScalar<T>(src.w)) {}

template<class T> tvec3<T> operator+(const tvec3<T> &a, const tvec3<T> &b) { return tvec3<T>(a.x + b.x, a.y + b.y, a.z + b.z); }
template<class T> tvec3<T> operator-(const tvec3<T> &a, const tvec3<T> &b) { return tvec3<T>(a.x - b.x, a.y - b.y, a.z - b.z); }
template<class T> tvec3<T> operator*(const tvec3<T> &a, const tvec3<T> &b) { return tvec3<T>(a.x * b.x, a.y * b.y, a.z * b.z); }
template<class T> tvec3<T> operator/(const tvec3<T> &a, const tvec3<T> &b) { return tvec3<T>(a.x / b.x, a.y / b.y, a.z / b.z); }
template<class T> tvec3<T> operator*(const tvec3<T> &a, T b) { return tvec3<T>(a.x * b, a.y * b, a.z * b); }
template<class T> tvec3<T> operator/(const tvec3<T> &a, T b) { return tvec3<T>(a.x / b, a.y / b, a.z / b); }

typedef int64_t lint;
typedef tvec1<float> float1;
typedef tvec2<float> float2;
typedef tvec3<float> float3;
typedef tvec4<float> float4;
typedef tvec1<int> int1;
typedef tvec2<int> int2;
typedef tvec3<int> int3;
typedef tvec4<int> int4;
typedef tvec1<lint> lint1;
typedef tvec2<lint> lint2;
typedef tvec3<lint> lint3;
typedef tvec4<lint> lint4;


template<class IntType>
inline IntType ceildiv(IntType a, IntType b)
{
    return a / b + (a%b == 0 ? 0 : 1);
}

template<class Scalar>
inline Scalar clamp(Scalar v, Scalar min, Scalar max)
{
    return std::min<Scalar>(std::max<Scalar>(v, min), max);
}


void tRandSetSeed(uint32_t seed);
float tRand(); // return -1.0 ~ 1.0
float3 tRand3();


template<class T>
struct RGBA
{
    T r, g, b, a;
};

template<class T>
inline void BGRA2RGBA(RGBA<T> *pixels, size_t num_pixels)
{
    for (size_t i = 0; i < num_pixels; ++i) {
        std::swap(pixels[i].r, pixels[i].b);
    }
}

template<class T>
inline void CopyWithBGRA2RGBA(RGBA<T> *dst, const RGBA<T> *src, size_t num_pixels)
{
    for (size_t i = 0; i < num_pixels; ++i) {
        RGBA<T> &d = dst[i];
        const RGBA<T> &s = src[i];
        d.r = s.b;
        d.g = s.g;
        d.b = s.r;
        d.a = s.a;
    }
}


#endif // tFoundation_h
