using System;
using System.Collections.Generic;
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
            if (DisableUndoGuard.enableUndo)
            {
                Undo.RegisterCreatedObjectUndo(ret, message);
            }
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
            if (DisableUndoGuard.enableUndo)
            {
                Undo.RegisterCreatedObjectUndo(ret, "Add Component");
            }
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

#if UNITY_EDITOR
        internal class DisableUndoGuard : IDisposable
        {
            internal static bool enableUndo = true;
            static readonly Stack<bool> m_UndoStateStack = new Stack<bool>();
            bool m_Disposed;
            public DisableUndoGuard(bool disable)
            {
                m_Disposed = false;
                m_UndoStateStack.Push(enableUndo);
                enableUndo = !disable;
            }

            public void Dispose()
            {
                if (!m_Disposed)
                {
                    if (m_UndoStateStack.Count == 0)
                    {
                        Debug.LogError("UnMatched DisableUndoGuard calls");
                        enableUndo = true;
                        return;
                    }
                    enableUndo = m_UndoStateStack.Pop();
                    m_Disposed = true;
                }
            }
        }
#endif

        public static void DepthFirstVisitor(this GameObject root, Action<GameObject> lambda)
        {
            for (var i = 0; i < root.transform.childCount; i++)
            {
                DepthFirstVisitor(root.transform.GetChild(i).gameObject, lambda);
            }
            lambda.Invoke(root);
        }
    }
}
