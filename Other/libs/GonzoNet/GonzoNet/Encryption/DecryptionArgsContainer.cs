using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace GonzoNet.Encryption
{
    public class ARC4DecryptionArgs
    {
        public byte[] EncryptionKey;
        public ICryptoTransform Transformer;
    }

    public class DecryptionArgsContainer
    {
        public ushort UnencryptedLength;

        public ARC4DecryptionArgs ARC4DecryptArgs;
    }
}
