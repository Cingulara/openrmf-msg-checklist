using System;
using System.IO;
using System.IO.Compression;
using NATS.Client;
using System.Text;
using NLog;
using NLog.Config;
using openrmf_msg_checklist.Models;
using openrmf_msg_checklist.Data;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace openrmf_msg_checklist
{
    class Program
    {
        static void Main(string[] args)
        {
            LogManager.Configuration = new XmlLoggingConfiguration($"{AppContext.BaseDirectory}nlog.config");

            var logger = LogManager.GetLogger("openrmf_msg_checklist");
            //logger.Info("log info");
            //logger.Debug("log debug");

            // Create a new connection factory to create a connection.
            ConnectionFactory cf = new ConnectionFactory();

            // Creates a live connection to the default NATS Server running locally
            IConnection c = cf.CreateConnection(Environment.GetEnvironmentVariable("natsserverurl"));

            // Setup an event handler to process incoming messages.
            // An anonymous delegate function is used for brevity.
            EventHandler<MsgHandlerEventArgs> readChecklist = (sender, natsargs) =>
            {
                try {
                    // print the message
                    logger.Info("New NATS subject: {0}", natsargs.Message.Subject);
                    logger.Info("New NATS data: {0}",Encoding.UTF8.GetString(natsargs.Message.Data));
                    
                    Artifact art = new Artifact();
                    Settings s = new Settings();
                    s.ConnectionString = Environment.GetEnvironmentVariable("mongoConnection");
                    s.Database = Environment.GetEnvironmentVariable("mongodb");
                    ArtifactRepository _artifactRepo = new ArtifactRepository(s);
                    art = _artifactRepo.GetArtifact(Encoding.UTF8.GetString(natsargs.Message.Data)).Result;
                    // now publish it back out w/ the reply subject
                    string msg = JsonConvert.SerializeObject(art).Replace("\\t","");
                    // publish back out on the reply line to the calling publisher
                    logger.Info("Sending back compressed Checklist Data");
                    c.Publish(natsargs.Message.Reply, Encoding.UTF8.GetBytes(CompressString(msg)));
                    c.Flush(); // flush the line
                }
                catch (Exception ex) {
                    // log it here
                    logger.Error(ex, "Error retrieving checklist record for artifactId {0}", Encoding.UTF8.GetString(natsargs.Message.Data));
                }
            };

            // The simple way to create an asynchronous subscriber
            // is to simply pass the event in.  Messages will start
            // arriving immediately.
            logger.Info("setting up the openRMF checklist subscription");
            IAsyncSubscription asyncNew = c.SubscribeAsync("openrmf.checklist.read", readChecklist);
        }
        /// <summary>
        /// Compresses the string.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static string CompressString(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;

            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            var gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return Convert.ToBase64String(gZipBuffer);
        }

        private static ObjectId GetInternalId(string id)
        {
            ObjectId internalId;
            if (!ObjectId.TryParse(id, out internalId))
                internalId = ObjectId.Empty;
            return internalId;
        }
    }
}
