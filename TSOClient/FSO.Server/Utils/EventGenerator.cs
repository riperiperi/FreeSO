using FSO.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Tuning;

namespace FSO.Server.Utils
{
    internal static class EventGenerator
    {
        public static void GenerateEvents(IDAFactory daFactory, EventConfig config)
        {
            using var da = daFactory.Get();

            var tuning = da.Tuning.All();
            var presets = da.Tuning.GetAllPresets().ToList();
            var events = da.Events.All(limit: 9999);

            foreach (var modifier in config.modifiers)
            {
                var (start, end) = EventConfig.GetNextRange(modifier.startDate, modifier.endDate);
                foreach (var option in modifier.options)
                {
                    var optionStart = start;
                    var optionEnd = end;

                    if (option.startDate != null && option.endDate != null)
                    {
                        (optionStart, optionEnd) = EventConfig.GetNextRange(option.startDate, option.endDate);
                    }

                    if (!config.timed)
                    {
                        optionStart = DateTime.MinValue;
                        optionEnd = DateTime.MaxValue;
                    }

                    var disabled = !(config.timed ? option.enableTimed : option.enableManual);

                    if (option.tuning.Count > 0)
                    {
                        // Put this option's tuning into a preset
                        var presetLabel = $"{modifier.label}: {option.label}";
                        var presetIdentifier = $"{modifier.name}-{option.name}";

                        var matchingPreset = presets.Find(preset => preset.description == presetIdentifier && preset.flags == 1);

                        if (matchingPreset != null)
                        {
                            EnsurePresetItems(da, matchingPreset, option.tuning);
                        }
                        else
                        {
                            matchingPreset = new Database.DA.Tuning.DbTuningPreset()
                            {
                                name = presetLabel,
                                description = presetIdentifier,
                                flags = 1,
                            };

                            matchingPreset.preset_id = da.Tuning.CreatePreset(matchingPreset);

                            EnsurePresetItems(da, matchingPreset, option.tuning, true);
                        }

                        // Does the event need updated?
                        var existingEvent = events.Find(x => x.type == Database.DA.DbEvents.DbEventType.obj_tuning && x.value == matchingPreset.preset_id);

                        if (existingEvent != null)
                        {
                            // Check the parameters...
                            if (disabled || existingEvent.start_day != optionStart || existingEvent.end_day != optionEnd)
                            {
                                da.Events.Delete(existingEvent.event_id);
                                existingEvent = null;
                            }
                        }

                        if (existingEvent == null && !disabled)
                        {
                            // Create it new
                            da.Events.Add(new Database.DA.DbEvents.DbEvent()
                            {
                                type = Database.DA.DbEvents.DbEventType.obj_tuning,
                                value = matchingPreset.preset_id,
                                value2 = 0,
                                start_day = optionStart,
                                end_day = optionEnd,
                            });
                        }
                    }

                    if (option.gift != null)
                    {
                        var gift = option.gift.Value;
                        int index = 0;
                        foreach (var obj in gift.guids)
                        {
                            string mail_sender = index == 0 ? $"Event: {option.label}" : null;
                            string mail_subject = index == 0 ? gift.title : null;
                            string mail_message = index == 0 ? gift.description : null;

                            index++;

                            // Does the event need updated?
                            var existingEvent = events.Find(x => 
                                x.type == Database.DA.DbEvents.DbEventType.free_object &&
                                x.value == (int)obj &&
                                x.value2 == 1 &&
                                x.mail_sender_name == mail_sender &&
                                x.mail_subject == mail_subject &&
                                x.mail_message == mail_message);

                            if (existingEvent != null)
                            {
                                // Check the parameters...
                                if (disabled || existingEvent.start_day != optionStart || existingEvent.end_day != optionEnd)
                                {
                                    da.Events.Delete(existingEvent.event_id);
                                    existingEvent = null;
                                }
                            }

                            if (existingEvent == null && !disabled)
                            {
                                // Create it new
                                da.Events.Add(new Database.DA.DbEvents.DbEvent()
                                {
                                    type = Database.DA.DbEvents.DbEventType.free_object,
                                    value = (int)obj,
                                    value2 = 1,
                                    mail_sender_name = mail_sender,
                                    mail_subject = mail_subject,
                                    mail_message = mail_message,
                                    start_day = optionStart,
                                    end_day = optionEnd,
                                });
                            }
                        }
                    }
                }
            }

            var dynTuning = new List<DbTuning>();
            var semiglobal = Content.Content.Get().WorldObjectGlobals.Get("skillobjects");

            float skillSpeed = config.skillSpeed ?? 1;
            if (skillSpeed != 1)
            {
                // Modify the skill completion timings.

                var originalTable = semiglobal.Resource.Tuning.GetTable(8200);

                for (int i = 0; i < 11; i++)
                {
                    short scaledValue = (short)(originalTable.GetKey(i).Value / skillSpeed);

                    dynTuning.Add(new DbTuning()
                    {
                        tuning_type = "skillobjects.iff",
                        tuning_table = 8,
                        tuning_index = i,
                        value = scaledValue,
                        owner_type = DbTuningType.DYNAMIC,
                        owner_id = 2
                    });
                }

                // Multiplier for skills above 10
                dynTuning.Add(new DbTuning()
                {
                    tuning_type = "global.iff",
                    tuning_table = 29,
                    tuning_index = 1,
                    value = (short)(800 / skillSpeed),
                    owner_type = DbTuningType.DYNAMIC,
                    owner_id = 2
                });
            }

            float payoutScale = config.payoutScale ?? 1;
            if (payoutScale != 1)
            {
                // Modify the payout multiplier.

                dynTuning.Add(new DbTuning()
                {
                    tuning_type = "income_mul",
                    tuning_table = 0,
                    tuning_index = 0,
                    value = payoutScale,
                    owner_type = DbTuningType.DYNAMIC,
                    owner_id = 2
                });
            }

            float singleplayerPenalty = config.singleplayerPenalty ?? 1;
            if (singleplayerPenalty != 1)
            {
                // Skills (% modifier)
                // Move the bonus for >0 sims into the 0 sim bonus.
                var skillTable = semiglobal.Resource.Tuning.GetTable(8198);

                int bonusSkill = 0;
                for (int i = 0; i < 6; i++)
                {
                    bonusSkill += skillTable.GetKey(i).Value;
                }

                float pctZero = 1 - singleplayerPenalty;

                for (int i = 0; i < 6; i++)
                {
                    int existingValue = skillTable.GetKey(i).Value;

                    dynTuning.Add(new DbTuning()
                    {
                        tuning_type = "skillobjects.iff",
                        tuning_table = 6,
                        tuning_index = i,
                        value = i == 0 ? (short)(bonusSkill * pctZero) : (short)(existingValue * singleplayerPenalty),
                        owner_type = DbTuningType.DYNAMIC,
                        owner_id = 2
                    });
                }

                // Money

                var moneyTable = semiglobal.Resource.Tuning.GetTable(8196);
                // Move the multiplier for the max group into the payout multiplier
                int moneyMultiplier = moneyTable.GetKey(4).Value;

                dynTuning.Add(new DbTuning()
                {
                    tuning_type = "skillobjects.iff",
                    tuning_table = 4,
                    tuning_index = 4,
                    value = (short)(moneyMultiplier * singleplayerPenalty),
                    owner_type = DbTuningType.DYNAMIC,
                    owner_id = 2
                });

                dynTuning.Add(new DbTuning()
                {
                    tuning_type = "income_mul",
                    tuning_table = 0,
                    tuning_index = 1,
                    value = 1 + ((moneyMultiplier - 10) / 10f) * pctZero,
                    owner_type = DbTuningType.DYNAMIC,
                    owner_id = 2
                });
            }

            da.DynPayouts.ReplaceDynTuning(dynTuning, 2);
        }

