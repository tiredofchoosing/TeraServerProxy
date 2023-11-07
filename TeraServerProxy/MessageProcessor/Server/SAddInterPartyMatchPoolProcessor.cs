﻿using NLog;
using System.ComponentModel;
using TeraCore.Game;
using TeraCore.Game.Messages;
using TeraCore.Game.Structures;
using TeraServerProxy.Structures;

namespace TeraServerProxy.MessageProcessor
{
    internal class SAddInterPartyMatchPoolProcessor : TeraMessageProcessor
    {
        public SAddInterPartyMatchPoolProcessor(ParsedMessage message, Client client, TeraDataPools dataPools, NLog.ILogger logger)
            : base(message, client, dataPools, logger) { }

        public override void Process()
        {
            if (Message is SAddInterPartyMatchPoolMessage m)
            {
                var player = Client.CurrentPlayer;

                if (m.Profiles.First().Name != player.Name)
                    return;

                foreach (var profile in m.Profiles)
                {
                    profile.LinkedPlayer = DataPools.GetPlayerByName(profile.Name);
                }

                var partyMatching = new PartyMatching(m.Profiles, m.Instances, m.MatchingType);
                DataPools.Add(partyMatching);
                Logger.Debug($"{Client}|Added PartyMatching: {partyMatching}.");
            }
        }
    }
}