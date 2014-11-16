using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoginDataModel.Entities
{
    public class CharacterAccess
    {
        private DataAccess Context;

        public CharacterAccess(DataAccess context)
        {
            this.Context = context;
        }

        public IQueryable<Character> GetForAccount(int accountId)
        {
            return Context.Context.Characters.Where(x => x.AccountID == accountId);
        }

        public CharacterCreationStatus CreateCharacter(Character Char)
        {
            if (Char.Name.Length > 24)
            {
                return CharacterCreationStatus.NameTooLong;
            }

            try
            {
                Context.Context.Characters.InsertOnSubmit(Char);
                Context.Context.SubmitChanges();
            }
            catch (Exception E)
            {
                Logger.Log("Exception when creating character:\r\n" + E.ToString(), LogLevel.warn);
                return CharacterCreationStatus.NameAlreadyExisted;
            }

            return CharacterCreationStatus.Success;
        }

        public void RetireCharacter(Character Char)
        {
            Context.Context.Characters.DeleteOnSubmit(Char);
            Context.Context.SubmitChanges();
        }
    }

    public enum CharacterCreationStatus
    {
        NameAlreadyExisted,
        NameTooLong,
        ExceededCharacterLimit,
        Success,
        GeneralError
    }
}
