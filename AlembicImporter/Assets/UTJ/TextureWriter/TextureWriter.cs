using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UTJ
{
    public class TextureWriter
    {
        public enum tTextureFormat
        {
            Unknown,
            ARGB32,
            ARGB2101010,
            RHalf,
            RGHalf,
            ARGBHalf,
            RFloat,
            RGFloat,
            ARGBFloat,
            RInt,
            RGInt,
            ARGBInt,
        };

        public enum tDataFormat
        {
            Unknown,
            Half,
            Half2,
            Half3,
            Half4,
            Float,
            Float2,
            Float3,
            Float4,
            Int,
            Int2,
            Int3,
            Int4,
            LInt, // 64bit int
        };


        public static tTextureFormat GetTextureFormat(RenderTexture v)
        {
            switch (v.format)
            {
                case RenderTextureFormat.ARGB32:    return tTextureFormat.ARGB32;
                case RenderTextureFormat.RHalf:     return tTextureFormat.RHalf;
                case RenderTextureFormat.RGHalf:    return tTextureFormat.RGHalf;
                case RenderTextureFormat.ARGBHalf:  return tTextureFormat.ARGBHalf;
                case RenderTextureFormat.RFloat:    return tTextureFormat.RFloat;
                case RenderTextureFormat.RGFloat:   return tTextureFormat.RGFloat;
                case RenderTextureFormat.ARGBFloat: return tTextureFormat.ARGBFloat;
                case RenderTextureFormat.RInt:      return tTextureFormat.RInt;
                case RenderTextureFormat.RGInt:     return tTextureFormat.RGInt;
                case RenderTextureFormat.ARGBInt:   return tTextureFormat.ARGBInt;
            }
            return tTextureFormat.Unknown;
        }

        public static tTextureFormat GetTextureFormat(Texture2D v)
        {
            switch (v.format)
            {
                case TextureFormat.ARGB32:      return tTextureFormat.ARGB32;
                case TextureFormat.RHalf:       return tTextureFormat.RHalf;
                case TextureFormat.RGHalf:      return tTextureFormat.RGHalf;
                case TextureFormat.RGBAHalf:    return tTextureFormat.ARGBHalf;
                case TextureFormat.RFloat:      return tTextureFormat.RFloat;
                case TextureFormat.RGFloat:     return tTextureFormat.RGFloat;
                case TextureFormat.RGBAFloat:   return tTextureFormat.ARGBFloat;
            }
            return tTextureFormat.Unknown;
        }

        [DllImport("TextureWriter")]
        private static extern int tWriteTexture(
            IntPtr dst_tex, int dst_width, int dst_height, tTextureFormat dst_fmt,
            IntPtr src, int src_num, tDataFormat src_fmt);

        public static bool Write(RenderTexture dst_tex, IntPtr src, int src_num, tDataFormat src_fmt)
        {
            return tWriteTexture(
                dst_tex.GetNativeTexturePtr(), dst_tex.width, dst_tex.height,
                GetTextureFormat(dst_tex), src, src_num, src_fmt) != 0;
        }

        public static bool Write(Texture2D dst_tex, IntPtr src, int src_num, tDataFormat src_fmt)
        {
            return tWriteTexture(
                dst_tex.GetNativeTexturePtr(), dst_tex.width, dst_tex.height,
                GetTextureFormat(dst_tex), src, src_num, src_fmt) != 0;
        }
    }
}