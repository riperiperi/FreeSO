/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Code.Utils
{
    /// <summary>
    /// Utility so calculations are only performed once where appropriate
    /// </summary>
    public class MathCache
    {
        private Dictionary<string, object> m_Value = new Dictionary<string, object>();

        public void Invalidate()
        {
            m_Value.Clear();
        }

        public void Invalidate(string id)
        {
            m_Value.Remove(id);
        }

        public TResult Calculate<TResult>(string id, Func<object, TResult> calculator)
        {
            if (!m_Value.ContainsKey(id))
            {
                m_Value.Add(id, calculator.Invoke(null));
            }
            return (TResult)m_Value[id];
        }
    }
}
