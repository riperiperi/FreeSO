using FSO.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Embedded
{
    internal static class ArchiveConfigBuilder
    {
        public static ServerConfiguration Build(ArchiveConfiguration config)
        {
            // TODO: server directory (build nfs and db string from this), server public host/ports

            string publicHost = "0.0.0.0"; // city connection is up to the user, lot connection automatically uses city
            int cityPort = 33;
            int lotPort = 34;

            string binding = config.Flags.HasFlag(ArchiveConfigFlags.Offline) ? "127.0.0.1" : "0.0.0.0";

            return new ServerConfiguration()
            {
                GameLocation = "unused",
                Secret = Guid.NewGuid().ToString(),
                Archive = config,
                SimNFS = config.ArchiveDataDirectory,
                Database = new Database.DatabaseConfiguration()
                {
                    Engine = "sqlite",
                    ConnectionString = $"Data Source={Path.GetFullPath(Path.Combine(config.ArchiveDataDirectory, "fsoarchive.db"))};Version=3;UTF8Encoding=True",
                },
                
                Services = new ServerConfigurationservices()
                {
                    Tasks = new Servers.Tasks.TaskServerConfiguration()
                    {
                        Enabled = true,
                        Call_Sign = "callisto",
                        Binding = "127.0.0.1:35100",
                        Internal_Host = "127.0.0.1:35",
                        Public_Host = "127.0.0.1:35",
                        Schedule = new List<Servers.Tasks.ScheduledTaskRunOptions>()
                        {
                            new Servers.Tasks.ScheduledTaskRunOptions()
                            {
                                Cron = "0 3 * * *",
                                Task = "prune_database",
                                Timeout = 3600,
                                Parameter = { }
                            },
                            new Servers.Tasks.ScheduledTaskRunOptions()
                            {
                                Cron = "0 4 * * *",
                                Task = "bonus",
                                Timeout = 3600,
                                Run_If_Missed = true,
                                Shard_Id = 1,
                                Parameter = { }
                            },
                            new Servers.Tasks.ScheduledTaskRunOptions()
                            {
                                Cron = "0 4 * * *",
                                Task = "job_balance",
                                Timeout = 3600,
                                Run_If_Missed = true,
                                Parameter = { }
                            },
                            new Servers.Tasks.ScheduledTaskRunOptions()
                            {
                                Cron = "0 0 * * *",
                                Task = "neighborhood_tick",
                                Timeout = 3600,
                                Run_If_Missed = true,
                                Parameter = { }
                            },
                            new Servers.Tasks.ScheduledTaskRunOptions()
                            {
                                Cron = "0 0 * * *",
                                Task = "birthday_gift",
                                Timeout = 3600,
                                Run_If_Missed = true,
                                Parameter = { }
                            }
                        },
                        Tuning = new Servers.Tasks.TaskTuning()
                        {
                            Bonus = new Servers.Tasks.Domain.BonusTaskTuning()
                            {
                                property_bonus = new Servers.Tasks.Domain.PropertyBonusTuning()
                                {
                                    per_unit = 10,
                                    overrides = new Dictionary<byte, int>()
                                    {
                                        { 1, 1500 },
                                        { 2, 1250 },
                                        { 3, 1000 }
                                    }
                                },
                                visitor_bonus = new Servers.Tasks.Domain.VisitorBonusTuning()
                                {
                                    per_unit = 8
                                }
                            },
                            BirthdayGift = new Servers.Tasks.Domain.BirthdayGiftTaskTuning()
                            {
                                items = new List<Servers.Tasks.Domain.BirthdayGiftItem>()
                                {
                                    new Servers.Tasks.Domain.BirthdayGiftItem()
                                    {
                                        age = 1000,
                                        guid = 1303919565,
                                        mail_subject = "1000 Days!",
                                        mail_message = "This is an example gift that shows how birthday gifts can be awarded by the server at different milestones - this one is for 1000 days. Please change this message. Or leave it the same, I don't mind.\n - Rhys",
                                        mail_sender_name = "FreeSO Developers"
                                    }
                                }
                            }
                        }
                    },
                    Cities = new List<Servers.City.CityServerConfiguration>()
                    {
                        new Servers.City.CityServerConfiguration()
                        {
                            Call_Sign = "ganymede",
                            ID = 1,
                            Binding = $"{binding}:{cityPort}100",
                            Internal_Host = $"127.0.0.1:{cityPort}",
                            Public_Host = $"{publicHost}:{cityPort}",
                            Neighborhoods = new Servers.City.CityServerNhoodConfiguration()
                            {
                                Mayor_Elegibility_Limit = 4,
                                Mayor_Elegilility_Falloff = 4,
                                Min_Nominations = 2,
                                Election_Week_Align = true,
                                Election_Move_Penalty = 14
                            },
                            Maintenance = new Servers.City.CityServerMaintenanceConfiguration()
                            {
                                Cron = "0 4 * * *",
                                Timeout = 3600,
                                Visits_Retention_Period = 7,
                            }
                        }
                    },
                    Lots = new List<Servers.Lot.LotServerConfiguration>()
                    {
                        new Servers.Lot.LotServerConfiguration()
                        {
                            Call_Sign = "europa",
                            Binding = $"{binding}:{lotPort}100",
                            Internal_Host = $"127.0.0.1:{lotPort}",
                            Public_Host = $"{publicHost}:{lotPort}",
                            Max_Lots = 100,
                            Cities = new Servers.Lot.LotServerConfigurationCity[]
                            {
                                new Servers.Lot.LotServerConfigurationCity()
                                {
                                    ID = 1,
                                    Host = $"127.0.0.1:{cityPort}100"
                                }
                            }
                        }
                    }
                } 
            };
        }
    }
}
