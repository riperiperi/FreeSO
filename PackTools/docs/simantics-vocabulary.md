# SimAntics Vocabulary (FreeSO engine)

Source of truth: `/Users/katlaszlo/Desktop/Github-Wiki/GitHub/FreeSO/TSOClient` (paths below relative to this).
Primitive registry: `tso.simantics/VMContext.cs:92-517` (`InitVMConfig(bool ts1)`). Handlers/operands: `tso.simantics/Primitives/*.cs`. Editor metadata: `FSO.IDE/EditorComponent/Primitives/*Descriptor.cs`.

## 1. Registered primitives

All operands are 8 raw bytes, parsed little-endian (`IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)` in every operand `Read`). Opcode is a ushort in each instruction; 0-255 = primitive, >=256 = tree call (see section 4).

| Opcode | Name | Purpose | Operand class | Registered at |
|---|---|---|---|---|
| 0x00 | sleep | Wait N ticks, count held in a parameter | VMSleepOperand | VMContext.cs:94 |
| 0x01 | generic_sims_online_call (TSO) / generic_sims_call (TS1) | Grab-bag of engine calls selected by sub-opcode | VMGenericTSOCallOperand / VMGenericTS1CallOperand | :502 / :464 |
| 0x02 | expression | Read/write/compare variables — the language's assignment + branch workhorse | VMExpressionOperand | :103 |
| 0x03 | find_best_interaction (TS1 only) | Autonomy: pick best interaction | VMFindBestActionOperand | :457 |
| 0x04 | grab | Sim picks up stack object | VMGrabOperand | :112 |
| 0x05 | drop | Sim puts down carried object | VMDropOperand | :119 |
| 0x06 | change_suit_or_accessory | Change avatar outfit/accessory | VMChangeSuitOrAccessoryOperand | :126 |
| 0x07 | refresh | Refresh object graphics/light/relative position | VMRefreshOperand | :133 |
| 0x08 | random_number | Store random 0..range-1 into a variable | VMRandomNumberOperand | :140 |
| 0x09 | burn | Set object on fire | VMBurnOperand | :147 |
| 0x0B | get_distance_to | Distance between two objects → variable | VMGetDistanceToOperand | :156 |
| 0x0C | get_direction_to | Direction to stack object → variable | VMGetDirectionToOperand | :163 |
| 0x0D | push_interaction | Queue an interaction on a sim | VMPushInteractionOperand | :170 |
| 0x0E | find_best_object_for_function | Find object serving a function (eat, sit…) → stack object | VMFindBestObjectForFunctionOperand | :177 |
| 0x0F | breakpoint | Debugger break if condition true | VMBreakPointOperand | :184 |
| 0x10 | find_location_for | Find placeable location for stack object | VMFindLocationForOperand | :191 |
| 0x11 | idle_for_input | Idle N ticks, interruptible by queued interactions | VMIdleForInputOperand | :198 |
| 0x12 | remove_object_instance | Delete object | VMRemoveObjectInstanceOperand | :205 |
| 0x13 | make_new_character (TS1 only) | Create a new sim | VMTS1MakeNewCharacterOperand | :471 |
| 0x14 | run_functional_tree | Run entry-point tree (main/gardening/…) of stack object | VMRunFunctionalTreeOperand | :214 |
| 0x15 | show_string | Debug string display (hijacked by FreeSO) | VMShowStringOperand | :223 |
| 0x16 | look_towards | Sim head-look at target | VMLookTowardsOperand | :230 |
| 0x17 | play_sound | Play sound event on object | VMPlaySoundOperand | :237 |
| 0x18 | old_relationship | Get/set relationship var (old operand layout) | VMOldRelationshipOperand | :244 |
| 0x19 | transfer_funds (TSO) / budget (TS1) | Move simoleons between accounts | VMTransferFundsOperand | :509 / :478 |
| 0x1A | relationship | Get/set relationship var | VMRelationshipOperand | :252 |
| 0x1B | goto_relative | Route sim to position relative to stack object | VMGotoRelativePositionOperand | :259 |
| 0x1C | run_tree_by_name | Call tree by name string (TTAs/BHAV name lookup) | VMRunTreeByNameOperand | :266 |
| 0x1D | set_motive_deltas | Set per-tick motive change rate | VMSetMotiveChangeOperand | :273 |
| 0x1E | syslog (TSO) / gosub_found_action (TS1) | Log / call action found by find-best | VMSysLogOperand / VMGosubFoundActionOperand | :281 / :485 |
| 0x1F | set_to_next | Iterate objects (by category/GUID) into a variable | VMSetToNextOperand | :288 |
| 0x20 | test_object_type | Is object in var of given GUID? | VMTestObjectTypeOperand | :295 |
| 0x23 | special_effect | Spawn effect/fanfare over object | VMSpecialEffectOperand | :306 |
| 0x24 | dialog_private | Dialog using object's private STR#301 | VMDialogOperand | :313 |
| 0x25 | test_sim_interacting_with | Is sim interacting with stack object? | VMTestSimInteractingWithOperand | :320 |
| 0x26 | dialog_global | Dialog using global strings | VMDialogOperand | :327 |
| 0x27 | dialog_semiglobal | Dialog using semiglobal strings | VMDialogOperand | :334 |
| 0x28 | online_jobs_call | TSO job-lot engine calls | VMOnlineJobsCallOperand | :341 |
| 0x29 | set_balloon_headline | Thought balloon / headline above object | VMSetBalloonHeadlineOperand | :348 |
| 0x2A | create_object_instance | Spawn new object by GUID | VMCreateObjectInstanceOperand | :355 |
| 0x2B | drop_onto | Move carried object into slot of stack object | VMDropOntoOperand | :362 |
| 0x2C | animate | Play animation on sim | VMAnimateSimOperand | :369 |
| 0x2D | goto_routing_slot | Route sim to a SLOT of the stack object | VMGotoRoutingSlotOperand | :376 |
| 0x2E | snap | Teleport sim into a routing slot | VMSnapOperand | :383 |
| 0x2F | reach | Reach to/into stack object (pick up etc.) | VMReachOperand | :390 |
| 0x30 | stop_all_sounds | Stop sounds owned by this object | VMStopAllSoundsOperand | :397 |
| 0x31 | stackobj_notify_out_of_idle | Interrupt stack object's sleep/idle | VMAnimateSimOperand (reused) | :404 |
| 0x32 | change_action_string | Rewrite queued-interaction caption | VMChangeActionStringOperand | :411 |
| 0x33 | manage_inventory (TS1 only) | TS1 inventory ops | VMTS1InventoryOperationsOperand | :492 |
| 0x3E | invoke_plugin | Run a FreeSO/TSO server plugin (jobs, pizza…) | VMInvokePluginOperand | :422 |
| 0x3F | get_terrain_info | Query terrain under object | VMGetTerrainInfoOperand | :429 |
| 0x41 | find_best_action | TSO autonomy: pick best action | VMFindBestActionOperand | :438 |
| 0x43 | inventory_operations | TSO inventory ops | VMInventoryOperationsOperand | :448 |

