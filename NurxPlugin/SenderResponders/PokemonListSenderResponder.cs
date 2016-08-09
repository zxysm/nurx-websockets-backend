using System;
using System.Collections.Generic;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Event;
using POGOProtos.Data;
using PoGo.NecroBot.Logic.Logging;
using SuperSocket.WebSocket;

namespace NurxPlugin.SenderResponders
{
    public class NurxPokemonData
    {
        public NurxPokemonData(PokemonData data)
        {
            Perfection = PoGo.NecroBot.Logic.PoGoUtils.PokemonInfo.CalculatePokemonPerfection(data);
            Base = data;
        }

        public PokemonData Base { get; set; }
        public double Perfection { get; set; }
    }

    class PokemonListSenderResponder : INurxMessageSender, INurxMessageResponder
    {
        // Private vars.
        private Session _pogoSession;
        private NurxService _service;
        private List<NurxPokemonData> _currentList = new List<NurxPokemonData>();
        public Object _lck = new Object();


        // Public properties.
        public string Command { get { return "pokemonlist"; } }


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
        /// Handle received messages, send pokemon list to requester.
        /// </summary>
        /// <param name="command">Nurx command instance.</param>
        /// <param name="wsSession">Requesting websockets session.</param>
        public async void MessageReceived(NurxCommand command, WebSocketSession wsSession)
        {
            
                if (_currentList.Count == 0)
                {
                    var poke = await _pogoSession.Inventory.GetPokemons();

                    lock (_lck)
                    {
                        _currentList = new List<NurxPokemonData>();
                        foreach (var p in poke) { _currentList.Add(new NurxPokemonData(p.Clone())); }
                    }
                }

            _service.Send(wsSession, "pokemonlist", _currentList);
        }


        /// <summary>
        /// Handle updates in pokemon list.
        /// </summary>
        /// <param name="evt">Event instance.</param>
        private void EventDispatcher_EventReceived(IEvent evt)
        {
            if (evt is PokemonListEvent)
            {
                Logger.Write("Sending pokemon list data to websockets clients.", LogLevel.Info);
                PokemonListEvent pEvt = (PokemonListEvent)evt;

                lock (_lck)
                {
                    _currentList = new List<NurxPokemonData>();
                    pEvt.PokemonList.ForEach(o => _currentList.Add(new NurxPokemonData(o.Item1.Clone())));
                }

                _service.Broadcast("pokemonlist", _currentList);
            }
        }
    }
}
