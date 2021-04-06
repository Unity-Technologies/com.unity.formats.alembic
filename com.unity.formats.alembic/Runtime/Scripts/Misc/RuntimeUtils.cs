using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Formats.Alembic.Importer
{
    static class RuntimeUtils
    {
        public static void DisposeIfPossible<T>(this ref NativeArray<T> array) where T : struct
        {
            if (array.IsCreated)
            {
                array.Dispose();
            }
        }

        public static NativeArray<T> ResizeIfNeeded<T>(this ref NativeArray<T> array, int newLength, Allocator a = Allocator.Persistent) where T : struct
        {
            if (array.Length != newLength)
            {
                array.DisposeIfPossible();
                array = new NativeArray<T>(newLength, a);
            }

            // The array is either created and of the right size, or not created and we are asking to resize to 0
            if (!array.IsCreated)
            {
                array = new NativeArray<T>(0, a);
            }

            return array;
        }

        public static unsafe void* GetPointer<T>(this NativeArray<T> array) where T : struct
        {
            return array.Length == 0 ? null : array.GetUnsafePtr();
        }

        public static ulong CombineHash(this ulong h1, ulong h2)
        {
            unchecked
            {
                return h1 ^ h2 + 0x9e3779b9 + (h1 << 6) + (h1 >> 2); // Similar to c++ boost::hash_combine
            }
        }

        public static GameObject CreateGameObjectWithUndo(string message)
        {
            var ret = new GameObject();
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(ret, message);
#endif
            return ret;
        }

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var ret = go.GetComponent<T>();
            if (ret != null)
            {
                return ret;
            }

            ret = go.AddComponent<T>();
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(ret, "Add Component");
#endif
            return ret;
        }

        public static void DestroyUnityObject(Object o)
        {
#if UNITY_EDITOR
            Object.DestroyImmediate(o, true);
#else
            Object.Destroy(o);
#endif
        }
    }
}
