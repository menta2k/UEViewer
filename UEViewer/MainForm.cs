using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using UELib;
using UELib.Engine;
using UELib.Core;
using SharpGL;
using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Core;
using SharpGL.SceneGraph.Cameras;
using SharpGL.SceneGraph.Collections;
using SharpGL.SceneGraph.Primitives;
using System.Threading;
namespace UEViewer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
#if DEBUG
            AllocConsole();
#endif
            Player = new SoundPlayer();
            InitializeComponent();
        }

        private void gameDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Manager = PackageManager.Instance;
#if !DEBUG
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK) {
               Manager.SetRoot(folderBrowserDialog1.SelectedPath);
            }
#endif
#if DEBUG
            Manager.SetRoot(@"D:\Games\LineageII EU");
#endif
            foreach (KeyValuePair<string, List<string>> kvp in Manager.Files)
            {
                TreeNode node = new TreeNode(kvp.Key);
                foreach (string f in kvp.Value)
                {
                    node.Nodes.Add(Path.GetFileName(f));
                }
                this.treeView1.Nodes.Add(node);
            }
        }
        private PackageManager Manager;
        private SoundPlayer Player;
        private SharpGL.OpenGL gl;
        private UnrealPackage CurrentPackage;
        private UObject selectedObject;
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int AllocConsole();

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Nodes.Count == 0)
            {
                 CurrentPackage = Manager.LoadPackage(e.Node.Text);
                 CurrentPackage.RegisterClass("StaticMesh", typeof(UStaticMesh));
                 CurrentPackage.RegisterClass("Shader", typeof(UShader));
                 CurrentPackage.InitializePackage();
                 dataGridView1.Rows.Clear();
                 foreach (UObject obj in CurrentPackage.Objects)
                 {
                     if (obj.ExportTable != null)
                     {
                         // add only supported objects
                         if (obj.IsClassType("Texture") || obj.IsClassType("Texture2D")||  obj.IsClassType("Sound") || obj.IsClassType("SoundCue") || obj.IsClassType("StaticMesh"))
                         {
                             this.dataGridView1.Rows.Add(obj.Name);
                         }
                     }
                 }
            }
        }

        private void openGLControl1_OpenGLInitialized(object sender, EventArgs e)
        {
            gl = this.openGLControl1.OpenGL;
            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.ShadeModel(OpenGL.GL_SMOOTH);
            gl.Hint(OpenGL.GL_PERSPECTIVE_CORRECTION_HINT, OpenGL.GL_NICEST);
            gl.Enable(OpenGL.GL_TEXTURE_2D);
        }
        private void FindObject(DataGridView grid)
        {
            int Row = grid.CurrentRow.Index;
            object X1 = grid[0, Row].Value;
            if (X1 == null)
            {
                return;
            }
            foreach (UObject obj in CurrentPackage.Objects)
            {
                if (obj.Name == X1.ToString())
                {
                    selectedObject = obj;
                }
            }
        }
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            FindObject(sender as DataGridView);
            if (selectedObject != null)
            {
                selectedObject.Decompile();
                if (selectedObject.IsClassType("Texture"))
                {
                   SharpGL.SceneGraph.Assets.Texture cube_texture = new SharpGL.SceneGraph.Assets.Texture();
                   Bitmap texture = LibSquish.GetImage((UTexture)selectedObject);
                   cube_texture.Create(gl, texture);
                   SharpGL.SceneGraph.Assets.Material material = new SharpGL.SceneGraph.Assets.Material();
                   material.Texture = cube_texture;
                   cube = new Cube();
                   cube.Material = material;
                   BoundingVolume boundingVolume = cube.BoundingVolume;
                   float[] extent = new float[3];
                   cube.BoundingVolume.GetBoundDimensions(out extent[0], out extent[1], out extent[2]);
                   float maxExtent = extent.Max();
                   float scaleFactor = maxExtent > 10 ? 10.0f / maxExtent : 1;
                   cube.Transformation.ScaleX = scaleFactor;
                   cube.Transformation.ScaleY = scaleFactor;
                   cube.Transformation.ScaleZ = scaleFactor;
                }
                if (selectedObject.IsClassType("Sound"))
                {
                    PlaySound();
                }
                if (selectedObject.IsClassType("StaticMesh"))
                {
                    smi = new StaticMeshInstance();
                    smi.Init((UStaticMesh)selectedObject, gl);
                    BoundingVolume boundingVolume = smi.BoundingVolume;
                    float[] extent = new float[3];
                    smi.BoundingVolume.GetBoundDimensions(out extent[0], out extent[1], out extent[2]);
                    float maxExtent = extent.Max();
                    //  Scale so that we are at most 10 units in size.
                    float scaleFactor = maxExtent > 10 ? 10.0f / maxExtent : 1;
                    smi.Transformation.ScaleX = scaleFactor;
                    smi.Transformation.ScaleY = scaleFactor;
                    smi.Transformation.ScaleZ = scaleFactor;
                    smi.Transformation.RotateX = -90;
                }
            }

        }
        uint[] vao = new uint[1];
        uint[] vbo = new uint[1];
        private uint fragmentShader;
        private uint vertexShader;
        private uint shaderProgram;
        private int posAttrib;
        private int timeLoc;
        private int sampleLoc;
        private int waveLoc;
        private void PlaySound()
        {
            try
            {
                gl.GenVertexArrays(1, vao);
                gl.BindVertexArray(vao[0]);


                gl.GenBuffers(1, vbo);

                float[] vertices = new float[]{
                    -1.0f,  1.0f,
                    1.0f, 1.0f,
                    1.0f, -1.0f,
                    -1.0f,1.0f,
                    -1.0f,-1.0f,
                    1.0f,-1.0f
            };
                gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, vbo[0]);
                unsafe
                {
                    fixed (float* verts = vertices)
                    {
                        var ptr = new IntPtr(verts);
                        // fill the buffer to the currently binded buffer
                        gl.BufferData(OpenGL.GL_ARRAY_BUFFER, vertices.Length * sizeof(float), ptr, OpenGL.GL_STATIC_DRAW);
                    }
                }
                //  Create the shader program.
                var vertexShaderSource = ManifestResourceLoader.LoadTextFile("SoundShader.vert");
                var fragmentShaderSource = ManifestResourceLoader.LoadTextFile("SoundShader.frag");
                vertexShader = gl.CreateShader(OpenGL.GL_VERTEX_SHADER);
                gl.ShaderSource(vertexShader, vertexShaderSource);
                gl.CompileShader(vertexShader);

                // Create and compile the fragment shader
                fragmentShader = gl.CreateShader(OpenGL.GL_FRAGMENT_SHADER);
                gl.ShaderSource(fragmentShader, fragmentShaderSource);
                gl.CompileShader(fragmentShader);

                // Link the vertex and fragment shader into a shader program
                shaderProgram = gl.CreateProgram();
                gl.AttachShader(shaderProgram, vertexShader);
                gl.AttachShader(shaderProgram, fragmentShader);

                gl.BindFragDataLocation(shaderProgram, (uint)0, "gl_FragColor");

                gl.LinkProgram(shaderProgram);
                int[] infoLength = new int[] { 0 };
                int bufSize = infoLength[0];
                //  Get the compile info.
                StringBuilder il = new StringBuilder(bufSize);
                gl.GetProgramInfoLog(shaderProgram, bufSize, IntPtr.Zero, il);
                gl.UseProgram(shaderProgram);

                posAttrib = gl.GetAttribLocation(shaderProgram, "position");
                gl.EnableVertexAttribArray((uint)posAttrib);
                gl.VertexAttribPointer((uint)posAttrib, 2, OpenGL.GL_FLOAT, false, 0, new IntPtr(0));


                timeLoc = gl.GetUniformLocation(shaderProgram, "iGlobalTime");
                sampleLoc = gl.GetUniformLocation(shaderProgram, "Spectrum");
                waveLoc = gl.GetUniformLocation(shaderProgram, "Wavedata");
                int resLoc = gl.GetUniformLocation(shaderProgram, "iResolution");


                gl.Uniform3(resLoc, (float)this.openGLControl1.Width, (float)this.openGLControl1.Height, (float)(this.openGLControl1.Width * this.openGLControl1.Height));
                Thread thread = new Thread(() => PlaySoundT(((USound)selectedObject).SoundBuffer));
                thread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace.ToString());
            }
        }
        public void PlaySoundT(byte[] data)
        {
            Player.Play(data, data.Length);
        }
        private void DrawSound()
        {
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            float[] spectrum = new float[256];
            float[] wavedata = new float[512];
            Player.getWaveData(ref wavedata, 256, 0);
            Player.getSpectrum(ref spectrum, 256, 0);

            DateTime currentDate = DateTime.Now;
            float time = (float)currentDate.Ticks / (float)TimeSpan.TicksPerSecond;

            gl.Uniform1(timeLoc, time);
            gl.Uniform1(sampleLoc, 256, spectrum);

            gl.Uniform1(waveLoc, 256, wavedata);
            gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT);

            gl.DrawArrays(OpenGL.GL_TRIANGLES, 0, 6);
        }
        private float rotate = 0;
        private void SetupLigtening()
        {
            // enable lighting
            float[] lightPos = new float[4] { 1000, 2000, 2000, 0 };
            float[] lightAmbient = new float[4] { 0.1f, 0.1f, 0.15f, 1 };
            float[] specIntens = new float[4] { 0.4f, 0.4f, 0.4f, 0 };
            float[] black = new float[4] { 0, 0, 0, 0 };
            float[] white = new float[4] { 1, 1, 1, 0 };
            gl.Enable(OpenGL.GL_COLOR_MATERIAL);
            gl.Enable(OpenGL.GL_LIGHT0);
            gl.Enable(OpenGL.GL_NORMALIZE);	
            // light parameters
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_POSITION, lightPos);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_DIFFUSE, white);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_AMBIENT, lightAmbient);
            gl.Light(OpenGL.GL_LIGHT0, OpenGL.GL_SPECULAR, white);
            // material parameters
            gl.Material(OpenGL.GL_FRONT, OpenGL.GL_DIFFUSE, white);
            gl.Material(OpenGL.GL_FRONT, OpenGL.GL_AMBIENT, white);
            gl.Material(OpenGL.GL_FRONT, OpenGL.GL_SPECULAR, specIntens);
            gl.Material(OpenGL.GL_FRONT, OpenGL.GL_SHININESS, 5);
        }
        private void DrawStaticMesh() {
            if (smi != null)
            {
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                gl.LoadIdentity();

                //  View from a bit away the y axis and a few units above the ground.
                gl.LookAt(-10, -5, 10, 0, 0, 0, 0, 1, 0);

                //  Rotate the objects every cycle.
                gl.Rotate(rotate, 0.0f, 1.0f, 0.0f);

                //  Move the objects down a bit so that they fit in the screen better.
                //gl.Translate(0, 0, -1);
                SetupLigtening();
                smi.PushObjectSpace(gl);
                smi.Render(gl, RenderMode.Render);
                smi.PopObjectSpace(gl);
                rotate += 1.0f;
            }
        }
        private void DrawTexture() {
            if (cube != null)
            {
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                gl.LoadIdentity();

                //  View from a bit away the y axis and a few units above the ground.
                gl.LookAt(-10, -5, 10, 0, 0, 0, 0, 1, 0);

                //  Rotate the objects every cycle.
                gl.Rotate(rotate, 0.0f, 1.0f, 0.0f);

                //  Move the objects down a bit so that they fit in the screen better.
                //gl.Translate(0, 0, -1);
                SetupLigtening();
                cube.PushObjectSpace(gl);
                cube.Render(gl, RenderMode.Render);
                cube.PopObjectSpace(gl);
                rotate += 1.0f;
            }
        }
        private StaticMeshInstance smi;
        private Cube cube;
        private void openGLControl1_OpenGLDraw(object sender, RenderEventArgs args)
        {
            if (selectedObject == null){
                return;
            }
            switch (selectedObject.GetClassName()) { 
                case "Texture2D":
                case "Texture":
                    DrawTexture();
                    break;
                case "Sound":
                    DrawSound();
                    break;
                case "StaticMesh":
                    DrawStaticMesh();
                    break;
                default:
                    break;
            }
        }
    }
}
