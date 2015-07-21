#ifndef __aiLogger_h__
#define __aiLogger_h__

#include <string>
#include <cstdio>
#include <cstdarg>

class aiLogger
{
public:

   static inline void Enable(bool on, const char *path)
   {
      if (on)
      {
         msInstance.open(path);
      }
      else
      {
         msInstance.close();
      }
   }
   
   static inline void Warning(const char *fmt, ...)
   {
      if (!msInstance.isOpened())
      {
         return;
      }

      va_list args;
      va_start(args, fmt);
      
      msInstance.warning(fmt, args);
      
      va_end(args);
   }

   static inline void Error(const char *fmt, ...)
   {
      if (!msInstance.isOpened())
      {
         return;
      }

      va_list args;
      va_start(args, fmt);
      
      msInstance.error(fmt, args);
      
      va_end(args);
   }

   static inline void Info(const char *fmt, ...)
   {
      if (!msInstance.isOpened())
      {
         return;
      }
      
      va_list args;
      va_start(args, fmt);
      
      msInstance.info(fmt, args);
      
      va_end(args);
   }
   
private:
   
   aiLogger();
   ~aiLogger();
   
   void open(const char *path);
   void close();
   
   bool isOpened() const;
   
   void warning(const char *fmt, va_list args);
   void error(const char *fmt, va_list args);
   void info(const char *fmt, va_list args);

private:
   
   std::string mPath;
   FILE *mFile;

private:
   
   static aiLogger msInstance;
};

#endif
