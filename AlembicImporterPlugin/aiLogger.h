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
   
   static inline void Indent(int level=1)
   {
      msInstance.indent(level);
   }

   static inline void Unindent(int level=1)
   {
      msInstance.unindent(level);
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
   
   static inline void Debug(const char *fmt, ...)
   {
      if (!msInstance.isOpened())
      {
         return;
      }
      
      va_list args;
      va_start(args, fmt);
      
      msInstance.debug(fmt, args);
      
      va_end(args);
   }
   
private:
   
   aiLogger();
   ~aiLogger();
   
   void open(const char *path);
   void close();
   
   bool isOpened() const;
   
   void indent(int level);
   void unindent(int level);

   void warning(const char *fmt, va_list args);
   void error(const char *fmt, va_list args);
   void info(const char *fmt, va_list args);
   void debug(const char *fmt, va_list args);

private:
   
   void _openDebug();
   void _close();
   void _indent();
   
   std::string mPath;
   FILE *mFile;
   int mIndentLevel;
   std::string mIndentString;

private:
   
   static aiLogger msInstance;
};

#endif
