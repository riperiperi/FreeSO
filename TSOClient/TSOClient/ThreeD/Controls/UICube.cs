using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Utils;
using TSOClient.Code;
using TSOClient.Code.UI.Model;
using tso.common.rendering.framework.model;
using tso.common.utils;
using tso.common.rendering.framework;

namespace TSOClient.ThreeD.Controls
{
    /// <summary>
    /// Object to test the Scene engine
    /// </summary>
    public class UICube : _3DComponent
    {
        private Texture2D m_Texture;
        private Color m_Color;

        public Color Color
        {
            get { return m_Color; }
            set
            {
                m_Color = value;
                m_Texture = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, value);
            }
        }



        const int NUM_TRIANGLES = 12;
        const int NUM_VERTICES = 36;

        // Array of vertex information - contains position, normal and texture data
        private VertexPositionNormalTexture[] _vertices;

        // The vertex buffer where we load the vertices before drawing the shape
        private VertexBuffer _shapeBuffer;

        // Lets us check if the data has been constructed or not to improve performance
        private bool _isConstructed = false;



        private Vector3 m_Size;
        public Vector3 Size { get { return m_Size; } set { m_Size = value; _isConstructed = false; } }
        private Vector3 m_Position;
        public Vector3 Position { get { return m_Position; } set { m_Position = value; _isConstructed = false; } }



        private void ConstructCube()
        {
            _vertices = new VertexPositionNormalTexture[NUM_VERTICES];

            // Calculate the position of the vertices on the top face.
            Vector3 topLeftFront = Position + new Vector3(-1.0f, 1.0f, -1.0f) * Size;
            Vector3 topLeftBack = Position + new Vector3(-1.0f, 1.0f, 1.0f) * Size;
            Vector3 topRightFront = Position + new Vector3(1.0f, 1.0f, -1.0f) * Size;
            Vector3 topRightBack = Position + new Vector3(1.0f, 1.0f, 1.0f) * Size;

            // Calculate the position of the vertices on the bottom face.
            Vector3 btmLeftFront = Position + new Vector3(-1.0f, -1.0f, -1.0f) * Size;
            Vector3 btmLeftBack = Position + new Vector3(-1.0f, -1.0f, 1.0f) * Size;
            Vector3 btmRightFront = Position + new Vector3(1.0f, -1.0f, -1.0f) * Size;
            Vector3 btmRightBack = Position + new Vector3(1.0f, -1.0f, 1.0f) * Size;

            // Normal vectors for each face (needed for lighting / display)
            Vector3 normalFront = new Vector3(0.0f, 0.0f, 1.0f) * Size;
            Vector3 normalBack = new Vector3(0.0f, 0.0f, -1.0f) * Size;
            Vector3 normalTop = new Vector3(0.0f, 1.0f, 0.0f) * Size;
            Vector3 normalBottom = new Vector3(0.0f, -1.0f, 0.0f) * Size;
            Vector3 normalLeft = new Vector3(-1.0f, 0.0f, 0.0f) * Size;
            Vector3 normalRight = new Vector3(1.0f, 0.0f, 0.0f) * Size;

            // UV texture coordinates
            Vector2 textureTopLeft = new Vector2(1.0f * Size.X, 0.0f * Size.Y);
            Vector2 textureTopRight = new Vector2(0.0f * Size.X, 0.0f * Size.Y);
            Vector2 textureBottomLeft = new Vector2(1.0f * Size.X, 1.0f * Size.Y);
            Vector2 textureBottomRight = new Vector2(0.0f * Size.X, 1.0f * Size.Y);

            // Add the vertices for the FRONT face.
            _vertices[0] = new VertexPositionNormalTexture(topLeftFront, normalFront, textureTopLeft);
            _vertices[1] = new VertexPositionNormalTexture(btmLeftFront, normalFront, textureBottomLeft);
            _vertices[2] = new VertexPositionNormalTexture(topRightFront, normalFront, textureTopRight);
            _vertices[3] = new VertexPositionNormalTexture(btmLeftFront, normalFront, textureBottomLeft);
            _vertices[4] = new VertexPositionNormalTexture(btmRightFront, normalFront, textureBottomRight);
            _vertices[5] = new VertexPositionNormalTexture(topRightFront, normalFront, textureTopRight);

            // Add the vertices for the BACK face.
            _vertices[6] = new VertexPositionNormalTexture(topLeftBack, normalBack, textureTopRight);
            _vertices[7] = new VertexPositionNormalTexture(topRightBack, normalBack, textureTopLeft);
            _vertices[8] = new VertexPositionNormalTexture(btmLeftBack, normalBack, textureBottomRight);
            _vertices[9] = new VertexPositionNormalTexture(btmLeftBack, normalBack, textureBottomRight);
            _vertices[10] = new VertexPositionNormalTexture(topRightBack, normalBack, textureTopLeft);
            _vertices[11] = new VertexPositionNormalTexture(btmRightBack, normalBack, textureBottomLeft);

            // Add the vertices for the TOP face.
            _vertices[12] = new VertexPositionNormalTexture(topLeftFront, normalTop, textureBottomLeft);
            _vertices[13] = new VertexPositionNormalTexture(topRightBack, normalTop, textureTopRight);
            _vertices[14] = new VertexPositionNormalTexture(topLeftBack, normalTop, textureTopLeft);
            _vertices[15] = new VertexPositionNormalTexture(topLeftFront, normalTop, textureBottomLeft);
            _vertices[16] = new VertexPositionNormalTexture(topRightFront, normalTop, textureBottomRight);
            _vertices[17] = new VertexPositionNormalTexture(topRightBack, normalTop, textureTopRight);

            // Add the vertices for the BOTTOM face. 
            _vertices[18] = new VertexPositionNormalTexture(btmLeftFront, normalBottom, textureTopLeft);
            _vertices[19] = new VertexPositionNormalTexture(btmLeftBack, normalBottom, textureBottomLeft);
            _vertices[20] = new VertexPositionNormalTexture(btmRightBack, normalBottom, textureBottomRight);
            _vertices[21] = new VertexPositionNormalTexture(btmLeftFront, normalBottom, textureTopLeft);
            _vertices[22] = new VertexPositionNormalTexture(btmRightBack, normalBottom, textureBottomRight);
            _vertices[23] = new VertexPositionNormalTexture(btmRightFront, normalBottom, textureTopRight);

            // Add the vertices for the LEFT face.
            _vertices[24] = new VertexPositionNormalTexture(topLeftFront, normalLeft, textureTopRight);
            _vertices[25] = new VertexPositionNormalTexture(btmLeftBack, normalLeft, textureBottomLeft);
            _vertices[26] = new VertexPositionNormalTexture(btmLeftFront, normalLeft, textureBottomRight);
            _vertices[27] = new VertexPositionNormalTexture(topLeftBack, normalLeft, textureTopLeft);
            _vertices[28] = new VertexPositionNormalTexture(btmLeftBack, normalLeft, textureBottomLeft);
            _vertices[29] = new VertexPositionNormalTexture(topLeftFront, normalLeft, textureTopRight);

            // Add the vertices for the RIGHT face. 
            _vertices[30] = new VertexPositionNormalTexture(topRightFront, normalRight, textureTopLeft);
            _vertices[31] = new VertexPositionNormalTexture(btmRightFront, normalRight, textureBottomLeft);
            _vertices[32] = new VertexPositionNormalTexture(btmRightBack, normalRight, textureBottomRight);
            _vertices[33] = new VertexPositionNormalTexture(topRightBack, normalRight, textureTopRight);
            _vertices[34] = new VertexPositionNormalTexture(topRightFront, normalRight, textureTopLeft);
            _vertices[35] = new VertexPositionNormalTexture(btmRightBack, normalRight, textureBottomRight);

            _isConstructed = true;
        }

