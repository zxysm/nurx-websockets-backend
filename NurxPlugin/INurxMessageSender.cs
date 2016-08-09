using PoGo.NecroBot.Logic.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NurxPlugin
{
    interface INurxMessageSender
    {
        void RegisterSender(Session pogoSession, NurxService service);
    }
}
