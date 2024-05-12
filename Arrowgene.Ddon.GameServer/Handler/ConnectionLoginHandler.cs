using System;
using System.Collections.Generic;
using Arrowgene.Ddon.Database.Model;
using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class ConnectionLoginHandler : GameStructurePacketHandler<C2SConnectionLoginReq>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(ConnectionLoginHandler));

        private OrbUnlockManager _OrbUnlockManager;
        private CharacterManager _CharacterManager;

        public ConnectionLoginHandler(DdonGameServer server) : base(server)
        {
            _OrbUnlockManager = server.OrbUnlockManager;
            _CharacterManager = server.CharacterManager;
        }

        public override void Handle(GameClient client, StructurePacket<C2SConnectionLoginReq> packet)
        {
            client.SetChallengeCompleted(true);

            Logger.Debug(client,
                $"Received SessionKey:{packet.Structure.SessionKey} for platform:{packet.Structure.PlatformType}");

            S2CConnectionLoginRes res = new S2CConnectionLoginRes();
            GameToken token = Database.SelectToken(packet.Structure.SessionKey);
            if (token == null)
            {
                Logger.Error(client, $"SessionKey:{packet.Structure.SessionKey} not found");
                res.Error = 1;
                client.Send(res);
                return;
            }

            if (!Database.DeleteTokenByAccountId(token.AccountId))
            {
                Logger.Error(client, $"Failed to delete session key from DB:{packet.Structure.SessionKey}");
            }


            Account account = Database.SelectAccountById(token.AccountId);
            if (account == null)
            {
                Logger.Error(client, $"AccountId:{token.AccountId} not found");
                res.Error = 1;
                client.Send(res);
                return;
            }

            DateTime now = DateTime.UtcNow;

            List<Connection> connections = Database.SelectConnectionsByAccountId(account.Id);
            if (connections.Count > 0)
            {
                foreach (Connection con in connections)
                {
                    if (con.Type == ConnectionType.GameServer)
                    {
                        Logger.Error(client, $"game server connection already exists");
                        res.Error = 1;
                        client.Send(res);
                        return;
                    }
                }
            }

            // Order Important,
            // account need to be only assigned after
            // verification that no connection exists, and before
            // registering the connection
            client.Account = account;

            Connection connection = new Connection();
            connection.ServerId = Server.Id;
            connection.AccountId = account.Id;
            connection.Type = ConnectionType.GameServer;
            connection.Created = now;
            if (!Database.InsertConnection(connection))
            {
                Logger.Error(client, $"Failed to register game connection");
                res.Error = 1;
                client.Send(res);
                return;
            }

            Character character = _CharacterManager.SelectCharacter(client, token.CharacterId);
            if (character == null)
            {
                Logger.Error(client, $"CharacterId:{token.CharacterId} not found");
                res.Error = 1;
                client.Send(res);
                return;
            }

            Logger.Info(client, "Logged Into GameServer");

            // update login token for client
            // client.Account.LoginToken = GameToken.GenerateLoginToken();
            client.Account.LoginTokenCreated = now;
            if (!Database.UpdateAccount(client.Account))
            {
                Logger.Error(client, "Failed to update OneTimeToken");
                res.Error = 1;
                client.Send(res);
                return;
            }


            Logger.Debug(client, $"Updated OneTimeToken:{client.Account.LoginToken}");

            res.OneTimeToken = client.Account.LoginToken;
            client.Send(res);
        }
    }
}
