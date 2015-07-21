#include "pch.h"
#include "aiLogger.h"

aiLogger aiLogger::msInstance;

aiLogger::aiLogger()
   : mPath(""), mFile(0)
{
}

aiLogger::~aiLogger()
{
   close();
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
      
      close();
      
      mPath = path;
      mFile = fopen(path, "a");
      
      if (!mFile)
      {
         mPath = "";
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

void aiLogger::close()
{
   if (mFile)
   {
      fclose(mFile);
      mFile = 0;
   }
   
   mPath = "";
}

void aiLogger::warning(const char *fmt, va_list args)
{
   if (isOpened())
   {
      fprintf(mFile, "WARNING: ");
      vfprintf(mFile, fmt, args);
      fflush(mFile);
   }
}

void aiLogger::error(const char *fmt, va_list args)
{
   if (isOpened())
   {
      fprintf(mFile, "ERROR: ");
      vfprintf(mFile, fmt, args);
      fflush(mFile);
   }
}

void aiLogger::info(const char *fmt, va_list args)
{
   if (isOpened())
   {
      fprintf(mFile, "INFO: ");
      vfprintf(mFile, fmt, args);
      fflush(mFile);
   }
}
