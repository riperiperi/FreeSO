using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FSO.Server.Api.Core.Models
{
    public class FSOUpdateManifest {
        public string Version;
        public List<FileDiff> Diffs;
    }
}