        private static void EnsurePresetItems(IDA da, DbTuningPreset preset, Dictionary<string, float> tuning, bool isNew = false)
        {
            var existing = isNew ? [] : da.Tuning.GetPresetItems(preset.preset_id).ToList();

            foreach (var item in tuning)
            {
                var split = item.Key.Split(':');

                if (split.Length != 3 || !int.TryParse(split[1], out int table) || !int.TryParse(split[2], out int index))
                {
                    continue;
                }

                string type = split[0];

                var existingIndex = existing.FindIndex(x => x.tuning_type == type && x.tuning_table == table && x.tuning_index == index);

                if (existingIndex == -1)
                {
                    da.Tuning.CreatePresetItem(new DbTuningPresetItem()
                    {
                        preset_id = preset.preset_id,
                        tuning_type = type,
                        tuning_table = table,
                        tuning_index = index,
                        value = item.Value,
                    });
                }
                else
                {
                    var existingItem = existing[existingIndex];
                    existing.RemoveAt(existingIndex);

                    if (existingItem.value != item.Value)
                    {
                        da.Tuning.UpdatePresetItemValue(existingItem.item_id, item.Value);
                    }
                }
            }

            // Delete anything that shouldn't be in the preset.
            foreach (var item in existing)
            {
                da.Tuning.DeletePreset(item.item_id);
            }
        }
    }
}
