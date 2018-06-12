using UnityEngine;
using System;

namespace UTJ.Alembic
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class ReadOnlyAttribute : PropertyAttribute
    {
    }
}
