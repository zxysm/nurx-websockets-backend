using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.State;
using SuperSocket.WebSocket;

namespace NurxPlugin.SenderResponders
{
    class LocationSenderResponder : INurxMessageSender, INurxMessageResponder
    {
        // Private vars.
        private NurxService _service;
        private Session _pogoSession;
        private double _lat;
        private double _lng;


        // Public properties.
        public string Command { get { return "location"; } }


        /// <summary>
        /// Register the sender with Nurx.
        /// </summary>
        /// <param name="pogoSession">NecroBot session instnace.</param>
        /// <param name="service">Nurx service instance.</param>
        public void RegisterSender(Session pogoSession, NurxService service)
        {
            _service= service;
            _pogoSession = pogoSession;
            pogoSession.Navigation.UpdatePositionEvent += Navigation_UpdatePositionEvent;
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
        /// Handle received messages, send location to requester.
        /// </summary>
        /// <param name="command">Nurx command instance.</param>
        /// <param name="wsSession">Requesting websockets session.</param>
        public void MessageReceived(NurxCommand command, WebSocketSession wsSession)
        {
            if (_lat == 0 && _lng == 0)
                return;

            var response = new
            {
                Lat = _lat,
                Lng = _lng
            };
            _service.Send(wsSession, "update_location", response);
        }


        /// <summary>
        /// Handle position updates, broadcast them to all session.
        /// </summary>
        /// <param name="lat">Current location latitude.</param>
        /// <param name="lng">Current location longitude.</param>
        private void Navigation_UpdatePositionEvent(double lat, double lng)
        {
            _lat = lat;
            _lng = lng;

            var response = new
            {
                Lat = _lat,
                Lng = _lng
            };
            _service.Broadcast("update_location", response);
        }
    }
}
