// 
//  ____  _     __  __      _        _ 
// |  _ \| |__ |  \/  | ___| |_ __ _| |
// | | | | '_ \| |\/| |/ _ \ __/ _` | |
// | |_| | |_) | |  | |  __/ || (_| | |
// |____/|_.__/|_|  |_|\___|\__\__,_|_|
//
// Auto-generated from tso on 2013-08-17 17:12:10Z.
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
public partial class Account : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _accountID;
	
	private string _accountName;
	
	private string _password;
	
	private EntitySet<Character> _characters;
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAccountIDChanged();
		
		partial void OnAccountIDChanging(int value);
		
		partial void OnAccountNameChanged();
		
		partial void OnAccountNameChanging(string value);
		
		partial void OnPasswordChanged();
		
		partial void OnPasswordChanging(string value);
		#endregion
	
	
	public Account()
	{
		_characters = new EntitySet<Character>(new Action<Character>(this.Characters_Attach), new Action<Character>(this.Characters_Detach));
		this.OnCreated();
	}
	
	[Column(Storage="_accountID", Name="AccountID", DbType="int(10)", IsPrimaryKey=true, IsDbGenerated=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int AccountID
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
				this.SendPropertyChanging();
				this._accountID = value;
				this.SendPropertyChanged("AccountID");
				this.OnAccountIDChanged();
			}
		}
	}
	
	[Column(Storage="_accountName", Name="AccountName", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
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
				this.SendPropertyChanging();
				this._accountName = value;
				this.SendPropertyChanged("AccountName");
				this.OnAccountNameChanged();
			}
		}
	}
	
	[Column(Storage="_password", Name="Password", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
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
				this.SendPropertyChanging();
				this._password = value;
				this.SendPropertyChanged("Password");
				this.OnPasswordChanged();
			}
		}
	}
	
	#region Children
	[Association(Storage="_characters", OtherKey="AccountID", ThisKey="AccountID", Name="FK_character_account")]
	[DebuggerNonUserCode()]
	public EntitySet<Character> Characters
	{
		get
		{
			return this._characters;
		}
		set
		{
			this._characters = value;
		}
	}
	#endregion
	
	public event System.ComponentModel.PropertyChangingEventHandler PropertyChanging;
	
	public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
	
	protected virtual void SendPropertyChanging()
	{
		System.ComponentModel.PropertyChangingEventHandler h = this.PropertyChanging;
		if ((h != null))
		{
			h(this, emptyChangingEventArgs);
		}
	}
	
	protected virtual void SendPropertyChanged(string propertyName)
	{
		System.ComponentModel.PropertyChangedEventHandler h = this.PropertyChanged;
		if ((h != null))
		{
			h(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
		}
	}
	
	#region Attachment handlers
	private void Characters_Attach(Character entity)
	{
		this.SendPropertyChanging();
		entity.Account = this;
	}
	
	private void Characters_Detach(Character entity)
	{
		this.SendPropertyChanging();
		entity.Account = null;
	}
	#endregion
}

[Table(Name="tso.character")]
public partial class Character : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _accountID;
	
	private int _characterID;
	
	private string _city;
	
	private System.Guid _guid;
	
	private string _lastCached;
	
	private string _name;
	
	private string _sex;
	
	private EntityRef<Account> _account = new EntityRef<Account>();
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAccountIDChanged();
		
		partial void OnAccountIDChanging(int value);
		
		partial void OnCharacterIDChanged();
		
		partial void OnCharacterIDChanging(int value);
		
		partial void OnCityChanged();
		
		partial void OnCityChanging(string value);
		
		partial void OnGUIDChanged();
		
		partial void OnGUIDChanging(System.Guid value);
		
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
	
	[Column(Storage="_accountID", Name="AccountID", DbType="int", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int AccountID
	{
		get
		{
			return this._accountID;
		}
		set
		{
			if ((_accountID != value))
			{
				if (_account.HasLoadedOrAssignedValue)
				{
					throw new System.Data.Linq.ForeignKeyReferenceAlreadyHasValueException();
				}
				this.OnAccountIDChanging(value);
				this.SendPropertyChanging();
				this._accountID = value;
				this.SendPropertyChanged("AccountID");
				this.OnAccountIDChanged();
			}
		}
	}
	
	[Column(Storage="_characterID", Name="CharacterID", DbType="int(10)", IsPrimaryKey=true, IsDbGenerated=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int CharacterID
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
				this.SendPropertyChanging();
				this._characterID = value;
				this.SendPropertyChanged("CharacterID");
				this.OnCharacterIDChanged();
			}
		}
	}
	
	[Column(Storage="_city", Name="City", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
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
				this.SendPropertyChanging();
				this._city = value;
				this.SendPropertyChanged("City");
				this.OnCityChanged();
			}
		}
	}
	
	[Column(Storage="_guid", Name="GUID", DbType="varchar(36)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public System.Guid GUID
	{
		get
		{
			return this._guid;
		}
		set
		{
			if ((_guid != value))
			{
				this.OnGUIDChanging(value);
				this.SendPropertyChanging();
				this._guid = value;
				this.SendPropertyChanged("GUID");
				this.OnGUIDChanged();
			}
		}
	}
	
	[Column(Storage="_lastCached", Name="LastCached", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
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
				this.SendPropertyChanging();
				this._lastCached = value;
				this.SendPropertyChanged("LastCached");
				this.OnLastCachedChanged();
			}
		}
	}
	
	[Column(Storage="_name", Name="Name", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
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
				this.SendPropertyChanging();
				this._name = value;
				this.SendPropertyChanged("Name");
				this.OnNameChanged();
			}
		}
	}
	
	[Column(Storage="_sex", Name="Sex", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
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
				this.SendPropertyChanging();
				this._sex = value;
				this.SendPropertyChanged("Sex");
				this.OnSexChanged();
			}
		}
	}
	
	#region Parents
	[Association(Storage="_account", OtherKey="AccountID", ThisKey="AccountID", Name="FK_character_account", IsForeignKey=true)]
	[DebuggerNonUserCode()]
	public Account Account
	{
		get
		{
			return this._account.Entity;
		}
		set
		{
			if (((this._account.Entity == value) 
						== false))
			{
				if ((this._account.Entity != null))
				{
					Account previousAccount = this._account.Entity;
					this._account.Entity = null;
					previousAccount.Characters.Remove(this);
				}
				this._account.Entity = value;
				if ((value != null))
				{
					value.Characters.Add(this);
					_accountID = value.AccountID;
				}
				else
				{
					_accountID = default(int);
				}
			}
		}
	}
	#endregion
	
	public event System.ComponentModel.PropertyChangingEventHandler PropertyChanging;
	
	public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
	
	protected virtual void SendPropertyChanging()
	{
		System.ComponentModel.PropertyChangingEventHandler h = this.PropertyChanging;
		if ((h != null))
		{
			h(this, emptyChangingEventArgs);
		}
	}
	
	protected virtual void SendPropertyChanged(string propertyName)
	{
		System.ComponentModel.PropertyChangedEventHandler h = this.PropertyChanged;
		if ((h != null))
		{
			h(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
		}
	}
}
