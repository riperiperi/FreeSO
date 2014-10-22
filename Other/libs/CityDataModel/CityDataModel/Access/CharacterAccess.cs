using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CityDataModel.Entities
{
    /// <summary>
    /// This was created by Darren in one of his refactoring spasms - WTF?! FFS! ¤%(¤%()¤%"/#(¤&)¤%(#/
    /// </summary>
    public class CharacterAccess
    {
        private DataAccess Context;

        public CharacterAccess(DataAccess context)
        {
            this.Context = context;
        }

        public Character GetForCharacterGUID(Guid GUID)
        {
            return Context.Context.Characters.FirstOrDefault(x => x.GUID == GUID);
        }

        public IQueryable<Character> GetForAccount(int accountId)
        {
            return Context.Context.Characters.Where(x => x.AccountID == accountId);
        }

        public CharacterCreationStatus CreateCharacter(Character character)
        {
            if (character.Name.Length > 24)
            {
                return CharacterCreationStatus.ExceededCharacterLimit;
            }

            try
            {
                Context.Context.Characters.InsertOnSubmit(character);
                Context.Context.SubmitChanges();
            }
            catch (Exception ex)
            {
                return CharacterCreationStatus.NameAlreadyExisted;
            }

            return CharacterCreationStatus.Success;
        }

        public void RetireCharacter(Character Char)
        {
            if (Char != null)
            {
                Context.Context.Characters.DeleteOnSubmit(Char);
                Context.Context.SubmitChanges();
            }
        }
    }

    public enum CharacterCreationStatus
    {
        NameAlreadyExisted,
        ExceededCharacterLimit,
        Success,
        GeneralError
    }
}
