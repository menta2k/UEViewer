using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UEViewer;
using UELib.Core;
namespace UELib.Engine
{
    public class Material
    {
        public bool bNoDynamicShadowCast { get; set; }
        public bool EnableCollisionforShadow { get; set; }
        public bool EnableCollision { get; set; }
        public UTexture Texture
        {
            get
            {
                if (Shader != null)
                {
                    return (UTexture)Shader.Diffuse;
                }
                return UTexture;
            }
        }
        public UTexture UTexture;
        public UShader Shader;
        private string m_Material;
        public Material(string txt)
        {
            var g = txt.Split(new char[] { '=' }, 2);
            string str = g[1].Substring(1, g[1].Length - 2);
            var line = str.Split(',');
            Type type = this.GetType();
            foreach (string item in line)
            {
                var kv = item.Split('=');
                PropertyInfo propertyInfo = type.GetProperty(kv[0]);
                if (propertyInfo == null)
                {
                    if (kv[0] == "Material")
                    {
                        m_Material = kv[1].Mid(kv[1].IndexOf("'"));
                        m_Material = m_Material.Substring(1, m_Material.Length - 2);
                    }
                }
                else
                {
                    object propertyVal = kv[1].Trim();
                    Type propertyType = propertyInfo.PropertyType;
                    var targetType = IsNullableType(propertyInfo.PropertyType) ? Nullable.GetUnderlyingType(propertyInfo.PropertyType) : propertyInfo.PropertyType;
                    propertyVal = Convert.ChangeType(propertyVal, targetType);
                    propertyInfo.SetValue(this, propertyVal, null);
                }
            }
            if (m_Material != "")
            {
                UObject obj = PackageManager.Instance.FindObject(m_Material);
                if (obj.IsClassType("Shader"))
                {
                    Shader = (UShader)obj;
                    Shader.BeginDeserializing();
                    foreach (UDefaultProperty p in obj.Properties)
                    {
                        Console.WriteLine(p.Decompile());
                    }
                    Console.WriteLine();
                }
                else if (obj.IsClassType("Texture"))
                {
                    UTexture = (UTexture)obj;
                    UTexture.Decompile();
                }
            }
        }
        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }
        public void Init()
        {

        }
        //Materials(0)=(bNoDynamicShadowCast=false,EnableCollisionforShadow=false,EnableCollision=true,Material=Texture'Aden_Colosseum_T.colo_add_wall009')
    }
}
