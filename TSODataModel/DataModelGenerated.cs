// 
//  ____  _     __  __      _        _ 
// |  _ \| |__ |  \/  | ___| |_ __ _| |
// | | | | '_ \| |\/| |/ _ \ __/ _` | |
// | |_| | |_) | |  | |  __/ || (_| | |
// |____/|_.__/|_|  |_|\___|\__\__,_|_|
//
// Auto-generated from tso on 2013-12-04 00:07:16Z.
// Please visit http://code.google.com/p/dblinq2007/ for more information.
//
using System;
using System.ComponentModel;
using System.Data;
#if MONO_STRICT
	using System.Data.Linq;
#else   // MONO_STRICT
	using DbLinq.Data.Linq;
	using DbLinq.Vendor;
#endif  // MONO_STRICT
	using System.Data.Linq.Mapping;
using System.Diagnostics;


public partial class DB : DataContext
{
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		#endregion
	
	
	public DB(string connectionString) : 
			base(connectionString)
	{
		this.OnCreated();
	}
	
	public DB(string connection, MappingSource mappingSource) : 
			base(connection, mappingSource)
	{
		this.OnCreated();
	}
	
	public DB(IDbConnection connection, MappingSource mappingSource) : 
			base(connection, mappingSource)
	{
		this.OnCreated();
	}
	
	public Table<Account> Accounts
	{
		get
		{
			return this.GetTable<Account>();
		}
	}
	
	public Table<Character> Characters
	{
		get
		{
			return this.GetTable<Character>();
		}
	}
}

#region Start MONO_STRICT
#if MONO_STRICT

public partial class TSo
{
	
	public TSo(IDbConnection connection) : 
			base(connection)
	{
		this.OnCreated();
	}
}
#region End MONO_STRICT
	#endregion
#else     // MONO_STRICT

public partial class DB
{
	
	public DB(IDbConnection connection) : 
			base(connection, new DbLinq.MySql.MySqlVendor())
	{
		this.OnCreated();
	}
	
	public DB(IDbConnection connection, IVendor sqlDialect) : 
			base(connection, sqlDialect)
	{
		this.OnCreated();
	}
	
	public DB(IDbConnection connection, MappingSource mappingSource, IVendor sqlDialect) : 
			base(connection, mappingSource, sqlDialect)
	{
		this.OnCreated();
	}
}
#region End Not MONO_STRICT
	#endregion
#endif     // MONO_STRICT
#endregion

[Table(Name="tso.account")]
public partial class Account
{
	
	private System.Nullable<int> _accountID;
	
	private string _accountName;
	
	private System.Nullable<int> _character1;
	
	private System.Nullable<int> _character2;
	
	private System.Nullable<int> _character3;
	
	private System.Nullable<int> _numCharacters;
	
	private string _password;
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAccountIDChanged();
		
		partial void OnAccountIDChanging(System.Nullable<int> value);
		
		partial void OnAccountNameChanged();
		
		partial void OnAccountNameChanging(string value);
		
		partial void OnCharacter1Changed();
		
		partial void OnCharacter1Changing(System.Nullable<int> value);
		
		partial void OnCharacter2Changed();
		
		partial void OnCharacter2Changing(System.Nullable<int> value);
		
		partial void OnCharacter3Changed();
		
		partial void OnCharacter3Changing(System.Nullable<int> value);
		
		partial void OnNumCharactersChanged();
		
		partial void OnNumCharactersChanging(System.Nullable<int> value);
		
		partial void OnPasswordChanged();
		
		partial void OnPasswordChanging(string value);
		#endregion
	
	
	public Account()
	{
		this.OnCreated();
	}
	
