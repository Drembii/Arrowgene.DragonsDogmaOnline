using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arrowgene.Ddon.Shared.Model
{
    public enum QuestState : uint
    {
        Unknown = 0,
        InProgress = 1,
        Cleared = 2,
        Failed = 3,
    }
}
