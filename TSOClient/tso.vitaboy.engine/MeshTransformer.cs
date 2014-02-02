using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace tso.vitaboy
{
    public class MeshTransformer
    {
        /// <summary>
        /// Transforms the verticies making up this mesh into
        /// the designated bone positions.
        /// </summary>
        /// <param name="bone">The bone to start with. Should always be the ROOT bone.</param>
        public static void Transform(Mesh mesh, Bone bone)
        {
            var binding = mesh.BoneBindings.FirstOrDefault(x => x.BoneName == bone.Name);
            if (binding != null)
            {
                for (var i = 0; i < binding.RealVertexCount; i++){
                    var vertexIndex = binding.FirstRealVertex + i;
                    var blendVertexIndex = vertexIndex;//binding.FirstBlendVertex + i;

                    var realVertex = mesh.RealVertexBuffer[vertexIndex];
                    var matrix = Matrix.CreateTranslation(realVertex.Position) * bone.AbsoluteMatrix;

                    //Position
                    var newPosition = Vector3.Transform(Vector3.Zero, matrix);
                    mesh.BlendVertexBuffer[blendVertexIndex].Position = newPosition;

                    //Normals
                    matrix = Matrix.CreateTranslation(
                        new Vector3(realVertex.Normal.X,
                                    realVertex.Normal.Y,
                                    realVertex.Normal.Z)) * bone.AbsoluteMatrix;

                    //mesh.BlendVertexBuffer[vertexIndex].Normal = Vector3.Transform(Vector3.Zero, matrix);
                }
            }

            foreach (var child in bone.Children)
            {
                Transform(mesh, child);
            }

            if (bone.Name == "ROOT")
            {
                mesh.InvalidateMesh();
            }
        }

    }
}
