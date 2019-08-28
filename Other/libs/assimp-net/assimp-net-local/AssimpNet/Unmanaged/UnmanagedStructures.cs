/*
* Copyright (c) 2012-2014 AssimpNet - Nicholas Woodfield
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Assimp.Unmanaged
{
    /// <summary>
    /// Represents an aiScene struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiScene
    {
        /// <summary>
        /// unsigned int, flags about the state of the scene
        /// </summary>
        public SceneFlags Flags;

        /// <summary>
        /// aiNode*, root node of the scenegraph.
        /// </summary>
        public IntPtr RootNode;

        /// <summary>
        /// Number of meshes contained.
        /// </summary>
        public uint NumMeshes;

        /// <summary>
        /// aiMesh**, meshes in the scene.
        /// </summary>
        public IntPtr Meshes;

        /// <summary>
        /// Number of materials contained.
        /// </summary>
        public uint NumMaterials;

        /// <summary>
        /// aiMaterial**, materials in the scene.
        /// </summary>
        public IntPtr Materials;

        /// <summary>
        /// Number of animations contained.
        /// </summary>
        public uint NumAnimations;

        /// <summary>
        /// aiAnimation**, animations in the scene.
        /// </summary>
        public IntPtr Animations;

        /// <summary>
        /// Number of embedded textures contained.
        /// </summary>
        public uint NumTextures;

        /// <summary>
        /// aiTexture**, textures in the scene.
        /// </summary>
        public IntPtr Textures;

        /// <summary>
        /// Number of lights contained.
        /// </summary>
        public uint NumLights;

        /// <summary>
        /// aiLight**, lights in the scene.
        /// </summary>
        public IntPtr Lights;

        /// <summary>
        /// Number of cameras contained.
        /// </summary>
        public uint NumCameras;

        /// <summary>
        /// aiCamera**, cameras in the scene.
        /// </summary>
        public IntPtr Cameras;

        /// <summary>
        /// void*, Private data do not touch!
        /// </summary>
        public IntPtr PrivateData;
    }

    /// <summary>
    /// Represents an aiNode struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiNode
    {
        /// <summary>
        /// Name of the node.
        /// </summary>
        public AiString Name;

        /// <summary>
        /// Node's transform relative to its parent.
        /// </summary>
        public Matrix4x4 Transformation;

        /// <summary>
        /// aiNode*, node's parent.
        /// </summary>
        public IntPtr Parent;

        /// <summary>
        /// Number of children the node owns.
        /// </summary>
        public uint NumChildren;

        /// <summary>
        /// aiNode**, array of nodes this node owns.
        /// </summary>
        public IntPtr Children;

        /// <summary>
        /// Number of meshes referenced by this node.
        /// </summary>
        public uint NumMeshes;

        /// <summary>
        /// unsigned int*, array of mesh indices.
        /// </summary>
        public IntPtr Meshes;

        /// <summary>
        /// aiMetadata*, pointer to a metadata container. May be NULL, if an importer doesn't document metadata then it doesn't write any.
        /// </summary>
        public IntPtr MetaData;
    }

    /// <summary>
    /// Represents an aiMetadataEntry struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiMetadataEntry
    {
        /// <summary>
        /// Type of metadata.
        /// </summary>
        public MetaDataType DataType;

        /// <summary>
        /// Pointer to data.
        /// </summary>
        public IntPtr Data;
    }

    /// <summary>
    /// Represents an aiMetadata struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiMetadata
    {
        /// <summary>
        /// Length of the Keys and Values arrays.
        /// </summary>
        public uint NumProperties;

        /// <summary>
        /// aiString*, array of keys. May not be NULL. Each entry must exist.
        /// </summary>
        public IntPtr keys;

        /// <summary>
        /// aiMetadataEntry*, array of values. May not be NULL. Entries may be NULL if the corresponding property key has no assigned value.
        /// </summary>
        public IntPtr Values;
    }

    /// <summary>
    /// Represents an aiMesh struct. Note: This structure requires marshaling, due to the arrays of IntPtrs.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiMesh
    {
        /// <summary>
        /// unsigned int, bitwise flag detailing types of primitives contained.
        /// </summary>
        public PrimitiveType PrimitiveTypes;

        /// <summary>
        /// Number of vertices in the mesh, denotes length of
        /// -all- per-vertex arrays.
        /// </summary>
        public uint NumVertices;

        /// <summary>
        /// Number of faces in the mesh.
        /// </summary>
        public uint NumFaces;

        /// <summary>
        /// aiVector3D*, array of positions.
        /// </summary>
        public IntPtr Vertices;

        /// <summary>
        /// aiVector3D*, array of normals.
        /// </summary>
        public IntPtr Normals;

        /// <summary>
        /// aiVector3D*, array of tangents.
        /// </summary>
        public IntPtr Tangents;

        /// <summary>
        /// aiVector3D*, array of bitangents.
        /// </summary>
        public IntPtr BiTangents;

        /// <summary>
        /// aiColor4D*[Max_Value], array of arrays of vertex colors. Max_Value is defined as <see cref="AiDefines.AI_MAX_NUMBER_OF_COLOR_SETS"/>.
        /// </summary>
        public AiMeshColorArray Colors;

        /// <summary>
        /// aiVector3D*[Max_Value], array of arrays of texture coordinates. Max_Value is defined as <see cref="AiDefines.AI_MAX_NUMBER_OF_TEXTURECOORDS"/>.
        /// </summary>
        public AiMeshTextureCoordinateArray TextureCoords;

        /// <summary>
        /// unsigned int[Max_Value], array of ints denoting the number of components for each set of texture coordinates - UV (2), UVW (3) for example.
        /// Max_Value is defined as <see cref="AiDefines.AI_MAX_NUMBER_OF_TEXTURECOORDS"/>.
        /// </summary>
        public AiMeshUVComponentArray NumUVComponents;
 
        /// <summary>
        /// aiFace*, array of faces.
        /// </summary>
        public IntPtr Faces;

        /// <summary>
        /// Number of bones in the mesh.
        /// </summary>
        public uint NumBones;

        /// <summary>
        /// aiBone**, array of bones.
        /// </summary>
        public IntPtr Bones;

        /// <summary>
        /// Material index referencing the material in the scene.
        /// </summary>
        public uint MaterialIndex;

        /// <summary>
        /// Optional name of the mesh.
        /// </summary>
        public AiString Name;

        /// <summary>
        /// Number of attachment meshes. NOT CURRENTLY IN USE.
        /// </summary>
        public uint NumAnimMeshes;

        /// <summary>
        /// aiAnimMesh**, array of attachment meshes for vertex-based animation. NOT CURRENTLY IN USE.
        /// </summary>
        public IntPtr AnimMeshes;

        /// <summary>
        /// Method of morphing when animeshes are specified. 
        /// </summary>
        public int Method;
    }

    /// <summary>
    /// Represents an aiTexture struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public unsafe struct AiTexture
    {
        //Internal use only
        private static readonly char[] s_nullFormat = new char[] { '\0', '\0', '\0', '\0' };

        /// <summary>
        /// Width of the texture.
        /// </summary>
        public uint Width;

        /// <summary>
        /// Height of the texture.
        /// </summary>
        public uint Height;

        /// <summary>
        /// sbyte[9], format extension hint. Fixed size char is two bytes regardless of encoding. Unmanaged assimp uses a char that
        /// maps to one byte.
        /// </summary>
        public fixed sbyte FormatHint[9];

        /// <summary>
        /// aiTexel*, array of texel data.
        /// </summary>
        public IntPtr Data;

        /// <summary>
        /// Sets the format hint.
        /// </summary>
        /// <param name="formatHint">Format hint - must be 3 characters or less</param>
        public void SetFormatHint(String formatHint)
        {
            char[] hintChars = (String.IsNullOrEmpty(formatHint)) ? s_nullFormat : formatHint.ToLowerInvariant().ToCharArray();

            int count = Math.Min(hintChars.Length, 8);

            fixed(sbyte* charPtr = FormatHint)
            {
                for (int i=0; i < count; i++)
                    charPtr[i] = (sbyte) hintChars[i];
                for (int i = count; i < 9; i++)
                    charPtr[i] = 0;
            }
        }

        /// <summary>
        /// Gets the format hint.
        /// </summary>
        /// <returns>The format hint</returns>
        public String GetFormatHint()
        {
            fixed(sbyte* charPtr = FormatHint)
            {
                return new String(charPtr);
            }
        }
    }

    /// <summary>
    /// Represents an aiFace struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiFace
    {
        /// <summary>
        /// Number of indices in the face.
        /// </summary>
        public uint NumIndices;

        /// <summary>
        /// unsigned int*, array of indices.
        /// </summary>
        public IntPtr Indices;
    }

    /// <summary>
    /// Represents an aiBone struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiBone
    {
        /// <summary>
        /// Name of the bone.
        /// </summary>
        public AiString Name;

        /// <summary>
        /// Number of weights.
        /// </summary>
        public uint NumWeights;

        /// <summary>
        /// VertexWeight*, array of vertex weights.
        /// </summary>
        public IntPtr Weights;

        /// <summary>
        /// Matrix that transforms the vertex from mesh to bone space in bind pose
        /// </summary>
        public Matrix4x4 OffsetMatrix;
    }

    /// <summary>
    /// Represents an aiMaterialProperty struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiMaterialProperty
    {
        /// <summary>
        /// Name of the property (key).
        /// </summary>
        public AiString Key;

        /// <summary>
        /// Textures: Specifies texture usage. None texture properties
        /// have this zero (or None).
        /// </summary>
        public TextureType Semantic;

        /// <summary>
        /// Textures: Specifies the index of the texture. For non-texture properties
        /// this is always zero.
        /// </summary>
        public uint Index;

        /// <summary>
        /// Size of the buffer data in bytes. This value may not be zero.
        /// </summary>
        public uint DataLength;

        /// <summary>
        /// Type of value contained in the buffer.
        /// </summary>
        public PropertyType Type;

        /// <summary>
        /// char*, byte buffer to hold the property's value.
        /// </summary>
        public IntPtr Data;
    }

    /// <summary>
    /// Represents an aiMaterial struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiMaterial
    {
        /// <summary>
        /// aiMaterialProperty**, array of material properties.
        /// </summary>
        public IntPtr Properties;

        /// <summary>
        /// Number of key-value properties.
        /// </summary>
        public uint NumProperties;

        /// <summary>
        /// Storage allocated for key-value properties.
        /// </summary>
        public uint NumAllocated;
    }

    /// <summary>
    /// Represents an aiNodeAnim struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiNodeAnim
    {
        /// <summary>
        /// Name of the node affected by the animation. The node must exist
        /// and be unique.
        /// </summary>
        public AiString NodeName;

        /// <summary>
        /// Number of position keys.
        /// </summary>
        public uint NumPositionKeys;

        /// <summary>
        /// VectorKey*, position keys of this animation channel. Positions
        /// are 3D vectors and are accompanied by at least one scaling and one rotation key.
        /// </summary>
        public IntPtr PositionKeys;

        /// <summary>
        /// The number of rotation keys.
        /// </summary>
        public uint NumRotationKeys;

        /// <summary>
        /// QuaternionKey*, rotation keys of this animation channel. Rotations are 4D vectors (quaternions).
        /// If there are rotation keys there will be at least one scaling and one position key.
        /// </summary>
        public IntPtr RotationKeys;

        /// <summary>
        /// Number of scaling keys.
        /// </summary>
        public uint NumScalingKeys;

        /// <summary>
        /// VectorKey*, scaling keys of this animation channel. Scalings are specified as a
        /// 3D vector, and if there are scaling keys, there will at least be one position
        /// and one rotation key.
        /// </summary>
        public IntPtr ScalingKeys;

        /// <summary>
        /// Defines how the animation behaves before the first key is encountered.
        /// </summary>
        public AnimationBehaviour Prestate;

        /// <summary>
        /// Defines how the animation behaves after the last key was processed.
        /// </summary>
        public AnimationBehaviour PostState;
    }

    /// <summary>
    /// Represents an aiMeshAnim struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiMeshAnim
    {
        /// <summary>
        /// Name of the mesh to be animated. Empty string not allowed.
        /// </summary>
        public AiString Name;

        /// <summary>
        /// Number of keys, there is at least one.
        /// </summary>
        public uint NumKeys;

        /// <summary>
        /// aiMeshkey*, the key frames of the animation. There must exist at least one.
        /// </summary>
        public IntPtr Keys;
    }

    /// <summary>
    /// Represents an aiAnimation struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiAnimation
    {
        /// <summary>
        /// Name of the animation.
        /// </summary>
        public AiString Name;

        /// <summary>
        /// Duration of the animation in ticks.
        /// </summary>
        public double Duration;

        /// <summary>
        /// Ticks per second, 0 if not specified in imported file.
        /// </summary>
        public double TicksPerSecond;

        /// <summary>
        /// Number of bone animation channels, each channel affects a single node.
        /// </summary>
        public uint NumChannels;

        /// <summary>
        /// aiNodeAnim**, node animation channels. Each channel affects a single node.
        /// </summary>
        public IntPtr Channels;

        /// <summary>
        /// Number of mesh animation channels. Each channel affects a single mesh and defines
        /// vertex-based animation.
        /// </summary>
        public uint NumMeshChannels;

        /// <summary>
        /// aiMeshAnim**, mesh animation channels. Each channel affects a single mesh. 
        /// </summary>
        public IntPtr MeshChannels;

        /// <summary>
        /// The number of mesh animation channels. Each channel affects
        /// a single mesh and defines morphing animation. 
        /// </summary>
        public uint NumMorphMeshChannels;

        /// <summary>
        /// aiMeshMorphAnim**, morph mesh animation channels. Each channel affects a single mesh. 
        /// </summary>
        public IntPtr MorphMeshChannels;
    }

    /// <summary>
    /// Represents an aiLight struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiLight
    {
        /// <summary>
        /// Name of the light.
        /// </summary>
        public AiString Name;

        /// <summary>
        /// Type of light.
        /// </summary>
        public LightSourceType Type;

        /// <summary>
        /// Position of the light.
        /// </summary>
        public Vector3D Position;

        /// <summary>
        /// Direction of the spot/directional light.
        /// </summary>
        public Vector3D Direction;

        /// <summary>
        /// Up direction of the spot/directional light.
        /// </summary>
        public Vector3D Up;

        /// <summary>
        /// Attenuation constant value.
        /// </summary>
        public float AttenuationConstant;

        /// <summary>
        /// Attenuation linear value.
        /// </summary>
        public float AttenuationLinear;

        /// <summary>
        /// Attenuation quadratic value.
        /// </summary>
        public float AttenuationQuadratic;

        /// <summary>
        /// Diffuse color.
        /// </summary>
        public Color3D ColorDiffuse;

        /// <summary>
        /// Specular color.
        /// </summary>
        public Color3D ColorSpecular;

        /// <summary>
        /// Ambient color.
        /// </summary>
        public Color3D ColorAmbient;

        /// <summary>
        /// Spot light inner angle.
        /// </summary>
        public float AngleInnerCone;

        /// <summary>
        /// Spot light outer angle.
        /// </summary>
        public float AngleOuterCone;

        /// <summary>
        /// Area light size.
        /// </summary>
        public Vector2D Size;
    }

    /// <summary>
    /// Represents an aiCamera struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiCamera
    {
        /// <summary>
        /// Name of the camera.
        /// </summary>
        public AiString Name;

        /// <summary>
        /// Position of the camera.
        /// </summary>
        public Vector3D Position;

        /// <summary>
        /// Up vector of the camera.
        /// </summary>
        public Vector3D Up;

        /// <summary>
        /// Viewing direction of the camera.
        /// </summary>
        public Vector3D LookAt;

        /// <summary>
        /// Field Of View of the camera.
        /// </summary>
        public float HorizontalFOV;

        /// <summary>
        /// Near clip plane distance.
        /// </summary>
        public float ClipPlaneNear;

        /// <summary>
        /// Far clip plane distance.
        /// </summary>
        public float ClipPlaneFar;

        /// <summary>
        /// The Aspect ratio.
        /// </summary>
        public float Aspect;
    }

    /// <summary>
    /// Represents an aiString struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public unsafe struct AiString
    {
        /// <summary>
        /// Byte length of the UTF-8 string.
        /// </summary>
        public UIntPtr Length;

        /// <summary>
        /// Actual string data.
        /// </summary>
        public fixed byte Data[AiDefines.MAX_LENGTH];

        /// <summary>
        /// Constructs a new instance of the <see cref="AiString"/> struct.
        /// </summary>
        /// <param name="data">The string data</param>
        public AiString(String data)
        {
            Length = UIntPtr.Zero;

            SetString(data);
        }

        /// <summary>
        /// Convienence method for getting the AiString string - if the length is not greater than zero, it returns
        /// an empty string rather than garbage.
        /// </summary>
        /// <returns>AiString string data</returns>
        public unsafe String GetString()
        {
            int length = (int) Length.ToUInt32();

            if(length > 0)
            {
                byte[] copy = new byte[length];

                fixed(byte* bytePtr = Data)
                {
                    MemoryHelper.Read<byte>(new IntPtr(bytePtr), copy, 0, length);
                }

                //Note: aiTypes.h specifies aiString is UTF-8 encoded string.
                return Encoding.UTF8.GetString(copy, 0, length);
            }
            else
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Convienence method for setting the AiString string (and length).
        /// </summary>
        /// <param name="data">String data to set</param>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public unsafe bool SetString(String data)
        {
            if(String.IsNullOrEmpty(data))
            {
                Length = new UIntPtr(0);
                fixed(byte* bytePtr = Data)
                    MemoryHelper.ClearMemory(new IntPtr(bytePtr), 0, AiDefines.MAX_LENGTH);

                return true;
            }

            //Note: aiTypes.h specifies aiString is UTF-8 encoded string.
            if(Encoding.UTF8.GetByteCount(data) <= AiDefines.MAX_LENGTH)
            {
                byte[] copy = Encoding.UTF8.GetBytes(data);

                //Write bytes to data field
                if(copy.Length > 0)
                {
                    fixed(byte* bytePtr = Data)
                        MemoryHelper.Write<byte>(new IntPtr(bytePtr), copy, 0, copy.Length);
                }

                Length = new UIntPtr((uint) copy.Length);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the fully qualified type name of this instance.
        /// </summary>
        /// <returns>A <see cref="T:System.String" /> containing a fully qualified type name.</returns>
        public override String ToString()
        {
            return GetString();
        }
    }

    /// <summary>
    /// Represents a log stream, which receives all log messages and streams them somewhere.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiLogStream
    {
        /// <summary>
        /// Function pointer that gets called when a message is to be logged.
        /// </summary>
        public IntPtr Callback;

        /// <summary>
        /// char*, user defined opaque data.
        /// </summary>
        public IntPtr UserData;
    }

    /// <summary>
    /// Represents the memory requirements for the different components of an imported
    /// scene. All sizes in in bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiMemoryInfo
    {
        /// <summary>
        /// Size of the storage allocated for texture data, in bytes.
        /// </summary>
        public uint Textures;

        /// <summary>
        /// Size of the storage allocated for material data, in bytes.
        /// </summary>
        public uint Materials;

        /// <summary>
        /// Size of the storage allocated for mesh data, in bytes.
        /// </summary>
        public uint Meshes;

        /// <summary>
        /// Size of the storage allocated for node data, in bytes.
        /// </summary>
        public uint Nodes;

        /// <summary>
        /// Size of the storage allocated for animation data, in bytes.
        /// </summary>
        public uint Animations;

        /// <summary>
        /// Size of the storage allocated for camera data, in bytes.
        /// </summary>
        public uint Cameras;

        /// <summary>
        /// Size of the storage allocated for light data, in bytes.
        /// </summary>
        public uint Lights;

        /// <summary>
        /// Total storage allocated for the imported scene, in bytes.
        /// </summary>
        public uint Total;
    }

    /// <summary>
    /// Represents an aiAnimMesh struct. Note: This structure requires marshaling, due to the array of IntPtrs.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiAnimMesh
    {
        /// <summary>
        /// aiVector3D*, replacement position array.
        /// </summary>
        public IntPtr Vertices;

        /// <summary>
        /// aiVector3D*, replacement normal array.
        /// </summary>
        public IntPtr Normals;

        /// <summary>
        /// aiVector3D*, replacement tangent array.
        /// </summary>
        public IntPtr Tangents;

        /// <summary>
        /// aiVector3D*, replacement bitangent array.
        /// </summary>
        public IntPtr BiTangents;

        /// <summary>
        /// aiColor4D*[Max_Value], array of arrays of vertex colors. Max_Value is defined as <see cref="AiDefines.AI_MAX_NUMBER_OF_COLOR_SETS"/>.
        /// </summary>
        public AiMeshColorArray Colors;

        /// <summary>
        /// aiVector3D*[Max_Value], array of arrays of texture coordinates. Max_Value is defined as <see cref="AiDefines.AI_MAX_NUMBER_OF_TEXTURECOORDS"/>.
        /// </summary>
        public AiMeshTextureCoordinateArray TextureCoords;

        /// <summary>
        /// unsigned int, number of vertices.
        /// </summary>
        public uint NumVertices;

        /// <summary>
        /// Weight of the AnimMesh.
        /// </summary>
        public float Weight;
    }

    /// <summary>
    /// Describes a file format which Assimp can export to.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiExportFormatDesc
    {
        /// <summary>
        /// char*, a short string ID to uniquely identify the export format. e.g. "collada" or "obj"
        /// </summary>
        public IntPtr FormatId;

        /// <summary>
        /// char*, a short description of the file format to present to users.
        /// </summary>
        public IntPtr Description;

        /// <summary>
        /// char*, a recommended file extension of the exported file in lower case.
        /// </summary>
        public IntPtr FileExtension;
    }

    /// <summary>
    /// Describes a blob of exported scene data. Blobs can be nested, the first blob always has an empty name. Nested
    /// blobs represent auxillary files produced by the exporter (e.g. material files) and are named accordingly.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiExportDataBlob
    {
        /// <summary>
        /// size_t, size of the data in bytes.
        /// </summary>
        public UIntPtr Size;

        /// <summary>
        /// void*, the data.
        /// </summary>
        public IntPtr Data;

        /// <summary>
        /// AiString, name of the blob.
        /// </summary>
        public AiString Name;

        /// <summary>
        /// aiExportDataBlob*, pointer to the next blob in the chain.
        /// </summary>
        public IntPtr NextBlob;
    }

    /// <summary>
    /// Contains callbacks to implement a custom file system to open and close files.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiFileIO
    {
        /// <summary>
        /// Function pointer to open a new file.
        /// </summary>
        public IntPtr OpenProc;

        /// <summary>
        /// Function pointer used to close an existing file.
        /// </summary>
        public IntPtr CloseProc;

        /// <summary>
        /// Char*, user defined opaque data.
        /// </summary>
        public IntPtr UserData;
    }

    /// <summary>
    /// Contains callbacks to read and write to a file opened by a custom file system.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiFile
    {
        /// <summary>
        /// Function pointer to read from a file.
        /// </summary>
        public IntPtr ReadProc;

        /// <summary>
        /// Function pointer to write to a file.
        /// </summary>
        public IntPtr WriteProc;

        /// <summary>
        /// Function pointer to retrieve the current position of the file cursor.
        /// </summary>
        public IntPtr TellProc;

        /// <summary>
        /// Function pointer to retrieve the size of the file.
        /// </summary>
        public IntPtr FileSizeProc;

        /// <summary>
        /// Function pointer to set the current position of the file cursor.
        /// </summary>
        public IntPtr SeekProc;

        /// <summary>
        /// Function pointer to flush the file contents.
        /// </summary>
        public IntPtr FlushProc;

        /// <summary>
        /// Char*, user defined opaque data.
        /// </summary>
        public IntPtr UserData;
    }

    #region Delegates

    /// <summary>
    /// Callback delegate for Assimp's LogStream.
    /// </summary>
    /// <param name="msg">Log message</param>
    /// <param name="userData">char* pointer to user data that is passed to the callback</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [CLSCompliant(false)]
    public delegate void AiLogStreamCallback([In, MarshalAs(UnmanagedType.LPStr)] String msg, IntPtr userData);

    /// <summary>
    /// Callback delegate for a custom file system, to write to a file.
    /// </summary>
    /// <param name="file">Pointer to an AiFile instance</param>
    /// <param name="dataToWrite">Char* pointer to data to write (casted from a void*)</param>
    /// <param name="sizeOfElemInBytes">Size of a single element in bytes to write</param>
    /// <param name="numElements">Number of elements to write</param>
    /// <returns>Number of elements successfully written. Should be zero if either size or numElements is zero. May be less than numElements if an error occured.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [CLSCompliant(false)]
    public delegate UIntPtr AiFileWriteProc(IntPtr file, IntPtr dataToWrite, UIntPtr sizeOfElemInBytes, UIntPtr numElements);

    /// <summary>
    /// Callback delegate for a custom file system, to read from a file.
    /// </summary>
    /// <param name="file">Pointer to an AiFile instance.</param>
    /// <param name="dataToRead">Char* pointer that will store the data read (casted from a void*)</param>
    /// <param name="sizeOfElemInBytes">Size of a single element in bytes to read</param>
    /// <param name="numElements">Number of elements to read</param>
    /// <returns>Number of elements succesfully read. Should be zero if either size or numElements is zero. May be less than numElements if end of file is encountered, or if an error occured.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [CLSCompliant(false)]
    public delegate UIntPtr AiFileReadProc(IntPtr file, IntPtr dataToRead, UIntPtr sizeOfElemInBytes, UIntPtr numElements);

    /// <summary>
    /// Callback delegate for a custom file system, to tell offset/size information about the file.
    /// </summary>
    /// <param name="file">Pointer to an AiFile instance.</param>
    /// <returns>Returns the current file cursor or the file size in bytes. May be -1 if an error has occured.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [CLSCompliant(false)]
    public delegate UIntPtr AiFileTellProc(IntPtr file);

    /// <summary>
    /// Callback delegate for a custom file system, to flush the contents of the file to the disk.
    /// </summary>
    /// <param name="file">Pointer to an AiFile instance.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [CLSCompliant(false)]
    public delegate void AiFileFlushProc(IntPtr file);

    /// <summary>
    /// Callback delegate for a custom file system, to set the current position of the file cursor.
    /// </summary>
    /// <param name="file">Pointer to An AiFile instance.</param>
    /// <param name="offset">Offset from the origin.</param>
    /// <param name="seekOrigin">Position used as a reference</param>
    /// <returns>Returns success, if successful</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [CLSCompliant(false)]
    public delegate ReturnCode AiFileSeek(IntPtr file, UIntPtr offset, Origin seekOrigin);

    /// <summary>
    /// Callback delegate for a custom file system, to open a given file and create a new AiFile instance.
    /// </summary>
    /// <param name="fileIO">Pointer to an AiFileIO instance.</param>
    /// <param name="pathToFile">Path to the target file</param>
    /// <param name="mode">Read-write permissions to request</param>
    /// <returns>Pointer to an AiFile instance.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [CLSCompliant(false)]
    public delegate IntPtr AiFileOpenProc(IntPtr fileIO, [In, MarshalAs(UnmanagedType.LPStr)] String pathToFile, [In, MarshalAs(UnmanagedType.LPStr)] String mode);

    /// <summary>
    /// Callback delegate for a custom file system, to close a given file and free its memory.
    /// </summary>
    /// <param name="fileIO">Pointer to an AiFileIO instance.</param>
    /// <param name="file">Pointer to an AiFile instance that will be closed.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [CLSCompliant(false)]
    public delegate void AiFileCloseProc(IntPtr fileIO, IntPtr file);


    #endregion

    #region Collections

    /// <summary>
    /// Fixed length array for representing the color channels of a mesh. Length is equal
    /// to <see cref="AiDefines.AI_MAX_NUMBER_OF_COLOR_SETS"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public unsafe struct AiMeshColorArray
    {
        //No fixed size intptrs
        private IntPtr m_ptr0, m_ptr1, m_ptr2, m_ptr3, m_ptr4, m_ptr5, m_ptr6, m_ptr7;

        /// <summary>
        /// Gets the length of the array.
        /// </summary>
        public int Length
        {
            get
            {
                return AiDefines.AI_MAX_NUMBER_OF_COLOR_SETS;
            }
        }

        /// <summary>
        /// Gets or sets an array value at the specified index.
        /// </summary>
        /// <param name="index">Zero-based index.</param>
        public IntPtr this[int index]
        {
            get
            {
                switch(index)
                {
                    case 0:
                        return m_ptr0;
                    case 1:
                        return m_ptr1;
                    case 2:
                        return m_ptr2;
                    case 3:
                        return m_ptr3;
                    case 4:
                        return m_ptr4;
                    case 5:
                        return m_ptr5;
                    case 6:
                        return m_ptr6;
                    case 7:
                        return m_ptr7;
                    default:
                        return IntPtr.Zero;
                }
            }
            set
            {
                switch(index)
                {
                    case 0:
                        m_ptr0 = value;
                        break;
                    case 1:
                        m_ptr1 = value;
                        break;
                    case 2:
                        m_ptr2 = value;
                        break;
                    case 3:
                        m_ptr3 = value;
                        break;
                    case 4:
                        m_ptr4 = value;
                        break;
                    case 5:
                        m_ptr5 = value;
                        break;
                    case 6:
                        m_ptr6 = value;
                        break;
                    case 7:
                        m_ptr7 = value;
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Fixed length array for representing the texture coordinate channels of a mesh. Length is equal
    /// to <see cref="AiDefines.AI_MAX_NUMBER_OF_TEXTURECOORDS"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public unsafe struct AiMeshTextureCoordinateArray
    {
        //No fixed size intptrs
        private IntPtr m_ptr0, m_ptr1, m_ptr2, m_ptr3, m_ptr4, m_ptr5, m_ptr6, m_ptr7;

        /// <summary>
        /// Gets the length of the array.
        /// </summary>
        public int Length
        {
            get
            {
                return AiDefines.AI_MAX_NUMBER_OF_TEXTURECOORDS;
            }
        }

        /// <summary>
        /// Gets or sets an array value at the specified index.
        /// </summary>
        /// <param name="index">Zero-based index.</param>
        public IntPtr this[int index]
        {
            get
            {
                switch(index)
                {
                    case 0:
                        return m_ptr0;
                    case 1:
                        return m_ptr1;
                    case 2:
                        return m_ptr2;
                    case 3:
                        return m_ptr3;
                    case 4:
                        return m_ptr4;
                    case 5:
                        return m_ptr5;
                    case 6:
                        return m_ptr6;
                    case 7:
                        return m_ptr7;
                    default:
                        return IntPtr.Zero;
                }
            }
            set
            {
                switch(index)
                {
                    case 0:
                        m_ptr0 = value;
                        break;
                    case 1:
                        m_ptr1 = value;
                        break;
                    case 2:
                        m_ptr2 = value;
                        break;
                    case 3:
                        m_ptr3 = value;
                        break;
                    case 4:
                        m_ptr4 = value;
                        break;
                    case 5:
                        m_ptr5 = value;
                        break;
                    case 6:
                        m_ptr6 = value;
                        break;
                    case 7:
                        m_ptr7 = value;
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Fixed length array for representing the number of UV components for each texture coordinate channel of a mesh. Length is equal
    /// to <see cref="AiDefines.AI_MAX_NUMBER_OF_TEXTURECOORDS"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [CLSCompliant(false)]
    public struct AiMeshUVComponentArray
    {
        //Could use fixed size array here, but have an inkling that constantly fixing for each indexer operation might be burdensome
        private uint m_uvw0, m_uvw1, m_uvw2, m_uvw3, m_uvw4, m_uvw5, m_uvw6, m_uvw7;

        /// <summary>
        /// Gets the length of the array.
        /// </summary>
        public int Length
        {
            get
            {
                return AiDefines.AI_MAX_NUMBER_OF_TEXTURECOORDS;
            }
        }

        /// <summary>
        /// Gets or sets an array value at the specified index.
        /// </summary>
        /// <param name="index">Zero-based index.</param>
        public uint this[int index]
        {
            get
            {
                switch(index)
                {
                    case 0:
                        return m_uvw0;
                    case 1:
                        return m_uvw1;
                    case 2:
                        return m_uvw2;
                    case 3:
                        return m_uvw3;
                    case 4:
                        return m_uvw4;
                    case 5:
                        return m_uvw5;
                    case 6:
                        return m_uvw6;
                    case 7:
                        return m_uvw7;
                    default:
                        return 0;
                }
            }
            set
            {
                switch(index)
                {
                    case 0:
                        m_uvw0 = value;
                        break;
                    case 1:
                        m_uvw1 = value;
                        break;
                    case 2:
                        m_uvw2 = value;
                        break;
                    case 3:
                        m_uvw3 = value;
                        break;
                    case 4:
                        m_uvw4 = value;
                        break;
                    case 5:
                        m_uvw5 = value;
                        break;
                    case 6:
                        m_uvw6 = value;
                        break;
                    case 7:
                        m_uvw7 = value;
                        break;
                }
            }
        }
    }

    #endregion
}