        public override void Update(UpdateState GState)
        {
        }

        public override void Draw(GraphicsDevice device)
        {
            if (_isConstructed == false)
                ConstructCube();

            var cubeEffect = new BasicEffect(device, null);


            Vector3 cameraPosition = new Vector3(0, 3, 4);
            Vector3 modelPosition = Vector3.Zero;
            float rotation = 0.0f;
            float aspectRatio = 0.0f;

            cubeEffect.World = Matrix.CreateRotationY(MathHelper.ToRadians(rotation)) *
                Matrix.CreateRotationX(MathHelper.ToRadians(rotation)) * Matrix.CreateTranslation(modelPosition);

            // Set the View matrix which defines the camera and what it's looking at
            cubeEffect.View = Matrix.CreateLookAt(cameraPosition, modelPosition, Vector3.Up);

            // Set the Projection matrix which defines how we see the scene (Field of view)
            cubeEffect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 1.0f, 1000.0f);

            // Enable textures on the Cube Effect. this is necessary to texture the model
            cubeEffect.TextureEnabled = true;
            cubeEffect.Texture = m_Texture;

            // Enable some pretty lights
            cubeEffect.EnableDefaultLighting();



            using (VertexBuffer buffer = new VertexBuffer(
                    device, typeof(VertexPositionNormalTexture),
                    NUM_VERTICES,
                    BufferUsage.WriteOnly))
            {
                // Load the buffer
                buffer.SetData(_vertices);

                // Send the vertex buffer to the device
                //device.SetVertexBuffer(buffer);

                device.Vertices[0].SetSource(buffer, 0, VertexPositionNormalTexture.SizeInBytes);
                device.VertexDeclaration = new VertexDeclaration(device, VertexPositionNormalTexture.VertexElements);

            }


            // apply the effect and render the cube
            foreach (EffectPass pass in cubeEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                // Draw the primitives from the vertex buffer to the device as triangles
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, NUM_TRIANGLES);

                pass.End();
            }




        }
    }
}