Unimplemented opcodes (0x0A tutorial, 0x21 find-5-worst-motives, 0x22 ui-effect, etc.) execute as no-op GOTO_TRUE (`VMThread.cs:574-577`). Comment at VMContext.cs:418 points to simantics.wikidot.com for the full historic set.

## 2. Operand layouts — the 15 authoring-relevant primitives

Byte offsets are within the fixed 8-byte operand; trailing bytes are unused padding.

### expression (0x02) — `VMExpressionOperand`, tso.simantics/Primitives/VMExpression.cs:268-302
| Off | Field | Type | Meaning |
|---|---|---|---|
| 0-1 | LhsData | int16 | LHS index/literal (meaning depends on LhsOwner scope) |
| 2-3 | RhsData | int16 | RHS index/literal |
| 4 | IsSigned | byte | signedness hint (editor only) |
| 5 | Operator | byte | `VMExpressionOperator` (VMExpression.cs:304-329): 0 `>`, 1 `<`, 2 `==`, 3 `+=`, 4 `-=`, 5 `=` (assign), 6 `*=`, 7 `/=`, 8 isFlagSet, 9 setFlag, 10 clearFlag, 11 `++ & <`, 12 `%=`, 13 `&=`, 14 `>=`, 15 `<=`, 16 `!=`, 17 `-- & >`, 18 push (TS1: `|=`), 19 pop (TS1: `^=`), 20 TS1 sqrt-assign |
| 6 | LhsOwner | byte | `VMVariableScope` (section 3) |
| 7 | RhsOwner | byte | `VMVariableScope` |