	[Column(Storage="_accountID", Name="AccountID", DbType="int", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<int> AccountID
	{
		get
		{
			return this._accountID;
		}
		set
		{
			if ((_accountID != value))
			{
				this.OnAccountIDChanging(value);
				this._accountID = value;
				this.OnAccountIDChanged();
			}
		}
	}
	
	[Column(Storage="_accountName", Name="AccountName", DbType="varchar(50)", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public string AccountName
	{
		get
		{
			return this._accountName;
		}
		set
		{
			if (((_accountName == value) 
						== false))
			{
				this.OnAccountNameChanging(value);
				this._accountName = value;
				this.OnAccountNameChanged();
			}
		}
	}
	
	[Column(Storage="_character1", Name="Character1", DbType="int", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<int> Character1
	{
		get
		{
			return this._character1;
		}
		set
		{
			if ((_character1 != value))
			{
				this.OnCharacter1Changing(value);
				this._character1 = value;
				this.OnCharacter1Changed();
			}
		}
	}
	
	[Column(Storage="_character2", Name="Character2", DbType="int", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<int> Character2
	{
		get
		{
			return this._character2;
		}
		set
		{
			if ((_character2 != value))
			{
				this.OnCharacter2Changing(value);
				this._character2 = value;
				this.OnCharacter2Changed();
			}
		}
	}
	
	[Column(Storage="_character3", Name="Character3", DbType="int", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<int> Character3
	{
		get
		{
			return this._character3;
		}
		set
		{
			if ((_character3 != value))
			{
				this.OnCharacter3Changing(value);
				this._character3 = value;
				this.OnCharacter3Changed();
			}
		}
	}
	
	[Column(Storage="_numCharacters", Name="NumCharacters", DbType="int", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<int> NumCharacters
	{
		get
		{
			return this._numCharacters;
		}
		set
		{
			if ((_numCharacters != value))
			{
				this.OnNumCharactersChanging(value);
				this._numCharacters = value;
				this.OnNumCharactersChanged();
			}
		}
	}
	
	[Column(Storage="_password", Name="Password", DbType="varchar(50)", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public string Password
	{
		get
		{
			return this._password;
		}
		set
		{
			if (((_password == value) 
						== false))
			{
				this.OnPasswordChanging(value);
				this._password = value;
				this.OnPasswordChanged();
			}
		}
	}
}

[Table(Name="tso.character")]
public partial class Character
{
	
	private System.Nullable<int> _accountID;
	
	private System.Nullable<long> _bodyOutfitID;
	
	private System.Nullable<int> _characterID;
	
	private string _city;
	
	private string _guid;
	
	private System.Nullable<long> _headOutfitID;
	
	private string _lastCached;
	
	private string _name;
	
	private string _sex;
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAccountIDChanged();
		
		partial void OnAccountIDChanging(System.Nullable<int> value);
		
		partial void OnBodyOutfitIDChanged();
		
		partial void OnBodyOutfitIDChanging(System.Nullable<long> value);
		
		partial void OnCharacterIDChanged();
		
		partial void OnCharacterIDChanging(System.Nullable<int> value);
		
		partial void OnCityChanged();
		
		partial void OnCityChanging(string value);
		
		partial void OnGUIDChanged();
		
		partial void OnGUIDChanging(string value);
		
		partial void OnHeadOutfitIDChanged();
		
		partial void OnHeadOutfitIDChanging(System.Nullable<long> value);
		
		partial void OnLastCachedChanged();
		
		partial void OnLastCachedChanging(string value);
		
		partial void OnNameChanged();
		
		partial void OnNameChanging(string value);
		
		partial void OnSexChanged();
		
		partial void OnSexChanging(string value);
		#endregion
	
	
	public Character()
	{
		this.OnCreated();
	}
	
	[Column(Storage="_accountID", Name="AccountID", DbType="int", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<int> AccountID
	{
		get
		{
			return this._accountID;
		}
		set
		{
			if ((_accountID != value))
			{
				this.OnAccountIDChanging(value);
				this._accountID = value;
				this.OnAccountIDChanged();
			}
		}
	}
	
	[Column(Storage="_bodyOutfitID", Name="BodyOutfitID", DbType="bigint(20)", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<long> BodyOutfitID
	{
		get
		{
			return this._bodyOutfitID;
		}
		set
		{
			if ((_bodyOutfitID != value))
			{
				this.OnBodyOutfitIDChanging(value);
				this._bodyOutfitID = value;
				this.OnBodyOutfitIDChanged();
			}
		}
	}
	
	[Column(Storage="_characterID", Name="CharacterID", DbType="int", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<int> CharacterID
	{
		get
		{
			return this._characterID;
		}
		set
		{
			if ((_characterID != value))
			{
				this.OnCharacterIDChanging(value);
				this._characterID = value;
				this.OnCharacterIDChanged();
			}
		}
	}
	
	[Column(Storage="_city", Name="City", DbType="varchar(50)", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public string City
	{
		get
		{
			return this._city;
		}
		set
		{
			if (((_city == value) 
						== false))
			{
				this.OnCityChanging(value);
				this._city = value;
				this.OnCityChanged();
			}
		}
	}
	
	[Column(Storage="_guid", Name="GUID", DbType="varchar(50)", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public string GUID
	{
		get
		{
			return this._guid;
		}
		set
		{
			if (((_guid == value) 
						== false))
			{
				this.OnGUIDChanging(value);
				this._guid = value;
				this.OnGUIDChanged();
			}
		}
	}
	
	[Column(Storage="_headOutfitID", Name="HeadOutfitID", DbType="bigint(20)", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<long> HeadOutfitID
	{
		get
		{
			return this._headOutfitID;
		}
		set
		{
			if ((_headOutfitID != value))
			{
				this.OnHeadOutfitIDChanging(value);
				this._headOutfitID = value;
				this.OnHeadOutfitIDChanged();
			}
		}
	}
	
	[Column(Storage="_lastCached", Name="LastCached", DbType="varchar(50)", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public string LastCached
	{
		get
		{
			return this._lastCached;
		}
		set
		{
			if (((_lastCached == value) 
						== false))
			{
				this.OnLastCachedChanging(value);
				this._lastCached = value;
				this.OnLastCachedChanged();
			}
		}
	}
	
	[Column(Storage="_name", Name="Name", DbType="varchar(50)", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public string Name
	{
		get
		{
			return this._name;
		}
		set
		{
			if (((_name == value) 
						== false))
			{
				this.OnNameChanging(value);
				this._name = value;
				this.OnNameChanged();
			}
		}
	}
	
	[Column(Storage="_sex", Name="Sex", DbType="varchar(50)", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public string Sex
	{
		get
		{
			return this._sex;
		}
		set
		{
			if (((_sex == value) 
						== false))
			{
				this.OnSexChanging(value);
				this._sex = value;
				this.OnSexChanged();
			}
		}
	}
}
