#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Formats.Alembic
{
    static partial class IconUtility
    {
        internal static Dictionary<string, Texture2D> cachedIcons => s_CachedIcons;
    }
}
#endif // UNITY_INCLUDE_TESTS
