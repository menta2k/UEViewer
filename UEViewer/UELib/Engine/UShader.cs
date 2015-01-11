using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UELib.Core;
using UELib;
using System.IO;
using UEViewer;
using System.Reflection;

namespace UELib.Engine
{
    [UnrealRegisterClass]
    public class UShader : URenderedMaterial, IUnrealViewable, IUnrealExportable
    {
        private const string WAVExtension = "frag";
        public UShader()
        {
            ShouldDeserializeOnDemand = true;
        }

        public UObject Diffuse { get; set; }
        public UObject Opacity { get; set; }
        public UObject Specular { get; set; }
        public UObject SpecularityMask { get; set; }
        public UObject SelfIllumination { get; set; }
        public UObject SelfIlluminationMask { get; set; }
        public UObject Detail { get; set; }
        public float DetailScale { get; set; }
        public bool TwoSided { get; set; }
        public bool Wireframe { get; set; }
        public bool ModulateStaticLighting2X { get; set; }
        public bool PerformLightingOnSpecularPass { get; set; }
        public bool ModulateSpecular2X { get; set; }
        public bool TreatAsTwoSided { get; set; }
        public bool ZWrite { get; set; }
        public bool AlphaTest { get; set; }
        public byte AlphaRef { get; set; }
        public int OutputBlending { get; set; }
        public IEnumerable<string> ExportableExtensions
        {
            get { return new[] { WAVExtension }; }
        }
        public void SerializeExport(string desiredExportExtension, System.IO.Stream exportStream)
        {
        }
        public bool CompatableExport()
        {
            return true;
        }
        protected override void Deserialize()
        {
            base.Deserialize();
            Console.WriteLine("==================================");
            Type type = this.GetType();
            foreach (UDefaultProperty p in Properties)
            {
                string line = p.Decompile();
                var kv = line.Split('=');
                PropertyInfo propertyInfo = type.GetProperty(kv[0]);
                object propertyVal = kv[1].Trim();
                Type propertyType = propertyInfo.PropertyType;
                if (propertyType != typeof(UObject))
                {
                    var targetType = IsNullableType(propertyInfo.PropertyType) ? Nullable.GetUnderlyingType(propertyInfo.PropertyType) : propertyInfo.PropertyType;
                    propertyVal = Convert.ChangeType(propertyVal, targetType);
                    propertyInfo.SetValue(this, propertyVal, null);
                }
                else
                {
                    string temp_str;
                    temp_str = kv[1].Mid(kv[1].IndexOf("'"));
                    temp_str = temp_str.Substring(1, temp_str.Length - 2);
                    string u_type = kv[1].Left(kv[1].IndexOf("'"));
                    u_type = "UELib.Engine.U" + u_type;
                    UObject obj = this.Package.FindObject(temp_str, Extensions.FindType(u_type));
                    if (obj != null)
                    {
                        obj.Decompile();
                    }
                    propertyInfo.SetValue(this, obj, null);
                    Console.WriteLine();
                }
            }
            Console.WriteLine("==================================");
        }
        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }
    }
}
