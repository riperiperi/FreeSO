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

        public IQueryable<Character> GetForCharacterGUID(string GUID)
        {
            return Context.Context.Characters.Where(x => x.GUID.ToString() == GUID);
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
    }

    public enum CharacterCreationStatus
    {
        NameAlreadyExisted,
        ExceededCharacterLimit,
        Success,
        GeneralError
    }
}
