using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace GonzoNet.Encryption
{
    public class ARC4DecryptionArgs
    {
        public ICryptoTransform Transformer;
    }

    public class AESDecryptionArgs
    {
        public byte[] Key;
        public byte[] IV;
    }

    public class DecryptionArgsContainer
    {
        public ushort UnencryptedLength;

        public ARC4DecryptionArgs ARC4DecryptArgs;
        public AESDecryptionArgs AESDecryptArgs;
    }
}
