using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.Logging;
using SuperSocket.WebSocket;
using POGOProtos.Data;

namespace NurxPlugin.SenderResponders
{
    class ProfileSenderResponder : INurxMessageSender, INurxMessageResponder
    {
        // Private vars.
        private NurxService _service;
        private Session _pogoSession;
        private PlayerData _currentData;


        // Public properties.
        public string Command { get { return "profile"; } }


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
        /// Handle received messages, send profile data to requester.
        /// </summary>
        /// <param name="command">Nurx command instance.</param>
        /// <param name="wsSession">Requesting websockets session.</param>
        public void MessageReceived(NurxCommand command, WebSocketSession wsSession)
        {
            if (_currentData == null)
                _currentData = _pogoSession.Profile.PlayerData;

            _service.Send(wsSession, "profile", _currentData);
        }


        /// <summary>
        /// Handle updates in profile information.
        /// </summary>
        /// <param name="evt">Event instance.</param>
        private void EventDispatcher_EventReceived(IEvent evt)
        {
            if(evt is ProfileEvent)
            {
                Logger.Write("Sending profile data to websockets clients.", LogLevel.Debug);

                ProfileEvent pEvt = (ProfileEvent)evt;
                _currentData = pEvt.Profile.PlayerData.Clone();
                _service.Broadcast("profile", _currentData);
            }
        }
    }
}
