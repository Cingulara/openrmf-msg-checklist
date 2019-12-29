// Copyright (c) Cingulara LLC 2019 and Tutela LLC 2019. All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007 license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using NATS.Client;
using System.Text;
using NLog;
using NLog.Config;
using openrmf_msg_checklist.Models;
using openrmf_msg_checklist.Data;
using openrmf_msg_checklist.Classes;
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
            
            // Create a new connection factory to create a connection.
            ConnectionFactory cf = new ConnectionFactory();
            // add the options for the server, reconnecting, and the handler events
            Options opts = ConnectionFactory.GetDefaultOptions();
            opts.MaxReconnect = -1;
            opts.ReconnectWait = 1000;
            opts.Name = "openrmf-msg-checklist";
            opts.Url = Environment.GetEnvironmentVariable("NATSSERVERURL");
            opts.AsyncErrorEventHandler += (sender, events) =>
            {
                logger.Info("NATS client error. Server: {0}. Message: {1}. Subject: {2}", events.Conn.ConnectedUrl, events.Error, events.Subscription.Subject);
            };

            opts.ServerDiscoveredEventHandler += (sender, events) =>
            {
                logger.Info("A new server has joined the cluster: {0}", events.Conn.DiscoveredServers);
            };

            opts.ClosedEventHandler += (sender, events) =>
            {
                logger.Info("Connection Closed: {0}", events.Conn.ConnectedUrl);
            };

            opts.ReconnectedEventHandler += (sender, events) =>
            {
                logger.Info("Connection Reconnected: {0}", events.Conn.ConnectedUrl);
            };

            opts.DisconnectedEventHandler += (sender, events) =>
            {
                logger.Info("Connection Disconnected: {0}", events.Conn.ConnectedUrl);
            };
            
            // Creates a live connection to the NATS Server with the above options
            IConnection c = cf.CreateConnection(opts);

            // send back a checklist based on an individual ID
            EventHandler<MsgHandlerEventArgs> readChecklist = (sender, natsargs) =>
            {
                try {
                    // print the message
                    logger.Info("New NATS subject: {0}", natsargs.Message.Subject);
                    logger.Info("New NATS data: {0}",Encoding.UTF8.GetString(natsargs.Message.Data));
                    
                    Artifact art = new Artifact();
                    // setup the MongoDB connection
                    Settings s = new Settings();
                    s.ConnectionString = Environment.GetEnvironmentVariable("MONGODBCONNECTION");
                    s.Database = Environment.GetEnvironmentVariable("MONGODB");
                    // setup the MongoDB repo connection
                    ArtifactRepository _artifactRepo = new ArtifactRepository(s);
                    art = _artifactRepo.GetArtifact(Encoding.UTF8.GetString(natsargs.Message.Data)).Result;
                    // when you serialize the \\ slash JSON chokes, so replace and regular \\ with 4 \\\\
                    art.rawChecklist = art.rawChecklist.Replace("\\","\\\\");
                    // now serialize the class into a string to compress and send
                    string msg = JsonConvert.SerializeObject(art);
                    // publish back out on the reply line to the calling publisher
                    logger.Info("Sending back compressed Checklist Data");
                    c.Publish(natsargs.Message.Reply, Encoding.UTF8.GetBytes(Compression.CompressString(msg)));
                    c.Flush(); // flush the line
                }
                catch (Exception ex) {
                    // log it here
                    logger.Error(ex, "Error retrieving checklist record for artifactId {0}", Encoding.UTF8.GetString(natsargs.Message.Data));
                }
            };

            // send back a checklist listing based on the system ID
            EventHandler<MsgHandlerEventArgs> readSystemChecklists = (sender, natsargs) =>
            {
                try {
                    // print the message
                    logger.Info("NATS Msg Checklists: {0}", natsargs.Message.Subject);
                    logger.Info("NATS Msg system data: {0}",Encoding.UTF8.GetString(natsargs.Message.Data));
                    
                    IEnumerable<Artifact> arts;
                    // setup the MondoDB connection
                    Settings s = new Settings();
                    s.ConnectionString = Environment.GetEnvironmentVariable("MONGODBCONNECTION");
                    s.Database = Environment.GetEnvironmentVariable("MONGODB");
                    // setup the database repo
                    ArtifactRepository _artifactRepo = new ArtifactRepository(s);
                    arts = _artifactRepo.GetSystemArtifacts(Encoding.UTF8.GetString(natsargs.Message.Data)).Result;
                    // remove the rawChecklist as we do not need all that data, just the main metadata for listing
                    foreach(Artifact a in arts) {
                        a.rawChecklist = "";
                    }
                    // now publish it back out w/ the reply subject
                    string msg = JsonConvert.SerializeObject(arts);
                    // publish back out on the reply line to the calling publisher
                    logger.Info("Sending back compressed Checklist Data");
                    c.Publish(natsargs.Message.Reply, Encoding.UTF8.GetBytes(Compression.CompressString(msg)));
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
            logger.Info("setting up the openRMF system checklist listing subscription");
            IAsyncSubscription asyncNewSystemChecklists = c.SubscribeAsync("openrmf.system.checklists.read", readSystemChecklists);
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
