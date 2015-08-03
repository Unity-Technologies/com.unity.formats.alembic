#include "pch.h"
#include "aiLogger.h"

aiLogger aiLogger::msInstance;

#ifdef _WIN32
static const char *gDebugLogPath = "c:/unity.alembic.log";
#else
static const char *gDebugLogPath = "/tmp/unity.alembic.log";
#endif

aiLogger::aiLogger()
   : mPath("")
   , mFile(0)
   , mIndentLevel(0)
   , mIndentString("")
{
   _openDebug();
}

aiLogger::~aiLogger()
{
   _close();
}

void aiLogger::_openDebug()
{
#if defined(aiDebug) || defined(aiDebugLog)
   mFile = fopen(gDebugLogPath, "a");   
   mPath = (mFile != 0 ? gDebugLogPath : "");
#endif
}

void aiLogger::open(const char *path)
{
   if (path && strlen(path) > 0)
   {
      if (mFile && mPath == path)
      {
         // already opened on writing to same file
         return;
      }
      
      // whatever log file is opened, close it
      _close();
      
      mPath = path;
      mFile = fopen(path, "a");
      
      if (!mFile)
      {
         mPath = "";
         _openDebug();
      }
   }
   else
   {
      close();
   }
}

bool aiLogger::isOpened() const
{
   return (mFile != 0);
}

void aiLogger::indent(int level)
{
   mIndentLevel += level;
   _indent();
}

void aiLogger::unindent(int level)
{
   mIndentLevel -= level;
   if (mIndentLevel < 0)
   {
      mIndentLevel = 0;
   }
   _indent();
}

void aiLogger::close()
{
#if defined(aiDebug) || defined(aiDebugLog)
   // do not close the debug log file
   if (mFile && mPath != gDebugLogPath)
   {
      _close();
      _openDebug();
   }
#else
   _close();
   _openDebug();
#endif
}

void aiLogger::_close()
{
   if (mFile)
   {
      fclose(mFile);
      mFile = 0;
   }
   
   mPath = "";
}

void aiLogger::_indent()
{
   mIndentString = "";
   for (int i=0; i<mIndentLevel; ++i)
   {
      mIndentString += "  ";
   }
}

void aiLogger::warning(const char *fmt, va_list args)
{
   if (isOpened())
   {
      fprintf(mFile, "[WARNING] %s", mIndentString.c_str());
      vfprintf(mFile, fmt, args);
      fprintf(mFile, "\n");
      fflush(mFile);
   }
}

void aiLogger::error(const char *fmt, va_list args)
{
   if (isOpened())
   {
      fprintf(mFile, "[ ERROR ] %s", mIndentString.c_str());
      vfprintf(mFile, fmt, args);
      fprintf(mFile, "\n");
      fflush(mFile);
   }
}

void aiLogger::info(const char *fmt, va_list args)
{
   if (isOpened())
   {
      fprintf(mFile, "[ INFO  ] %s", mIndentString.c_str());
      vfprintf(mFile, fmt, args);
      fprintf(mFile, "\n");
      fflush(mFile);
   }
}

void aiLogger::debug(const char *fmt, va_list args)
{
   if (isOpened())
   {
      fprintf(mFile, "[ DEBUG ] %s", mIndentString.c_str());
      vfprintf(mFile, fmt, args);
      fprintf(mFile, "\n");
      fflush(mFile);
   }
}
