using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content.framework;
using Microsoft.Xna.Framework.Graphics;
using TSO.Content.codecs;
using System.Text.RegularExpressions;
using TSO.Vitaboy;

namespace TSO.Content
{
    /// <summary>
    /// Provides access to mesh (*.mesh) data in FAR3 archives.
    /// </summary>
    public class AvatarMeshProvider : FAR3Provider<Mesh>{
        public AvatarMeshProvider(Content contentManager, GraphicsDevice device) : base(contentManager, new MeshCodec(), new Regex(".*\\\\meshes\\\\.*\\.dat"))
        {
        }
    }
}