Comparisons branch true/false; mutations return true (VMExpression.cs:146). `%=` is positive-modulo; `/=0` yields -1 (:120-137). Push/pop use scope lists in TSO, LhsData 0=front 1=back (:203-249).

### sleep (0x00) — `VMSleepOperand`, VMSleep.cs:37-55
| 0-1 | StackVarToDec | int16 | index of the **parameter** (arg) holding tick count; decremented each tick until < 0 (VMSleep.cs:15-33). Interruptible by notify-out-of-idle. |

### animate (0x2C) — `VMAnimateSimOperand`, VMAnimateSim.cs:182-310
| 0-1 | AnimationID | uint16 | anim table index (0 = clear/reset animation) |
| 2 | LocalEventNumber | byte | local var receiving animation events |
| 3 | _pad | byte | unused |
| 4 | Source | byte | `VMAnimationScope` (Engine/Scopes/VMAnimationScope.cs): 0 Object, 1 Global, 2 PersonStock, 3 Misc |
| 5 | Flags | byte | bit0+bit4 = Mode (0 play&wait, 2 stop-carry play&wait); bit1 PlayBackwards; bit2 IDFromParam (AnimationID is a param index); bit5 StoreFrameInLocal; bit6 Hurryable (VMAnimateSim.cs:236-305) |
| 6 | ExpectedEventCount | byte | number of anim events expected |

### play_sound (0x17) — `VMPlaySoundOperand`, VMPlaySound.cs:63-150
| 0-1 | EventID | uint16 | FWAV/sound event id |
| 2-3 | SampleRate | uint16 | 8.8 fixed point, effectively unused (comment :66) |
| 4 | Flags | byte | bit0 Loop, bit1 StackObjAsSource, bit2 NoZoom, bit3 NoPan, bit4 AutoVary, bit5 SimSpeedAffects |
| 5 | Volume | byte | volume |

### dialog_* (0x24/0x26/0x27) — `VMDialogOperand`, VMDialogPrivateStrings.cs:138-219
| 0 | CancelStringID | byte | button 3 label (STR id, 1-based; 0 = none) |
| 1 | IconNameStringID | byte | icon name string |
| 2 | MessageStringID | byte | body text string |
| 3 | YesStringID | byte | button 1 label |
| 4 | NoStringID | byte | button 2 label |
| 5 | Type | byte | `VMDialogType` (:242-270): 0 Message, 1 YesNo, 2 YesNoCancel, 3 TextEntry, 5 NumericEntry, 6 ImageMapped, 7 Custom, 8 UserBitmap; 127-129 FreeSO extensions |
| 6 | TitleStringID | byte | title string |
| 7 | Flags | byte | `VMDialogFlags` (:221-240): bit0 Continue (non-blocking), bits1-3 icon type (0 auto,1 none,2 neighbour,3 indexed,4 named), bit4 UseTempXL, bit5 UseTemp1, bit6 FilterProfanity, bit7 NewEngageContinue |

