using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DumpSelectedGameObjectFull
{
    private const int MaxDepth = 4;
    private const int MaxEnumerableItems = 50;

    [MenuItem("Tools/Debug/Dump Selected GameObject Full To TXT")]
    public static void Dump()
    {
        GameObject go = Selection.activeGameObject;

        if (go == null)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        var sb = new StringBuilder();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);

        DumpGameObject(go, sb, 0, visited);

        string path = Path.Combine(Application.dataPath, "../SelectedGameObjectDump_Full.txt");
        File.WriteAllText(path, sb.ToString());

        Debug.Log($"Dumped selected GameObject to: {path}");
        EditorUtility.RevealInFinder(path);
    }

    private static void DumpGameObject(GameObject go, StringBuilder sb, int indent, HashSet<object> visited)
    {
        string pad = new string(' ', indent * 2);

        sb.AppendLine($"{pad}GameObject: {go.name}");
        sb.AppendLine($"{pad}ActiveSelf: {go.activeSelf}");
        sb.AppendLine($"{pad}ActiveInHierarchy: {go.activeInHierarchy}");
        sb.AppendLine($"{pad}Layer: {go.layer}");
        sb.AppendLine($"{pad}Tag: {go.tag}");
        sb.AppendLine($"{pad}Scene: {go.scene.name}");

        sb.AppendLine($"{pad}Transform:");
        sb.AppendLine($"{pad}  Position: {go.transform.position}");
        sb.AppendLine($"{pad}  LocalPosition: {go.transform.localPosition}");
        sb.AppendLine($"{pad}  Rotation: {go.transform.rotation.eulerAngles}");
        sb.AppendLine($"{pad}  LocalRotation: {go.transform.localRotation.eulerAngles}");
        sb.AppendLine($"{pad}  Scale: {go.transform.localScale}");

        sb.AppendLine($"{pad}Components:");

        foreach (Component component in go.GetComponents<Component>())
        {
            if (component == null)
            {
                sb.AppendLine($"{pad}  Missing Script");
                continue;
            }

            sb.AppendLine($"{pad}  Component: {component.GetType().FullName}");

            try
            {
                sb.AppendLine($"{pad}  Serialized Unity JSON:");
                sb.AppendLine(EditorJsonUtility.ToJson(component, true));
            }
            catch (Exception e)
            {
                sb.AppendLine($"{pad}  Serialized Unity JSON failed: {e.Message}");
            }

            sb.AppendLine($"{pad}  Runtime reflection dump:");
            DumpObject(component, sb, indent + 2, visited, 0);
            sb.AppendLine();
        }

        foreach (Transform child in go.transform)
            DumpGameObject(child.gameObject, sb, indent + 1, visited);
    }

    private static void DumpObject(object obj, StringBuilder sb, int indent, HashSet<object> visited, int depth)
    {
        string pad = new string(' ', indent * 2);

        if (obj == null)
        {
            sb.AppendLine($"{pad}null");
            return;
        }

        Type type = obj.GetType();

        if (IsSimple(type))
        {
            sb.AppendLine($"{pad}{FormatSimple(obj)}");
            return;
        }

        if (obj is UnityEngine.Object unityObject)
        {
            sb.AppendLine($"{pad}{unityObject.name} ({unityObject.GetType().FullName})");
        }

        if (depth >= MaxDepth)
        {
            sb.AppendLine($"{pad}<max depth reached>");
            return;
        }

        if (!type.IsValueType)
        {
            if (!visited.Add(obj))
            {
                sb.AppendLine($"{pad}<already visited>");
                return;
            }
        }

        if (obj is IEnumerable enumerable && obj is not string)
        {
            int count = 0;
            foreach (object item in enumerable)
            {
                if (count >= MaxEnumerableItems)
                {
                    sb.AppendLine($"{pad}<more items omitted>");
                    break;
                }

                sb.AppendLine($"{pad}[{count}]");
                DumpObject(item, sb, indent + 1, visited, depth + 1);
                count++;
            }

            return;
        }

        Type current = type;

        while (current != null && current != typeof(object))
        {
            sb.AppendLine($"{pad}Fields from {current.FullName}:");

            FieldInfo[] fields = current.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            foreach (FieldInfo field in fields)
            {
                if (field.IsStatic)
                    continue;

                try
                {
                    object value = field.GetValue(obj);

                    sb.AppendLine($"{pad}  {field.FieldType.Name} {field.Name}:");

                    if (value == null || IsSimple(field.FieldType) || value is UnityEngine.Object)
                    {
                        sb.AppendLine($"{pad}    {FormatValue(value)}");
                    }
                    else
                    {
                        DumpObject(value, sb, indent + 2, visited, depth + 1);
                    }
                }
                catch (Exception e)
                {
                    sb.AppendLine($"{pad}  {field.Name}: <error: {e.Message}>");
                }
            }

            current = current.BaseType;
        }
    }

    private static string FormatValue(object value)
    {
        if (value == null)
            return "null";

        if (value is UnityEngine.Object unityObject)
            return $"{unityObject.name} ({unityObject.GetType().FullName})";

        return FormatSimple(value);
    }

    private static string FormatSimple(object value)
    {
        if (value == null)
            return "null";

        return value.ToString();
    }

    private static bool IsSimple(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(Vector2) ||
               type == typeof(Vector3) ||
               type == typeof(Vector4) ||
               type == typeof(Quaternion) ||
               type == typeof(Color) ||
               type == typeof(Rect) ||
               type == typeof(Bounds);
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}