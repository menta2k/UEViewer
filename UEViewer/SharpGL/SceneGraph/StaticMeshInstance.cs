using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using SharpGL.SceneGraph.Collections;
using SharpGL.SceneGraph.Core;
using SharpGL.SceneGraph.Lighting;
using SharpGL.SceneGraph.Raytracing;
using SharpGL.SceneGraph.Helpers;
using System.Xml.Serialization;
using SharpGL.SceneGraph.Transformations;
using SharpGL.SceneGraph.Assets;
using UELib.Engine;
using UEViewer;
namespace SharpGL.SceneGraph.Primitives
{
    [Serializable]
    public class StaticMeshInstance : SceneElement,
        IHasObjectSpace,
        IRenderable,
        IRayTracable,
        IVolumeBound,
        IDeepCloneable<StaticMeshInstance>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StaticMeshInstance"/> class.
        /// </summary>
        public StaticMeshInstance()
        {
            Name = "StaticMeshInstance";
        }
        public void Init(UStaticMesh mesh, OpenGL gl)
        {
            int i = 0;
            var materials = mesh.GetMaterials();
            foreach (SMeshFace face in mesh.Faces)
            {
                MeshSurfice s = new MeshSurfice();
                int end_offset = face.index_offset + (face.triangle_count * 3);
                for (int j = face.index_offset; j < end_offset; j += 3)
                {
                    s.Vertices.Add(mesh.Verteces[mesh.vertex_indicies_1[j]].Location.ToVertex());
                    s.Vertices.Add(mesh.Verteces[mesh.vertex_indicies_1[j + 1]].Location.ToVertex());
                    s.Vertices.Add(mesh.Verteces[mesh.vertex_indicies_1[j + 2]].Location.ToVertex());
                    s.UVs.Add(mesh.texture_coords[0].elements[mesh.vertex_indicies_1[j]].UV.ToUV());
                    s.UVs.Add(mesh.texture_coords[0].elements[mesh.vertex_indicies_1[j + 1]].UV.ToUV());
                    s.UVs.Add(mesh.texture_coords[0].elements[mesh.vertex_indicies_1[j + 2]].UV.ToUV());
                    s.Normals.Add(mesh.Verteces[mesh.vertex_indicies_1[j]].Normal.ToVertex());
                    s.Normals.Add(mesh.Verteces[mesh.vertex_indicies_1[j + 1]].Normal.ToVertex());
                    s.Normals.Add(mesh.Verteces[mesh.vertex_indicies_1[j + 2]].Normal.ToVertex());
                }
                SceneGraph.Assets.Material mat = new SharpGL.SceneGraph.Assets.Material();
                Texture texture = new Texture();
                System.Drawing.Bitmap image = LibSquish.GetImage(materials[i].Texture);
                texture.Create(gl, image);
                mat.Texture = texture;
                s.Material = mat;
                s.TriangleCount = face.triangle_count;
                Surfices.Add(s);
                i++;
            }
            vertices = Surfices.SelectMany(s => s.Vertices).ToList();
            Console.WriteLine();
        }
        private List<MeshSurfice> Surfices = new List<MeshSurfice>();
        public virtual void Render(OpenGL gl, RenderMode renderMode)
        {
            foreach (MeshSurfice s in Surfices)
            {
                s.Render(gl, renderMode);
            }
        }
        /// <summary>
        /// The IHasObjectSpace helper.
        /// </summary>
        private HasObjectSpaceHelper hasObjectSpaceHelper = new HasObjectSpaceHelper();
        private BoundingVolumeHelper boundingVolumeHelper = new BoundingVolumeHelper();
        /// <summary>
        /// Gets the bounding volume.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public BoundingVolume BoundingVolume
        {
            get
            {
                //  todo; only create bv when vertices changed.
                boundingVolumeHelper.BoundingVolume.FromVertices(vertices);
                boundingVolumeHelper.BoundingVolume.Pad(0.1f);
                return boundingVolumeHelper.BoundingVolume;
            }
        }
        /// <summary>
        /// The vertices that make up the polygon.
        /// </summary>
        private List<Vertex> vertices = new List<Vertex>();

        /// <summary>
        /// Gets or sets the vertices.
        /// </summary>
        /// <value>
        /// The vertices.
        /// </value>
        [Description("The vertices that make up the polygon."), Category("Polygon")]
        public List<Vertex> Vertices
        {
            get { return vertices; }
            set { vertices = value; }
        }
        private Intersection TestIntersection(Ray ray)
        {
            Intersection intersect = new Intersection();
            return intersect;
        }
        public Intersection Raytrace(Ray ray, Scene scene)
        {
            //	First we see if this ray intersects this polygon.
            Intersection intersect = TestIntersection(ray);

            //	If there wasn't an intersection, return.
            if (intersect.intersected == false)
                return intersect;

            //	There was an intersection, find the color of this point on the 
            //	polygon.
            var lights = from se in scene.SceneContainer.Traverse()
                         where se is Light
                         select se;
            foreach (Light light in lights)
            {
                if (light.On)
                {
                    //	Can we see this light? Cast a shadow ray.
                    Ray shadowRay = new Ray();
                    bool shadow = false;
                    shadowRay.origin = intersect.point;
                    shadowRay.direction = light.Position - shadowRay.origin;

                    //	Test it with every polygon.
                    foreach (StaticMeshInstance p in scene.SceneContainer.Traverse<StaticMeshInstance>())
                    {
                        if (p == this) continue;
                        Intersection shadowIntersect = p.TestIntersection(shadowRay);
                        if (shadowIntersect.intersected)
                        {
                            shadow = true;
                            break;
                        }
                    }

                    if (shadow == false)
                    {
                        //	Now find out what this light complements to our color.
                        //todofloat angle = light.Direction.ScalarProduct(intersect.normal);
                        //todo ray.light += material.CalculateLighting(light, angle);
                        ray.light.Clamp();
                    }
                }
            }

            return intersect;
        }
        /// <summary>
        /// Pushes us into Object Space using the transformation into the specified OpenGL instance.
        /// </summary>
        /// <param name="gl">The OpenGL instance.</param>
        public void PushObjectSpace(OpenGL gl)
        {
            //  Use the helper to push us into object space.
            hasObjectSpaceHelper.PushObjectSpace(gl);
        }

        /// <summary>
        /// Pops us from Object Space using the transformation into the specified OpenGL instance.
        /// </summary>
        /// <param name="gl">The gl.</param>
        public void PopObjectSpace(OpenGL gl)
        {
            //  Use the helper to pop us from object space.
            hasObjectSpaceHelper.PopObjectSpace(gl);
        }
        public StaticMeshInstance DeepClone()
        {
            //  Create a new polygon.
            StaticMeshInstance polygon = new StaticMeshInstance();

            //  Clone the data.
            polygon.hasObjectSpaceHelper = hasObjectSpaceHelper.DeepClone();

            //  TODO clone lists.
            return polygon;
        }
        /// <summary>
        /// Gets the transformation that pushes us into object space.
        /// </summary>
        [Description("The Polygon Object Space Transformation"), Category("Polygon")]
        public LinearTransformation Transformation
        {
            get { return hasObjectSpaceHelper.Transformation; }
            set { hasObjectSpaceHelper.Transformation = value; }
        }

    }
}
