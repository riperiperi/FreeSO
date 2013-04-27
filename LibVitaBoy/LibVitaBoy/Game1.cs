using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using System.IO;
using System.Text;

namespace LibVitaBoy
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private Mesh Head;
        private Mesh Body;
        private Mesh RightHand;
        private Mesh LeftHand;

        private Texture2D HeadTexture;
        private Texture2D BodyTexture;
        private Texture2D RightHandTexture;
        private Texture2D LeftHandTexture;

        private Skelenton Skelenton;

        private Matrix View;
        private Matrix Projection;
        private Matrix World;



        public Game1()
        {

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();


            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi / 4.0f, GraphicsDevice.Viewport.Width / GraphicsDevice.Viewport.Height, 1.0f, 2000.0f);
            View = Matrix.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up);
            World = Matrix.Identity;




            Head = new Mesh();
            Head.Read(File.ReadAllBytes("head.mesh"));

            Body = new Mesh();
            Body.Read(File.ReadAllBytes("body.mesh"));

            HeadTexture = Texture2D.FromFile(GraphicsDevice, "head.jpg");
            BodyTexture = Texture2D.FromFile(GraphicsDevice, "body.jpg");

            RightHand = new Mesh();
            RightHand.Read(File.ReadAllBytes("rhand.mesh"));

            LeftHand = new Mesh();
            LeftHand.Read(File.ReadAllBytes("lhand.mesh"));

            RightHandTexture = Texture2D.FromFile(GraphicsDevice, "hand.jpg");
            LeftHandTexture = Texture2D.FromFile(GraphicsDevice, "hand.jpg");

            Skelenton = new Skelenton();
            Skelenton.Read(File.ReadAllBytes("skeleton.skel"));


        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }



        private float m_RotationX = 0.0f;
        private float m_RotationY = 0.0f;
        private float m_RotationZ = 0.0f;
        private float m_Scale = 1.0f;


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            var kb = Keyboard.GetState();
            if (kb.IsKeyDown(Keys.Left))
            {
                if (kb.IsKeyDown(Keys.LeftShift))
                {
                    m_RotationZ -= 0.01f;
                }
                else
                {
                    m_RotationX -= 0.01f;
                }
            }
            else if (kb.IsKeyDown(Keys.Right))
            {
                if (kb.IsKeyDown(Keys.LeftShift))
                {
                    m_RotationZ += 0.01f;
                }
                else
                {
                    m_RotationX += 0.01f;
                }
            }
            else if (kb.IsKeyDown(Keys.Up))
            {
                m_RotationY += 0.01f;
            }
            else if (kb.IsKeyDown(Keys.Down))
            {
                m_RotationY -= 0.01f;
            }
            else if (kb.IsKeyDown(Keys.Add))
            {
                m_Scale += 0.01f;
            }
            else if (kb.IsKeyDown(Keys.Subtract))
            {
                m_Scale -= 0.01f;
            }


            World = Matrix.Identity * Matrix.CreateScale(m_Scale) * Matrix.CreateRotationX(m_RotationX) * Matrix.CreateRotationY(m_RotationY) * Matrix.CreateRotationZ(m_RotationZ);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            /**glRotatef(Character.Rotation.x,1.0f,0.0f,0.0f);
    glRotatef(Character.Rotation.y,0.0f,1.0f,0.0f);
    glRotatef(Character.Rotation.z,0.0f,0.0f,1.0f);*/

            var matrix = Matrix.Identity;



            ComputeBonePositions(Skelenton.RootBone, Matrix.Identity);
            DrawMeshes();
            DrawBones(Skelenton);
            base.Draw(gameTime);
        }






        public void DrawMeshes()
        {
            TransformVertices(Head, Skelenton.RootBone);
            //BlendVertices(Head);
            DrawMesh(Head, HeadTexture);

            TransformVertices(Body, Skelenton.RootBone);
            //BlendVertices(Body);
            DrawMesh(Body, BodyTexture);

            TransformVertices(RightHand, Skelenton.RootBone);
            //BlendVertices(RightHand);
            DrawMesh(RightHand, RightHandTexture);

            TransformVertices(LeftHand, Skelenton.RootBone);
            //BlendVertices(LeftHand);
            DrawMesh(LeftHand, LeftHandTexture);
        }

        private void BlendVertices(Mesh mesh)
        {
            for (var i = 0; i < mesh.BlendVertexCount; i++)
            {
                var blendVertex = mesh.TransformedVertex[mesh.RealVertexCount + i];
                var weight = blendVertex.BlendData.Weight;

                var realVertex = mesh.TransformedVertex[blendVertex.BlendData.OtherVertex];
                realVertex.Vertex.Coord.X = weight * blendVertex.Vertex.Coord.X + (1 - weight) * realVertex.Vertex.Coord.X;
                realVertex.Vertex.Coord.Y = weight * blendVertex.Vertex.Coord.Y + (1 - weight) * realVertex.Vertex.Coord.Y;
                realVertex.Vertex.Coord.Z = weight * blendVertex.Vertex.Coord.Z + (1 - weight) * realVertex.Vertex.Coord.Z;
            }
        }

        private void TransformVertices(Mesh mesh, Bone bone){
            //var x = mesh.BoneBindings;
            var boneBinding = mesh.BoneBindings.FirstOrDefault(x => mesh.BoneNames[x.BoneIndex] == bone.Name);

            if (boneBinding != null){
                for (var i = 0; i < boneBinding.RealVertexCount; i++)
                {
                    var vertexIndex = boneBinding.FirstRealVertex + i;
                    var transformedVertex = mesh.TransformedVertex[vertexIndex];
                    var relativeVertex = mesh.Vertex[vertexIndex];

                    var translatedMatrix = Matrix.CreateTranslation(new Vector3(relativeVertex.Vertex.Coord.X, relativeVertex.Vertex.Coord.Y, relativeVertex.Vertex.Coord.Z)) * bone.AbsoluteMatrix;
                    transformedVertex.Vertex.Coord = Vector3.Transform(Vector3.Zero, translatedMatrix);

                    //var translatedMatrix = bone.AbsoluteMatrix * Matrix.CreateTranslation(relativeVertex.Vertex.Coord);
                    //transformedVertex.Vertex.Coord = Vector3.Transform(Vector3.Zero, translatedMatrix);

                }

                for (var i = 0; i < boneBinding.BlendVertexCount; i++)
                {
                    var vertexIndex = boneBinding.FirstBlendVertex + i;
                    var transformedVertex = mesh.TransformedVertex[vertexIndex];
                    var relativeVertex = mesh.Vertex[vertexIndex];


                }

                //for (var i = 0; i < boneBinding.BlendVertexCount; i++)
                //{
                //    var vertexIndex = boneBinding.FirstBlendVertex + i;
                //    var transformedVertex = mesh.TransformedVertex[vertexIndex];
                //    var relativeVertex = mesh.Vertex[vertexIndex];


                //    //var translatedMatrix = bone.AbsoluteMatrix * Matrix.CreateTranslation(relativeVertex.Vertex.Coord);
                //    //transformedVertex.Vertex.Coord = Vector3.Transform(Vector3.Zero, translatedMatrix);

                //    var translatedMatrix = Matrix.CreateTranslation(new Vector3(relativeVertex.Vertex.Coord.X, relativeVertex.Vertex.Coord.Y, relativeVertex.Vertex.Coord.Z)) * bone.AbsoluteMatrix;
                //    transformedVertex.Vertex.Coord = Vector3.Transform(Vector3.Zero, translatedMatrix);
                //}
            }

            foreach (var child in bone.Children){
                TransformVertices(mesh, child);
            }
        }

        private void DrawBones(Skelenton skel)
        {
            var device = GraphicsDevice;

            device.RenderState.PointSize = 10.0f;
            device.VertexDeclaration = new VertexDeclaration(GraphicsDevice, VertexPositionColor.VertexElements);
            device.RenderState.CullMode = CullMode.None;

            var effect = new BasicEffect(GraphicsDevice, null);
            

            //effect.Texture = TextureUtils.TextureFromColor(device, color);
            //effect.TextureEnabled = true;
            
            effect.World = World;
            effect.View = View;
            effect.Projection = Projection;
            effect.VertexColorEnabled = true;
            effect.EnableDefaultLighting();
            
            effect.CommitChanges();
            effect.Begin();
            foreach (var pass in effect.Techniques[0].Passes)
            {
                pass.Begin();

                foreach(var bone in skel.Bones){
                    var color = Color.Green;

                    if (bone.Name == "ROOT")
                    {
                        color = Color.Red;
                    }
                    else if (bone.Name == "HEAD")
                    {
                        color = Color.Yellow;
                    }

                    var vertex = new VertexPositionColor(bone.AbsolutePosition, color);
                    var vertexList = new VertexPositionColor[1]{vertex};
                    device.DrawUserPrimitives(PrimitiveType.PointList, vertexList, 0, 1);
                }
                pass.End();
            }
            effect.End();
        }


        private bool firstBoneCompute = true;
        private StringBuilder boneLog = null;

        private string PrintMatrix(Matrix mtx)
        {
            return mtx.M11 + "," + mtx.M12 + "," + mtx.M13 + "," + mtx.M14 + "," + mtx.M21 + "," + mtx.M22 + "," + mtx.M23 + "," + mtx.M24 + "," + mtx.M31 + "," + mtx.M32 + "," + mtx.M33 + "," + mtx.M34 + "," + mtx.M41 + "," + mtx.M42 + "," + mtx.M43 + "," + mtx.M44;
        }

        private void ComputeBonePositions(Bone bone, Matrix world)
        {

            var translateMatrix = Matrix.CreateTranslation(bone.Translation);
            var rotationMatrix = FindQuaternionMatrix(bone.Rotation);

            var myWorld = (rotationMatrix * translateMatrix) * world;
            bone.AbsolutePosition = Vector3.Transform(Vector3.Zero, myWorld);
            bone.AbsoluteMatrix = myWorld;

            foreach (var child in bone.Children){
                ComputeBonePositions(child, myWorld);
            }
        }


        private Matrix MatrixFromGL(params float[] args)
        {
            var result = new Matrix();
            result.M11 = args[0];
            result.M12 = args[1];
            result.M13 = args[2];
            result.M14 = args[3];

            result.M21 = args[4];
            result.M22 = args[5];
            result.M23 = args[6];
            result.M24 = args[7];

            result.M31 = args[8];
            result.M32 = args[9];
            result.M33 = args[10];
            result.M34 = args[11];

            result.M41 = args[12];
            result.M42 = args[13];
            result.M43 = args[14];
            result.M44 = args[15];

            return result;
        }

        private Matrix FindQuaternionMatrix(Vector4 Quaternion){
            float x2 = Quaternion.X * Quaternion.X;
            float y2 = Quaternion.Y * Quaternion.Y;
            float z2 = Quaternion.Z * Quaternion.Z;
            float xy = Quaternion.X * Quaternion.Y;
            float xz = Quaternion.X * Quaternion.Z;
            float yz = Quaternion.Y * Quaternion.Z;
            float wx = Quaternion.W * Quaternion.X;
            float wy = Quaternion.W * Quaternion.Y;
            float wz = Quaternion.W * Quaternion.Z;



            var mtxIn = new Matrix();

            mtxIn.M11 = 1.0f - 2.0f * (y2 + z2);
            mtxIn.M12 = 2.0f * (xy - wz);
            mtxIn.M13 = 2.0f * (xz + wy);
            mtxIn.M14 = 0.0f;
            mtxIn.M21 = 2.0f * (xy + wz);
            mtxIn.M22 = 1.0f - 2.0f * (x2 + z2);
            mtxIn.M23 = 2.0f * (yz - wx);
            mtxIn.M24 = 0.0f;
            mtxIn.M31 = 2.0f * (xz - wy);
            mtxIn.M32 = 2.0f * (yz + wx);
            mtxIn.M33 = 1.0f - 2.0f * (x2 + y2);
            mtxIn.M34 = 0.0f;
            mtxIn.M41 = 0.0f;
            mtxIn.M42= 0.0f;
            mtxIn.M43 = 0.0f;
            mtxIn.M44 = 1.0f;

            return mtxIn;
        }




        private void DrawMesh(Mesh mesh, Texture2D texture)
        {
            //glTranslatef(Character.Translation.x, Character.Translation.y, zoom + Character.Translation.z);
            //glRotatef(Character.Rotation.x, 1.0f, 0.0f, 0.0f);
            //glRotatef(Character.Rotation.y, 0.0f, 1.0f, 0.0f);
            //glRotatef(Character.Rotation.z, 0.0f, 0.0f, 1.0f);



            var device = GraphicsDevice;

            device.VertexDeclaration = new VertexDeclaration(GraphicsDevice, MeshVertex.VertexElements);
            device.RenderState.CullMode = CullMode.None;

            var effect = new BasicEffect(GraphicsDevice, null);
            effect.Texture = texture;
            effect.TextureEnabled = true;
            effect.VertexColorEnabled = false;
            effect.World = World;
            effect.View = View;
            effect.Projection = Projection;
            effect.CommitChanges();

            effect.Begin();
            foreach (var pass in effect.Techniques[0].Passes)
            {
                pass.Begin();

                foreach (var face in mesh.FaceData)
                {
                    var vertexA = mesh.TransformedVertex[face.VertexA];
                    var vertexB = mesh.TransformedVertex[face.VertexB];
                    var vertexC = mesh.TransformedVertex[face.VertexC];

                    var vertexList = new MeshVertex[3] { vertexA.Vertex, vertexB.Vertex, vertexC.Vertex };
                    device.DrawUserPrimitives(PrimitiveType.TriangleList, vertexList, 0, 1);
                }

                //device.DrawUserPrimitives(PrimitiveType.TriangleList, mesh.TransformedVertexData, 0, mesh.TransformedVertexData.Length / 3);
                pass.End();
            }
            effect.End();
        }






    }
}
