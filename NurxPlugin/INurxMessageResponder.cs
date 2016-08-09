using PoGo.NecroBot.Logic.State;
using SuperSocket.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NurxPlugin
{
    interface INurxMessageResponder
    {
        void RegisterResponder(Session pogoSession, NurxService service);
        void MessageReceived(NurxCommand command, WebSocketSession wsSession);
        string Command { get; }
    }
}
