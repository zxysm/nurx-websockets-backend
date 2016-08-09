using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.Logging;
using SuperSocket.WebSocket;
using POGOProtos.Map.Fort;

namespace NurxPlugin.SenderResponders
{
    class PokeStopSenderResponder : INurxMessageSender, INurxMessageResponder
    {
        // Private vars.
        private Session _pogoSession;
        private NurxService _service;
        private List<FortData> _currentList = new List<FortData>();


        // Public properties.
        public string Command { get { return "pokestops"; } }


        /// <summary>
        /// Register the sender with Nurx.
        /// </summary>
        /// <param name="pogoSession">NecroBot session instnace.</param>
        /// <param name="service">Nurx service instance.</param>
        public void RegisterSender(Session pogoSession, NurxService service)
        {
            _pogoSession = pogoSession;
            _service = service;

            _pogoSession.EventDispatcher.EventReceived += EventDispatcher_EventReceived;
        }


        /// <summary>
        /// Register the responder to the nurx service.
        /// </summary>
        /// <param name="pogoSession">NecroBot session instnace.</param>
        /// <param name="service">Nurx service instance.</param>
        public void RegisterResponder(Session pogoSession, NurxService service)
        {
            _pogoSession = pogoSession;
            _service = service;
            _service.RegisterResponder(Command, this);
        }


        /// <summary>
        /// Handle received messages, send pokestops data to requester.
        /// </summary>
        /// <param name="command">Nurx command instance.</param>
        /// <param name="wsSession">Requesting websockets session.</param>
        public void MessageReceived(NurxCommand command, WebSocketSession wsSession)
        {
            _service.Send(wsSession, "pokestops", _currentList);
        }


        /// <summary>
        /// Handle updates in pokestop list.
        /// </summary>
        /// <param name="evt">Event instance.</param>
        private void EventDispatcher_EventReceived(IEvent evt)
        {
            if (evt is PokeStopListEvent)
            {
                Logger.Write("Sending pokestop data to websockets clients.", LogLevel.Info);
                PokeStopListEvent pEvt = (PokeStopListEvent)evt;

                _currentList = new List<FortData>();
                pEvt.Forts.ForEach(f => _currentList.Add(f.Clone()));

                _service.Broadcast("pokestops", _currentList);
            }
        }
    }
}
