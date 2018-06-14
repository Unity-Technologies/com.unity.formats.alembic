using UnityEngine;
using System;

namespace UnityEngine.Formats.Alembic.Importer
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class ReadOnlyAttribute : PropertyAttribute
    {
    }
}
