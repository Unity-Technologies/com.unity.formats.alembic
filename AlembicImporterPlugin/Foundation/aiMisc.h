#ifndef aiMisc_h
#define aiMisc_h

template<class IntType> inline IntType ceildiv(IntType a, IntType b) { return a / b + (a%b == 0 ? 0 : 1); }
template<class IntType> inline IntType ceilup(IntType a, IntType b) { return ceildiv(a, b) * b; }

#endif // aiMisc_h
