using FSO.Client.Controllers;
using FSO.Common.DataService.Model;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.LotControls
{
    public class UIAvatarDataServiceUpdater
    {
        private UILotControl Control;
        private VM vm
        {
            get { return Control.vm; }
        }

        public UIAvatarDataServiceUpdater(UILotControl owner)
        {
            Control = owner;
        }

        private HashSet<uint> LastActive = new HashSet<uint>();
        private HashSet<uint> PendingDSRequests = new HashSet<uint>();
        private Dictionary<uint, Avatar> AvatarObjects = new Dictionary<uint, Avatar>();
        private int Ticker;

        public void Update()
        {
            if (Ticker++ > 60*4)
            {
                var changed = new HashSet<uint>();
                foreach (var avatar in vm.Context.ObjectQueries.AvatarsByPersist)
                {
                    changed.Add(avatar.Key);
                    UpdateAvatar(avatar.Key, avatar.Value);
                }
                var release = (LastActive.Except(changed));
                foreach (var dcAva in release)
                {
                    Avatar obj = null;
                    AvatarObjects.TryGetValue(dcAva, out obj);
                    if (obj != null) obj.ClientSourced = false;
                }
                LastActive = changed;
                Ticker = 0;
            }
        }

        public void ReleaseAvatars()
        {
            foreach (var dcAva in LastActive)
            {
                Avatar obj = null;
                AvatarObjects.TryGetValue(dcAva, out obj);
                if (obj != null) obj.ClientSourced = false;
            }
        }

        private void UpdateAvatar(uint pid, VMAvatar avatar)
        {
            Avatar obj = null;
            if (AvatarObjects.TryGetValue(pid, out obj)) {
                UpdateAvatar(obj, avatar);
            }
            else if (!PendingDSRequests.Contains(pid))
            {
                var controller = Control.FindController<CoreGameScreenController>();
                if (controller != null)
                {
                    PendingDSRequests.Add(pid);
                    controller.GetAvatarModel(pid, (model) =>
                    {
                        //should happen in UI thread.
                        AvatarObjects[pid] = model;
                        PendingDSRequests.Remove(pid);
                        UpdateAvatar(model, avatar);
                    });
                }
            }
        }

        private void UpdateAvatar(Avatar model, VMAvatar avatar)
        {
            model.ClientSourced = true;
            model.Avatar_CurrentJob = (ushort)avatar.GetPersonData(VMPersonDataVariable.OnlineJobID);

            model.Avatar_SkillsLockPoints = (ushort)avatar.SkillLocks;
            model.Avatar_Skills = new AvatarSkills()
            {
                AvatarSkills_Body = (ushort)avatar.GetPersonData(VMPersonDataVariable.BodySkill),
                AvatarSkills_Charisma = (ushort)avatar.GetPersonData(VMPersonDataVariable.CharismaSkill),
                AvatarSkills_Cooking = (ushort)avatar.GetPersonData(VMPersonDataVariable.CookingSkill),
                AvatarSkills_Creativity = (ushort)avatar.GetPersonData(VMPersonDataVariable.CreativitySkill),
                AvatarSkills_Logic = (ushort)avatar.GetPersonData(VMPersonDataVariable.LogicSkill),
                AvatarSkills_Mechanical = (ushort)avatar.GetPersonData(VMPersonDataVariable.MechanicalSkill),

                AvatarSkills_LockLv_Body = (ushort)(avatar.GetPersonData(VMPersonDataVariable.SkillLockBody)/100),
                AvatarSkills_LockLv_Charisma = (ushort)(avatar.GetPersonData(VMPersonDataVariable.SkillLockCharisma) / 100),
                AvatarSkills_LockLv_Cooking = (ushort)(avatar.GetPersonData(VMPersonDataVariable.SkillLockCooking) / 100),
                AvatarSkills_LockLv_Creativity = (ushort)(avatar.GetPersonData(VMPersonDataVariable.SkillLockCreativity) / 100),
                AvatarSkills_LockLv_Logic = (ushort)(avatar.GetPersonData(VMPersonDataVariable.SkillLockLogic) / 100),
                AvatarSkills_LockLv_Mechanical = (ushort)(avatar.GetPersonData(VMPersonDataVariable.SkillLockMechanical) / 100),
            };

            var jobs = new List<JobLevel>();
            foreach (var level in ((VMTSOAvatarState)avatar.TSOState).JobInfo)
            {
                jobs.Add(new JobLevel()
                {
                    JobLevel_JobType = (ushort)level.Key,
                    JobLevel_JobExperience = (ushort)level.Value.Experience,
                    JobLevel_JobGrade = (ushort)level.Value.Level
                });
            }
            model.Avatar_JobLevelVec = jobs;
            /*
            var friendships = new List<Relationship>();
            foreach (var rel in avatar.MeToPersist)
            {
                if (rel.Key < 16777216)
            }
            */
        }
    }
}
