using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using UELib;
using UELib.Decoding;
using UELib.Core;
using UELib.Types;
using UELib.Engine;


namespace UEViewer
{
    public class PackageManager
    {
        private Dictionary<string, string> Paths;
        public Dictionary<string, List<string>> Files;
        private List<UnrealPackage> _CachedPackages = new List<UnrealPackage>();
        private string m_RootDirectory = "";

        private static PackageManager instance;
        public static PackageManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PackageManager();
                }
                return instance;
            }
        }
        private PackageManager() { }

        public void SetRoot(string root)
        {
            m_RootDirectory = root;
            Files = new Dictionary<string, List<string>>();
            Paths = new Dictionary<string, string>();
            Paths.Add("Animations", "ukx");
            Paths.Add("MAPS", "unr");
            Paths.Add("Sounds", "uax");
            Paths.Add("StaticMeshes", "usx");
            Paths.Add("SysTextures", "utx");
            Paths.Add("Textures", "utx");

            foreach (KeyValuePair<string, string> kvp in Paths)
            {
                if (Directory.Exists(Path.Combine(m_RootDirectory, kvp.Key)))
                {
                    string[] files = Directory.GetFiles(Path.Combine(m_RootDirectory, kvp.Key), "*." + kvp.Value, SearchOption.AllDirectories);
                    Files.Add(kvp.Key, files.ToList());
                }
            }
        }
        public PackageManager(string root)
        {
            Files = new Dictionary<string, List<string>>();
            Paths = new Dictionary<string, string>();
            Paths.Add("Animations", "ukx");
            Paths.Add("MAPS", "unr");
            Paths.Add("Sounds", "uax");
            Paths.Add("StaticMeshes", "usx");
            Paths.Add("SysTextures", "utx");
            Paths.Add("Textures", "utx");

            foreach (KeyValuePair<string, string> kvp in Paths)
            {
                if (Directory.Exists(Path.Combine(root, kvp.Key)))
                {
                    string[] files = Directory.GetFiles(Path.Combine(root, kvp.Key), "*." + kvp.Value, SearchOption.AllDirectories);
                    Files.Add(kvp.Key, files.ToList());
                }
            }
        }
        public UObject FindObject(string fullname)
        {
            string[] words = fullname.Split('.');
            UnrealPackage p = LoadPackage(words[0]);
            p.InitializePackage();
            words = words.RemoveAt(0);
            return p.FindObjectByGroup(String.Join(".", words));

        }
        public UObject FindObject(string fullname, Type type)
        {
            string[] words = fullname.Split('.');
            UnrealPackage p = LoadPackage(words[0]);
            p.InitializePackage();
            words = words.RemoveAt(0);
            return p.FindObject(String.Join(".", words), type);
        }
        public UnrealPackage LoadPackage(string name)
        {
            foreach (KeyValuePair<string, List<string>> kvp in Files)
            {
                foreach (string f in kvp.Value)
                {
                    if (Path.GetFileName(f).StartsWith(name))
                    {
                        var package = _CachedPackages.Find(pkg => pkg.PackageName == Path.GetFileNameWithoutExtension(f));
                        if (package != null)
                            return package;
                        if (IsLineage(f))
                        {
                            package = UnrealLoader.LoadPackage(f, new LineageDecoder(), FileAccess.Read);
                        }
                        else
                        {
                            package = UnrealLoader.LoadPackage(f, FileAccess.Read);
                        }
                        PreInit(package);
                        _CachedPackages.Add(package);
                        return package;
                    }
                }
            }
            return null;
        }
        List<string> VariableTypes = new System.Collections.Generic.List<string>
				{
					"Engine.Actor.Skins:ObjectProperty",
					"Engine.Actor.Components:ObjectProperty",
					"Engine.SkeletalMeshComponent.AnimSets:ObjectProperty",
					"Engine.SequenceOp.InputLinks:StructProperty",
					"Engine.SequenceOp.OutputLinks:StructProperty",
					"Engine.SequenceOp.VariableLinks:StructProperty",
					"Engine.SequenceAction.Targets:ObjectProperty",
					"XInterface.GUIComponent.Controls:ObjectProperty",
					"Engine.Material.Expressions:ObjectProperty",
					"Engine.ParticleSystem.Emitters:ObjectProperty",
                    "Engine.Materials:StructProperty"
				};
        private Tuple<string, string, PropertyType> ParseVariable(string data)
        {
            int num;
            while (true)
            {
                num = data.IndexOf(':');
                if (num != -1)
                {
                    break;
                }
                data += ":ObjectProperty";
            }
            string text = data.Left(num);
            checked
            {
                string item = text.Mid(text.LastIndexOf('.') + 1);
                PropertyType item2;
                try
                {
                    item2 = (PropertyType)System.Enum.Parse(typeof(PropertyType), data.Substring(num + 1));
                }
                catch (System.Exception)
                {
                    item2 = PropertyType.ObjectProperty;
                }
                return Tuple.Create<string, string, PropertyType>(item, text, item2);
            }
        }
        private void PreInit(UnrealPackage package)
        {
            UnrealConfig.VariableTypes = new System.Collections.Generic.Dictionary<string, Tuple<string, PropertyType>>();
            foreach (string current in VariableTypes)
            {
                Tuple<string, string, UELib.Types.PropertyType> tuple = ParseVariable(current);
                UnrealConfig.VariableTypes.Add(tuple.Item1, Tuple.Create<string, PropertyType>(tuple.Item2, tuple.Item3));
            }
            package.RegisterClass("StaticMesh", typeof(UStaticMesh));
            package.RegisterClass("Shader", typeof(UShader));
        }
        private bool IsLineage(string filename)
        {
            bool ret_result = false;
            using (BinaryReader b = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                byte[] m_Data = b.ReadBytes(22);
                string result = Encoding.Unicode.GetString(m_Data);
                if (result == "Lineage2Ver")
                {
                    ret_result = true;
                }
            }
            return ret_result;
        }
    }
}
