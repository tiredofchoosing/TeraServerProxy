﻿using NLog;
using TeraCore.Game;
using TeraCore.Game.Messages;
using TeraServerProxy.Structures;

namespace TeraServerProxy.MessageProcessor
{
    internal class CUnregisterPartyInfoProcessor : TeraMessageProcessor
    {
        public CUnregisterPartyInfoProcessor(ParsedMessage message, Client client, TeraDataPools dataPools, NLog.ILogger logger)
            : base(message, client, dataPools, logger) { }

        public override void Process()
        {
            if (Message is CUnregisterPartyInfoMessage)
            {
                var player = Client.CurrentPlayer;
                var party = DataPools.GetPartyByPlayer(player);
                var partyInfo = DataPools.GetPartyInfoByParty(party);
                if (partyInfo != null)
                {
                    DataPools.Remove(partyInfo);
                    Logger.Debug($"{Client}|Removed PartyInfo: {partyInfo}.");
                }
                else
                    Logger.Warn($"{Client}|No PartyInfo to remove.");
            }
        }
    }
}