using FSO.Common.DataService;
using FSO.Common.Serialization.Primitives;
using FSO.Server.DataService.Model;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Gluon;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Voltron;
using FSO.Server.Protocol.Voltron.Packets;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class DataServiceWrapperHandler
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDataService DataService;

        public DataServiceWrapperHandler(IDataService dataService)
        {
            this.DataService = dataService;
        }

        public async void Handle(IGluonSession session, DataServiceWrapperPDU packet)
        {
            try //data service throws exceptions (SecurityException, etc) when invalid requests are made. These should not crash the server...
            {
                if (packet.Body is cTSOTopicUpdateMessage)
                {
                    //Client wants to update a value in the data service
                    var update = packet.Body as cTSOTopicUpdateMessage;
                    DataService.ApplyUpdate(update, session);

                    List<uint> resultDotPath = new List<uint>();
                    foreach (var item in update.DotPath)
                    {
                        resultDotPath.Add(item);
                        if (item == packet.RequestTypeID)
                        {
                            break;
                        }
                    }

                    try
                    {
                        var result = await DataService.SerializePath(resultDotPath.ToArray());
                        if (result != null)
                        {
                            session.Write(new DataServiceWrapperPDU()
                            {
                                SendingAvatarID = packet.SendingAvatarID,
                                RequestTypeID = packet.RequestTypeID,
                                Body = result
                            });
                        }
                    } catch (Exception e)
                    {
                        //TODO
                        //keep this silent for now - bookmarks tend to spam errors.
                    }
                }
            }
            catch (Exception e)
            {
                LOG.Error(e, "Gluon DataService request failed!");
            }
        }

        /// <summary>
        /// The user is asking for some in RAM data
        /// </summary>
        /// <param name="session"></param>
        /// <param name="packet"></param>
        public async void Handle(IVoltronSession session, DataServiceWrapperPDU packet)
        {
            try //data service throws exceptions (SecurityException, etc) when invalid requests are made. These should not crash the server...
            {
                if (packet.Body is cTSONetMessageStandard)
                {

                    var msg = (cTSONetMessageStandard)packet.Body;
                    if (msg.ComplexParameter is cTSOTopicUpdateMessage)
                    {
                        var update = msg.ComplexParameter as cTSOTopicUpdateMessage;
                        DataService.ApplyUpdate(update, session);
                        return;
                    }
                    
                    var type = MaskedStructUtils.FromID(packet.RequestTypeID);

                    if (!msg.Parameter.HasValue)
                    {
                        return;
                    }

                    //Lookup the entity, then process the request and send the response
                    var task = DataService.Get(type, msg.Parameter.Value);
                    if (task != null)
                    {
                        var entity = await task;

                        var serialized = DataService.SerializeUpdate(type, entity, msg.Parameter.Value);
                        for (int i = 0; i < serialized.Count; i++)
                        {
                            object serial = serialized[i];
                            session.Write(new DataServiceWrapperPDU()
                            {
                                SendingAvatarID = packet.SendingAvatarID,
                                RequestTypeID = packet.RequestTypeID,
                                Body = serial
                            });
                        }
                    }
                }
                else if (packet.Body is cTSOTopicUpdateMessage)
                {
                    //Client wants to update a value in the data service
                    var update = packet.Body as cTSOTopicUpdateMessage;
                    DataService.ApplyUpdate(update, session);

                    List<uint> resultDotPath = new List<uint>();
                    foreach (var item in update.DotPath)
                    {
                        var ires = item;
                        if (ires == 0x1095C1E1) ires = 0x7EA285CD; //rewrite: filter id -> returns -> result list
                        resultDotPath.Add(ires);

                        if (ires == packet.RequestTypeID)
                        {
                            break;
                        }
                    }

                    var result = await DataService.SerializePath(resultDotPath.ToArray());
                    if (result != null)
                    {
                        session.Write(new DataServiceWrapperPDU()
                        {
                            SendingAvatarID = packet.SendingAvatarID,
                            RequestTypeID = packet.RequestTypeID,
                            Body = result
                        });
                    }

                    /*var task = DataService.Get(update.DotPath[0], update.DotPath[1]);
                    if(task != null)
                    {
                        var entity = await task;

                        var serialized = DataService.SerializeUpdate(type, entity, msg.Parameter.Value);
                    }*/
                    /**/
                }
            }
            catch (Exception e)
            {
                //SerializePath throws generic exceptions.
                //plus we don't want weird special cases crashing the whole server.
                LOG.Error(e, "Voltron DataService request failed!");
            }
        }
    }
}
