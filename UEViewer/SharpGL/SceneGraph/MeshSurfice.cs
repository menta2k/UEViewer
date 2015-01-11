using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;

using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Collections;
using SharpGL.SceneGraph.Core;
using SharpGL.SceneGraph.Primitives;
using System.Xml.Serialization;
using SharpGL.SceneGraph.Assets;
using UEViewer;
namespace SharpGL.SceneGraph
{
    public class MeshSurfice : IHasMaterial, IRenderable
    {
        public virtual void Render(OpenGL gl, RenderMode renderMode)
        {
            if (Material != null)
                Material.Push(gl);
            int k = 0;
            gl.Begin(OpenGL.GL_TRIANGLES);
            for (int j = 0; j < triangle_count; j++)
            {
                gl.Normal(normals[k].ToArray());
                gl.TexCoord(uvs[k].ToArray());
                gl.Vertex(vertices[k].ToArray());

                gl.Normal(normals[k + 1].ToArray());
                gl.TexCoord(uvs[k + 1].ToArray());
                gl.Vertex(vertices[k + 1].ToArray());

                gl.Normal(normals[k + 2].ToArray());
                gl.TexCoord(uvs[k + 2].ToArray());
                gl.Vertex(vertices[k + 2].ToArray());
                k += 3;
            }
            gl.End();
            if (Material != null)
                Material.Pop(gl);
        }

        private List<Vertex> vertices = new List<Vertex>();

        private List<UV> uvs = new List<UV>();
        private int triangle_count;

        public int TriangleCount
        {
            get { return triangle_count; }
            set { triangle_count = value; }
        }
        private List<Vertex> normals = new List<Vertex>();
        public Material Material
        {
            get;
            set;
        }

        [Description("The vertices that make up the MeshSurfice."), Category("MeshSurfice")]
        public List<Vertex> Vertices
        {
            get { return vertices; }
            set { vertices = value; }
        }

        [Description("The material coordinates."), Category("MeshSurfice")]
        public List<UV> UVs
        {
            get { return uvs; }
            set { uvs = value; }
        }

        [Description("The normals."), Category("Normals")]
        public List<Vertex> Normals
        {
            get { return normals; }
            set { normals = value; }
        }
    }
}
