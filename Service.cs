using static Io.Platform6.Imdg.Cm.CommonMessage.Types;

using Io.Platform6.Imdg.Cm;
using Hazelcast;
using P6Connector.Models;
using System.Text.Json;
using Hazelcast.Client;
using Hazelcast.Config;
using Hazelcast.Core;
using Hazelcast.Logging;
using Microsoft.Extensions.Logging;

using ILogger = Microsoft.Extensions.Logging.ILogger;
using System.Threading;

namespace P6Connector
{
    public class Service
    {

        private readonly ILogger logger;
        private readonly string id;
        private readonly string idKey;
        private readonly Thread listenerThread;

        public IHazelcastInstance? Client;

        private string nodeId;

        public Service(DeployParameters parameters, ILogger logger, Func<CommonMessage, CommonMessage> onMessage)
        {

            id = parameters.Id;
            idKey = Constants.SenderIdPrefix + parameters.Id;
            nodeId = "unknown";
            this.logger = logger;

            DeployService(parameters);
            listenerThread = StartListener(onMessage);
        }

        public void Release()
        {
            UndeployService();
            listenerThread.Interrupt();

        }

        private void ListenerThreadProc(Object objOnMessage)
        {
            var onMessage = (Func<CommonMessage, CommonMessage>)objOnMessage;

            var queueName = "cmb." + id;
            var receiveQueue = Client.GetQueue<CommonMessage>(queueName);

            try
            {
                while (true)
                {
                    logger.LogInformation("Waiting for common message on: " + queueName + " ...");
                    var request = Task.Run(() => receiveQueue.Take()).Result;
                    logger.LogInformation("Got request message: " + request);

                    var response = onMessage(request);

                    logger.LogInformation("Sending response message: " + response);
                    var responseQueue = Client.GetQueue<CommonMessage>(request.ReplyTo);
                    var isSent = Task.Run(() => responseQueue.Offer(response)).Result;
                    if (!isSent)
                    {
                        logger.LogError("Failed sending CMB response to: " + responseQueue);
                        throw new Exception("Unable to send the common message with id " + response.Id + "!");
                    }

                }
            }
            catch (ThreadInterruptedException)
            {
                logger.LogInformation("Listner Thread Terminated.");
            }
        }

        private Thread StartListener(Func<CommonMessage, CommonMessage> onMessage)
        {
            var lThread = new Thread(ListenerThreadProc);
            lThread.Start(onMessage);
            return lThread;
        }

        private void CreateHazelcastClient()
        {
            Environment.SetEnvironmentVariable("hazelcast.logging.level", "info");
            Environment.SetEnvironmentVariable("hazelcast.logging.type", "console");

            var config = new ClientConfig();
            var hostname = Environment.GetEnvironmentVariable("HOSTNAME") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("PORT") != null ? Convert.ToUInt32(Environment.GetEnvironmentVariable("PORT"), 10) : 5900;

            config.GetSerializationConfig()
                .AddSerializerConfig(new SerializerConfig()
                    .SetImplementation(new Serializers.MessageSerializer())
                    .SetTypeClass(typeof(CommonMessage)));

            config.GetNetworkConfig().AddAddress(hostname + ":" + port);

            Client = Task.Run(() => HazelcastClient.NewHazelcastClient(config)).Result;
            nodeId = Client.GetLocalEndpoint().GetUuid();

            logger.LogInformation("CMB Client Created: " + nodeId);
        }

        private void DeployService(DeployParameters parameters)
        {

            if (Client == null) CreateHazelcastClient();

            var commonMessage = CallService(new CallServiceParameters
            {
                ReceiverId = Constants.ServiceManagerId,
                Action = Constants.ActionDeploy,
                Headers = new List<Header> {
                    BusConnection.CreateHeader(Constants.Platform6AppKey + "node.id", nodeId),
                    BusConnection.CreateHeader(Constants.Platform6AppKey + "service.id", id),
                    BusConnection.CreateHeader(Constants.Platform6AppKey + "service.path", parameters.Path),
                    BusConnection.CreateHeader(Constants.Platform6AppKey + "service.ctx", parameters.BasePath),
                    BusConnection.CreateHeader(Constants.Platform6AppKey + "service.version", parameters.Versions.Server),
                    BusConnection.CreateHeader(Constants.Platform6AppKey + "service.ui.version", parameters.Versions.Client),
                    BusConnection.CreateHeader(Constants.Platform6AppKey + "service.ui", JsonSerializer.Serialize(parameters.Ui))
                }
            });

            logger.LogInformation("CMB Response: " + commonMessage);

        }

        private void UndeployService()
        {

            CallService(new CallServiceParameters
            {
                ReceiverId = Constants.ServiceManagerId,
                Action = Constants.ActionUnDeploy,
                Headers = new List<Header> {
                    BusConnection.CreateHeader(Constants.Platform6AppKey + "node.id", nodeId),
                    BusConnection.CreateHeader(Constants.Platform6AppKey + "service.id", id)
                }
            });
        }

        public CommonMessage CallService(CallServiceParameters parameters)
        {

            logger.LogInformation("CMB Call: " + parameters.ReceiverId);

            var receiverId = parameters.ReceiverId;
            var headers = new List<Header>();
            var attachments = parameters.Attachments ?? new List<Attachment>();

            if (parameters.Username != null) headers.Add(BusConnection.CreateHeader(Constants.RequestPrefix + "user", parameters.Username));
            if (parameters.Action != null) headers.Add(BusConnection.CreateHeader(Constants.RequestPrefix + "action", parameters.Action));
            if (parameters.Headers != null) headers.AddRange(parameters.Headers);

            var commonMessage = Task.Run(() => BusConnection.CreateCommonMessage(idKey, parameters.ReceiverId, headers, attachments)).Result;

            return SendCommonMessage(receiverId, commonMessage);
        }

        public CommonMessage SendCommonMessage(string receiverId, CommonMessage commonMessage)
        {

            if (null == Client)
                throw new Exception("Hazelcast client is null!");

            logger.LogInformation("CMB Send.  CommonMessage: " + commonMessage);

            var receiverIdKey = Constants.ReceiverIdPrefix + receiverId;
            var request = Client.GetQueue<CommonMessage>(receiverIdKey);
            var isSent = Task.Run(() => request.Offer(commonMessage)).Result;

            if (!isSent)
            {
                logger.LogError("Failed sending CMB to: " + receiverId);
                throw new Exception("Unable to send the common message with id " + commonMessage.Id + "!");
            }

            var response = Task.Run(() => Client.GetQueue<CommonMessage>(idKey).Take()).Result;

            if (!response.Id.Equals(commonMessage.Id))
                throw new Exception("Common message response's id " + response.Id + " is not the same as the common message request's id " + commonMessage.Id + "!");

            return response;
        }
    }
}
