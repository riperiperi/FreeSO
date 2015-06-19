/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the CityDatamodel.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

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

		/// <summary>
		/// Returns the first character for a specific character GUID.
		/// </summary>
		/// <param name="GUID">A Guid instance for a character.</param>
		/// <returns>IQueryable instance containing the first character found for the GUID.</returns>
        public Character GetForCharacterGUID(Guid GUID)
        {
            return Context.Context.Characters.FirstOrDefault(x => x.GUID == GUID);
        }

		/// <summary>
		/// Returns all the characters for a specific account.
		/// </summary>
		/// <param name="accountId">Account's ID.</param>
		/// <returns>IQueryable instance containing all characters for the account.</returns>
        public IQueryable<Character> GetForAccount(int accountId)
        {
            return Context.Context.Characters.Where(x => x.AccountID == accountId);
        }

		/// <summary>
		/// Returns all characters with houses from the DB.
		/// </summary>
		/// <returns>IQueryable instance containing all characters with houses.</returns>
		public IQueryable<Character> GetAllCharsWithHouses()
		{
			return Context.Context.Characters.Where(x => x.House != null);
		}

		/// <summary>
		/// Attempts to create a character in the DB.
		/// </summary>
		/// <param name="character">A Character instance to add to the DB.</param>
		/// <returns>A CharacterCreationStatus indicating success or failure.</returns>
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

		/// <summary>
		/// Attempts to retire a character from the DB.
		/// </summary>
		/// <param name="Char">A Character instance to retire.</param>
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
        NameTooLong,
        ExceededCharacterLimit,
        Success,
        GeneralError
    }
}
