using FSO.Content.Framework;
using FSO.Vitaboy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for cfp files (*.cfp).
    /// </summary>
    public class CFPCodec : IContentCodec<CFP>
    {
        #region IContentCodec<CFP> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var result = new CFP();
            result.Read(stream);
            return result;
        }

        #endregion
    }
}
