using Unity.Collections;

namespace UnityEngine.Formats.Alembic.Importer
{
    static class Utils
    {
        public static void DisposeIfPossible<T>(this NativeArray<T> array) where T : struct
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
    }
}
