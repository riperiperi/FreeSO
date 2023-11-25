using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Utils
{
    /// <summary>
    /// Represents a vertex making up a 2D sprite in the game.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ParticleVertex : IVertexType
    {
        public Vector3 Position;
        public Vector3 ModelPosition;
        public Vector3 TextureCoordinate;

        /// <summary>
        /// Creates a new ParticleVertex instance.
        /// </summary>
        /// <param name="position">Position of particle.</param>
        /// <param name="modelPosition">Position of this vertex within the particle.</param>
        /// <param name="textureCoords">Texture coordinate for this vertex.</param>
        public ParticleVertex(Vector3 position, Vector3 modelPosition, Vector3 textureCoords)
        {
            this.Position = position;
            this.ModelPosition = modelPosition;
            this.TextureCoordinate = textureCoords;
        }

        public static int SizeInBytes = sizeof(float) * 9;

        public static VertexDeclaration VertexElements = new VertexDeclaration
        (
             new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
             new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0),
             new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1)
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexElements; }
        }
    }
}