String source table depends on opcode (private STR#301 / global / semiglobal). Blocking dialogs park thread in `VMDialogResult` async state; response code 0/1/2 = yes-ok/no/cancel (:272-281).

### goto_routing_slot (0x2D) — `VMGotoRoutingSlotOperand`, VMGotoRoutingSlot.cs:33-75
| 0-1 | Data | uint16 | slot number or variable index (per Type) |
| 2-3 | Type | uint16 | `VMSlotScope` (Engine/Scopes/VMSlotScope.cs): 0 StackVariable (param holds slot #), 1 Literal, 2 Global |
| 4 | Flags | byte | bit0 NoFailureTrees |

### push_interaction (0x0D) — `VMPushInteractionOperand`, VMPushInteraction.cs:64-136
| 0 | Interaction | byte | TTAB index of interaction to push (or 254 = interaction id in temp, see handler) |
| 1 | ObjectLocation | byte | index of variable holding target object id |
| 2 | Priority | byte | `VMPushPriority` (:138-147): 0 Inherited, 1 Maximum, 2 Autonomous, 3 UserDriven, 4 ParentIdle, 5 ParentExit, 6 Idle |
| 3 | Flags | byte | bit0 UseCustomIcon, bit1 ObjectInLocal (ObjectLocation is a local, else param), bit2 PushHeadContinuation, bit7 PushTailContinuation |
| 4 | IconLocation | byte | variable holding custom icon object id |

### find_best_object_for_function (0x0E) — `VMFindBestObjectForFunctionOperand`, VMFindBestObjectForFunction.cs:151-171
| 0-1 | Function | uint16 | function index into the entry-point table (same indices as run_functional_tree: prepare-food, eat, sit, etc.); result becomes stack object, returns false if none found |

### set_motive_deltas (0x1D) — `VMSetMotiveChangeOperand`, VMSetMotiveChange.cs:43-112
| 0 | DeltaOwner | byte | VMVariableScope of per-tick delta |
| 1 | MaxOwner | byte | VMVariableScope of clamp value |
| 2 | Motive | byte | `VMMotive` (Model/VMMotive.cs): 0-3 Happy*/Mood, 5 Energy, 6 Comfort, 7 Hunger, 8 Hygiene, 9 Bladder, 11 SleepState, 13 Room, 14 Social, 15 Fun |
| 3 | Flags | byte | bit0 ClearAll (zero all deltas), bit1 Once |
| 4-5 | DeltaData | int16 | delta operand data |
| 6-7 | MaxData | int16 | max operand data |

### test_object_type (0x20) — `VMTestObjectTypeOperand`, VMTestObjectType.cs:28-52
| 0-3 | GUID | uint32 | OBJD GUID to test against |
| 4-5 | IdData | int16 | data for scope holding object id |
| 6 | IdOwner | byte | VMVariableScope of object id |

### random_number (0x08) — `VMRandomNumberOperand`, VMRandomNumber.cs:19-46
| 0-1 | DestinationData | int16 | destination var data |
| 2-3 | DestinationScope | **uint16** | VMVariableScope (note: 2 bytes here, not 1) |
| 4-5 | RangeData | int16 | range var data |
| 6-7 | RangeScope | **uint16** | VMVariableScope |

### remove_object_instance (0x12) — `VMRemoveObjectInstanceOperand`, VMRemoveObjectInstance.cs:27-76
| 0-1 | Target | int16 | 0 = me, else stack object (per handler) |
| 2 | Flags | byte | bit0 ReturnImmediately, bit1 CleanupAll (remove multi-tile group) |

### create_object_instance (0x2A) — `VMCreateObjectInstanceOperand`, VMCreateObjectInstance.cs:174-305
| 0-3 | GUID | uint32 | OBJD GUID to spawn |
| 4 | Position | byte | `VMCreateObjectPosition` (:311-323): 0 InFrontOfMe, 1 OnTopOfMe, 2 InMyHand, 3 InFrontOfStackObject, 4 InSlot0OfStackObject, 5 UnderneathMe, 6 OutOfWorld, 7 BelowObjectInStackParam0, 8 BelowObjectInLocal, 9 NextToMeInDirectionOfLocal |
| 5 | Flags | byte | bit0 NoDuplicate, bit1 PassObjectIds, bit2 UseNeighbor, bit3 FailIfNonEmpty, bit4 PassTemp0, bit5 FaceStackObjDir (:206-275) |
| 6 | LocalToUse | byte | local index for positions 8/9 |
| 7 | InteractionCallback | byte | interaction to push on the new object |

New object id lands in stack object.

### change_suit_or_accessory (0x06) — `VMChangeSuitOrAccessoryOperand`, VMChangeSuitOrAccessory.cs:161-233
| 0 | SuitData | byte | suit index (or temp index if UseTemp) |
| 1 | SuitScope | byte | `VMSuitScope` (Engine/Scopes/VMSuitScope.cs): 0 Global, 1 Person (`VMPersonSuits` slots), 2 Object |
| 2-3 | Flags | uint16 | bit0 Remove, bit1 UseTemp, bit2 Update |

### idle_for_input (0x11) — `VMIdleForInputOperand`, VMIdleForInput.cs:53-66
| 0-1 | StackVarToDec | int16 | parameter index holding tick count (like sleep) |
| 2-3 | AllowPush | uint16 | 1 = queued interactions may interrupt this idle |

## 3. VMVariableScope — where expressions read/write

`tso.simantics/Engine/Scopes/VMVariableScope.cs:3-65`. The scope byte + a 16-bit "data" value address every readable/writable cell. Key values (data = index unless noted):

| # | Name | Meaning |
|---|---|---|
| 0 | MyObjectAttributes | attribute[data] of caller |
| 1 | StackObjectAttributes | attribute[data] of stack object |
| 3 | MyObject | caller's object-data field[data] (position, room, flags…) |
| 4 | StackObject | stack object's object-data field[data] |
| 6 | Global | global sim state[data] |
| 7 | Literal | data IS the value |
| 8 | Temps | temp register[data] (temp0-temp7 conventionally) |
| 9 | Parameters | current tree arg[data] |
| 10 | StackObjectID | the stack object id itself (data ignored) |
| 11 | TempByTemp | temp[temp[data]] |
| 12/28/29 | TreeAdRange / TreeAdPersonalityVar / TreeAdMin | autonomy advertisement tuning |
| 13 | StackObjectTemp | stack object's temp[data] |
| 14/15 | MyMotives / StackObjectMotives | motive[data] (VMMotive index) |
| 16/20 | StackObjectSlot / MySlot | object id in slot[data] |
| 17 | StackObjectMotiveByTemp | stackobj motive[temp[data]] |
| 18/19 | MyPersonData / StackObjectPersonData | person data[data] (skills, flags…) |
| 21 | StackObjectDefinition | OBJD field[data] of stack object |
| 22 | StackObjectAttributeByParameter | stackobj attribute[param[data]] |
| 24/32/38 | NeighborInStackObject / NeighborPersonData / NeighborsObjectDefinition | TS1 neighbour data |
| 25 | Local | local[data] |
| 26 | Tuning | BCON/tuning constant; data encodes table+index |
| 27 | DynSpriteFlagForTempOfStackObject | dynamic sprite flag[temp[data]] |
| 30/31 | MyPersonDataByTemp / StackObjectPersonDataByTemp | person data[temp[data]] |
| 33/34 | JobData / NeighborhoodData | TS1 |
| 35 | StackObjectFunction | entry-point table of stack object |
| 36/37 | MyTypeAttr / StackObjectTypeAttr | per-type (shared) attributes |
| 40/41 | LocalByTemp / StackObjectAttributeByTemp | indexed via temp[data] |
| 42 | TempXL | 32-bit temp register[data] |
| 43/44/45 | CityTime / TSOStandardTime / GameTime | clock fields[data] |
| 46/47 | MyList / StackObjectList | object list cursor ops |
| 48 | MoneyOverHead32Bit | money-over-head display |
| 49-53 | lead-tile / master-def accessors (multi-tile objects) |
| 54 | FeatureEnableLevel | FreeSO feature gate |
| 59 | MyAvatarID | caller's persist avatar id |

Writability varies per scope (enforced in `Engine/Utils/VMMemory.cs`). Editor names for each scope's slots come from `EditorScope.GetVarScopeDataNames` used across descriptors (e.g. `FSO.IDE/EditorComponent/Primitives/SleepDescriptor.cs:33`).

## 4. BHAV structure rules

`tso.files/Formats/IFF/Chunks/BHAV.cs`:
- Chunk header: version 0x8000-0x8003. TSO writes **0x8003** (Type byte, Args byte, Locals **byte**, 2 pad, Version uint16, count uint32 — :98-104); TS1 writes 0x8002 with Locals as uint16 (:80-86). `Args` = number of declared parameters, `Locals` = number of locals; both are declared in the header, not in code.
- Each instruction = **12 bytes**: Opcode uint16, TruePointer byte, FalsePointer byte, Operand 8 bytes (:64-69, BHAVInstruction :120-127). Execution starts at instruction 0.
- Pointers are instruction indices, so a tree maxes out at ~253 instructions. Special values (`tso.simantics/Engine/VMThread.cs:652-669`): **255 = return false**, **254 = return true**, **253 = "error"/no branch** — if one branch is 253 execution continues down the other; if both are 253 the frame pops with ERROR.
- Opcode dispatch (`VMThread.cs:561-583`): opcode < 256 → primitive table; **opcode >= 256 → subroutine call**: >= 8192 semiglobal tree, >= 4096 private tree (the BHAV chunk id inside the object's own IFF), 256-4095 global tree (globals.iff). The 8-byte operand of a tree call is `VMSubRoutineOperand` = four int16 args (`Primitives/VMSubRoutine.cs:37-46`); an arg of **-1 passes temp[i]** when any of args 1-3 is nonzero (`UseTemp0`, VMSubRoutine.cs:27,44; consumed at VMThread.cs:508-517). Callee gets max(Args,4) arg slots.
- Object linkage: `OBJD.BHAV_MainID` (tso.files/Formats/IFF/Chunks/OBJD.cs:193,431) is the object's main loop tree; `BHAV_Init` (:212,470) runs at creation. Interactions live in the **TTAB** chunk: each `TTABInteraction` has `ActionFunction` and `TestFunction` (tree ids), `MotiveEntries` (autonomy advertisements), `Flags`, `TTAIndex` (the index push_interaction references), `AutonomyThreshold`, `Flags2`/TSOFlags (TTAB.cs:193-206). Interaction captions come from the paired TTAs string chunk with the same id.
- Return values across trees: the caller's branch is chosen by the callee's return true/false; data is passed back via temps (temp0 by convention) since args are copies.

## 5. Gotchas for an authoring schema

1. **Fixed 8-byte operand, little-endian, per-primitive layout.** No shared encoding: scope bytes are 1 byte in expression (VMExpression.cs:285-286) but **2 bytes** in random_number (VMRandomNumber.cs:30,32). Never assume a uniform (data,scope) pair size.
2. **Two dialects.** `InitVMConfig(bool ts1)` swaps opcodes 1, 3, 19, 25, 30, 51 between TS1 and TSO (VMContext.cs:455-515), and expression operators 18-20 change meaning (list push/pop in TSO vs `|=`/`^=`/sqrt in TS1 — VMExpression.cs:192-255). A schema must be dialect-tagged.
3. **Same operand class, different primitive.** `stackobj_notify_out_of_idle` (0x31) reuses `VMAnimateSimOperand` (VMContext.cs:404-409); dialogs share `VMDialogOperand` across 3 opcodes; relationship has two operand layouts (0x18 vs 0x1A).
4. **Sentinel-heavy conventions**: tree-call arg -1 = temp[i]; string ids are 1-based with 0 = "none"; interaction index 254 in push_interaction = "from temp"; branch pointer 253/254/255 are reserved — generated trees must keep real instruction count <= 253.
5. **Version pitfall**: writing TSO BHAVs truncates Locals to a byte (BHAV.cs:101); >255 locals silently corrupts.
6. **Unknown opcodes don't fail** — they return true and fall through (VMThread.cs:574-577), so a typo'd opcode produces silent misbehavior, not an error.
7. **Flags are dense bitfields** with non-obvious packing (Animate `Mode` spreads across bits 0 and 4 — VMAnimateSim.cs:288-305). Schema should expose the named boolean properties, not raw flag bytes; the C# property accessors are the authoritative bit map.
8. **Engine hacks exist**: expression `<`/`>=` against literal 1024 on MyObject.Room is special-cased for stairs (VMExpression.cs:165-177) — semantic comparisons aren't always pure.
9. Editor descriptors (`FSO.IDE/EditorComponent/Primitives/*Descriptor.cs`) carry the human-readable semantics (`GetBody`, `PopulateOperandView` text) and per-field value providers — the best source for enum labels when generating docs; `SubroutineDescriptor.cs` covers tree calls.
