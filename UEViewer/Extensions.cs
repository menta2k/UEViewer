using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UELib;
using UELib.Core;
using UELib.Engine;
using SharpGL.SceneGraph;

namespace UEViewer
{
    public static class Extensions
    {
        public static string Left(this string s, int index)
        {
            return s.Substring(0, index);
        }

        public static string Mid(this string s, int index)
        {
            return s.Substring(index, s.Length - index);
        }

        public static string Right(this string s, int index)
        {
            return s.Substring(0, s.Length - index);
        }
        public static T[] RemoveAt<T>(this T[] source, int index)
        {
            T[] dest = new T[source.Length - 1];
            if (index > 0)
                Array.Copy(source, 0, dest, 0, index);

            if (index < source.Length - 1)
                Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

            return dest;
        }
        public static float[] ToArray(this Vertex v)
        {
            return new float[] { v.X, v.Y, v.Z };
        }
        public static float[] ToArray(this SharpGL.SceneGraph.UV uv)
        {
            return new float[] { uv.U, uv.V };
        }
        public static SharpGL.SceneGraph.UV ToUV(this UELib.Engine.UV uv)
        {
            return new SharpGL.SceneGraph.UV(uv.U, uv.V);
        }
        public static Vertex ToVertex(this UVector v)
        {
            return new Vertex(v.X, v.Y, v.Z);
        }
        public static Type FindType(string qualifiedTypeName)
        {
            Type t = Type.GetType(qualifiedTypeName);

            if (t != null)
            {
                return t;
            }
            else
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = asm.GetType(qualifiedTypeName);
                    if (t != null)
                        return t;
                }
                return null;
            }
        }
        public static List<Material> GetMaterials(this UObject obj)
        {
            List<Material> materials = new List<Material>();
            if (obj.Properties != null)
            {
                var materials_prop = obj.Properties.Find("Materials");
                if (materials_prop != null)
                {
                    string text = materials_prop.Decompile();
                    Console.WriteLine(text);
                    string[] lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                    foreach (string line in lines)
                    {
                        Material m = new Material(line);
                        materials.Add(m);
                    }
                }
            }
            return materials;
        }
        public static Tuple<string, string, UELib.Types.PropertyType> ParseVariable(string data)
        {
        retry:
            var groupIndex = data.IndexOf(':');
            if (groupIndex == -1)
            {
                data += ":ObjectProperty";
                goto retry;
            }
            var varGroup = data.Left(groupIndex);
            var varName = varGroup.Mid(varGroup.LastIndexOf('.') + 1);
            UELib.Types.PropertyType varType;
            try
            {
                varType = (UELib.Types.PropertyType)Enum.Parse(typeof(UELib.Types.PropertyType), data.Substring(groupIndex + 1));
            }
            catch (Exception)
            {
                varType = UELib.Types.PropertyType.ObjectProperty;
            }
            return Tuple.Create(varName, varGroup, varType);
        }
    }
}
