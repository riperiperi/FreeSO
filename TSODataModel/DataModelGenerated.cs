// 
//  ____  _     __  __      _        _ 
// |  _ \| |__ |  \/  | ___| |_ __ _| |
// | | | | '_ \| |\/| |/ _ \ __/ _` | |
// | |_| | |_) | |  | |  __/ || (_| | |
// |____/|_.__/|_|  |_|\___|\__\__,_|_|
//
// Auto-generated from pd on 2013-04-13 12:16:32Z.
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


public partial class PD : DataContext
{
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		#endregion
	
	
	public PD(string connectionString) : 
			base(connectionString)
	{
		this.OnCreated();
	}
	
	public PD(string connection, MappingSource mappingSource) : 
			base(connection, mappingSource)
	{
		this.OnCreated();
	}
	
	public PD(IDbConnection connection, MappingSource mappingSource) : 
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
	
	public Table<AccountRole> AccountRoles
	{
		get
		{
			return this.GetTable<AccountRole>();
		}
	}
	
	public Table<AccountSession> AccountSessions
	{
		get
		{
			return this.GetTable<AccountSession>();
		}
	}
	
	public Table<Avatar> Avatar
	{
		get
		{
			return this.GetTable<Avatar>();
		}
	}
	
	public Table<AvatarInBox> AvatarInBoxes
	{
		get
		{
			return this.GetTable<AvatarInBox>();
		}
	}
	
	public Table<AvatarMetric> AvatarMetrics
	{
		get
		{
			return this.GetTable<AvatarMetric>();
		}
	}
	
	public Table<AvatarRelationship> AvatarRelationships
	{
		get
		{
			return this.GetTable<AvatarRelationship>();
		}
	}
	
	public Table<City> Cities
	{
		get
		{
			return this.GetTable<City>();
		}
	}
	
	public Table<CityMotD> CityMotD
	{
		get
		{
			return this.GetTable<CityMotD>();
		}
	}
	
	public Table<CityNeighborhood> CityNeighborhoods
	{
		get
		{
			return this.GetTable<CityNeighborhood>();
		}
	}
	
	public Table<SecurityRole> SecurityRoles
	{
		get
		{
			return this.GetTable<SecurityRole>();
		}
	}
	
	public Table<Setting> Settings
	{
		get
		{
			return this.GetTable<Setting>();
		}
	}
	
	public Table<SettingsAvatarMetric> SettingsAvatarMetrics
	{
		get
		{
			return this.GetTable<SettingsAvatarMetric>();
		}
	}
	
	public Table<SettingsDefaultMetric> SettingsDefaultMetrics
	{
		get
		{
			return this.GetTable<SettingsDefaultMetric>();
		}
	}
}

#region Start MONO_STRICT
#if MONO_STRICT

public partial class PD
{
	
	public PD(IDbConnection connection) : 
			base(connection)
	{
		this.OnCreated();
	}
}
#region End MONO_STRICT
	#endregion
#else     // MONO_STRICT

public partial class PD
{
	
	public PD(IDbConnection connection) : 
			base(connection, new DbLinq.MySql.MySqlVendor())
	{
		this.OnCreated();
	}
	
	public PD(IDbConnection connection, IVendor sqlDialect) : 
			base(connection, sqlDialect)
	{
		this.OnCreated();
	}
	
	public PD(IDbConnection connection, MappingSource mappingSource, IVendor sqlDialect) : 
			base(connection, mappingSource, sqlDialect)
	{
		this.OnCreated();
	}
}
#region End Not MONO_STRICT
	#endregion
#endif     // MONO_STRICT
#endregion

[Table(Name="pd.account")]
public partial class Account : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _accountID;
	
	private System.Nullable<System.DateTime> _created;
	
	private string _email;
	
	private string _password;
	
	private sbyte _status;
	
	private System.DateTime _updated;
	
	private string _userName;
	
	private string _uuid;
	
	private EntitySet<AccountRole> _accountRoles;
	
	private EntitySet<Avatar> _avatar;
	
	private EntitySet<AccountSession> _accountSessions;
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAccountIDChanged();
		
		partial void OnAccountIDChanging(int value);
		
		partial void OnCreatedChanged();
		
		partial void OnCreatedChanging(System.Nullable<System.DateTime> value);
		
		partial void OnEmailChanged();
		
		partial void OnEmailChanging(string value);
		
		partial void OnPasswordChanged();
		
		partial void OnPasswordChanging(string value);
		
		partial void OnStatusChanged();
		
		partial void OnStatusChanging(sbyte value);
		
		partial void OnUpdatedChanged();
		
		partial void OnUpdatedChanging(System.DateTime value);
		
		partial void OnUserNameChanged();
		
		partial void OnUserNameChanging(string value);
		
		partial void OnUUIDChanged();
		
		partial void OnUUIDChanging(string value);
		#endregion
	
	
	public Account()
	{
		_accountRoles = new EntitySet<AccountRole>(new Action<AccountRole>(this.AccountRoles_Attach), new Action<AccountRole>(this.AccountRoles_Detach));
		_avatar = new EntitySet<Avatar>(new Action<Avatar>(this.Avatar_Attach), new Action<Avatar>(this.Avatar_Detach));
		_accountSessions = new EntitySet<AccountSession>(new Action<AccountSession>(this.AccountSessions_Attach), new Action<AccountSession>(this.AccountSessions_Detach));
		this.OnCreated();
	}
	
	[Column(Storage="_accountID", Name="accountId", DbType="int(10)", IsPrimaryKey=true, IsDbGenerated=true, AutoSync=AutoSync.Never, CanBeNull=false)]
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
	
	[Column(Storage="_created", Name="created", DbType="timestamp", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<System.DateTime> Created
	{
		get
		{
			return this._created;
		}
		set
		{
			if ((_created != value))
			{
				this.OnCreatedChanging(value);
				this.SendPropertyChanging();
				this._created = value;
				this.SendPropertyChanged("Created");
				this.OnCreatedChanged();
			}
		}
	}
	
	[Column(Storage="_email", Name="email", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string Email
	{
		get
		{
			return this._email;
		}
		set
		{
			if (((_email == value) 
						== false))
			{
				this.OnEmailChanging(value);
				this.SendPropertyChanging();
				this._email = value;
				this.SendPropertyChanged("Email");
				this.OnEmailChanged();
			}
		}
	}
	
	[Column(Storage="_password", Name="password", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
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
	
	[Column(Storage="_status", Name="status", DbType="tinyint(4)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public sbyte Status
	{
		get
		{
			return this._status;
		}
		set
		{
			if ((_status != value))
			{
				this.OnStatusChanging(value);
				this.SendPropertyChanging();
				this._status = value;
				this.SendPropertyChanged("Status");
				this.OnStatusChanged();
			}
		}
	}
	
	[Column(Storage="_updated", Name="updated", DbType="timestamp", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public System.DateTime Updated
	{
		get
		{
			return this._updated;
		}
		set
		{
			if ((_updated != value))
			{
				this.OnUpdatedChanging(value);
				this.SendPropertyChanging();
				this._updated = value;
				this.SendPropertyChanged("Updated");
				this.OnUpdatedChanged();
			}
		}
	}
	
	[Column(Storage="_userName", Name="username", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string UserName
	{
		get
		{
			return this._userName;
		}
		set
		{
			if (((_userName == value) 
						== false))
			{
				this.OnUserNameChanging(value);
				this.SendPropertyChanging();
				this._userName = value;
				this.SendPropertyChanged("UserName");
				this.OnUserNameChanged();
			}
		}
	}
	
	[Column(Storage="_uuid", Name="uuid", DbType="char(36)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string UUID
	{
		get
		{
			return this._uuid;
		}
		set
		{
			if (((_uuid == value) 
						== false))
			{
				this.OnUUIDChanging(value);
				this.SendPropertyChanging();
				this._uuid = value;
				this.SendPropertyChanged("UUID");
				this.OnUUIDChanged();
			}
		}
	}
	
	#region Children
	[Association(Storage="_accountRoles", OtherKey="AccountID", ThisKey="AccountID", Name="FK_account_roles_account")]
	[DebuggerNonUserCode()]
	public EntitySet<AccountRole> AccountRoles
	{
		get
		{
			return this._accountRoles;
		}
		set
		{
			this._accountRoles = value;
		}
	}
	
	[Association(Storage="_avatar", OtherKey="AccountID", ThisKey="AccountID", Name="FK_avatar_account")]
	[DebuggerNonUserCode()]
	public EntitySet<Avatar> Avatar
	{
		get
		{
			return this._avatar;
		}
		set
		{
			this._avatar = value;
		}
	}
	
	[Association(Storage="_accountSessions", OtherKey="AccountID", ThisKey="AccountID", Name="FK__account")]
	[DebuggerNonUserCode()]
	public EntitySet<AccountSession> AccountSessions
	{
		get
		{
			return this._accountSessions;
		}
		set
		{
			this._accountSessions = value;
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
	private void AccountRoles_Attach(AccountRole entity)
	{
		this.SendPropertyChanging();
		entity.Account = this;
	}
	
	private void AccountRoles_Detach(AccountRole entity)
	{
		this.SendPropertyChanging();
		entity.Account = null;
	}
	
	private void Avatar_Attach(Avatar entity)
	{
		this.SendPropertyChanging();
		entity.Account = this;
	}
	
	private void Avatar_Detach(Avatar entity)
	{
		this.SendPropertyChanging();
		entity.Account = null;
	}
	
	private void AccountSessions_Attach(AccountSession entity)
	{
		this.SendPropertyChanging();
		entity.Account = this;
	}
	
	private void AccountSessions_Detach(AccountSession entity)
	{
		this.SendPropertyChanging();
		entity.Account = null;
	}
	#endregion
}

[Table(Name="pd.account_roles")]
public partial class AccountRole : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _accountID;
	
	private int _roleID;
	
	private EntityRef<Account> _account = new EntityRef<Account>();
	
	private EntityRef<SecurityRole> _securityRole = new EntityRef<SecurityRole>();
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAccountIDChanged();
		
		partial void OnAccountIDChanging(int value);
		
		partial void OnRoleIDChanged();
		
		partial void OnRoleIDChanging(int value);
		#endregion
	
	
	public AccountRole()
	{
		this.OnCreated();
	}
	
	[Column(Storage="_accountID", Name="accountId", DbType="int(10)", IsPrimaryKey=true, AutoSync=AutoSync.Never, CanBeNull=false)]
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
	
	[Column(Storage="_roleID", Name="roleId", DbType="int(10)", IsPrimaryKey=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int RoleID
	{
		get
		{
			return this._roleID;
		}
		set
		{
			if ((_roleID != value))
			{
				this.OnRoleIDChanging(value);
				this.SendPropertyChanging();
				this._roleID = value;
				this.SendPropertyChanged("RoleID");
				this.OnRoleIDChanged();
			}
		}
	}
	
	#region Parents
	[Association(Storage="_account", OtherKey="AccountID", ThisKey="AccountID", Name="FK_account_roles_account", IsForeignKey=true)]
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
					previousAccount.AccountRoles.Remove(this);
				}
				this._account.Entity = value;
				if ((value != null))
				{
					value.AccountRoles.Add(this);
					_accountID = value.AccountID;
				}
				else
				{
					_accountID = default(int);
				}
			}
		}
	}
	
	[Association(Storage="_securityRole", OtherKey="RoleID", ThisKey="RoleID", Name="FK_account_roles_security_roles", IsForeignKey=true)]
	[DebuggerNonUserCode()]
	public SecurityRole SecurityRole
	{
		get
		{
			return this._securityRole.Entity;
		}
		set
		{
			if (((this._securityRole.Entity == value) 
						== false))
			{
				if ((this._securityRole.Entity != null))
				{
					SecurityRole previousSecurityRole = this._securityRole.Entity;
					this._securityRole.Entity = null;
					previousSecurityRole.AccountRoles.Remove(this);
				}
				this._securityRole.Entity = value;
				if ((value != null))
				{
					value.AccountRoles.Add(this);
					_roleID = value.RoleID;
				}
				else
				{
					_roleID = default(int);
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

[Table(Name="pd.account_session")]
public partial class AccountSession : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _accountID;
	
	private bool _active;
	
	private System.DateTime _created;
	
	private System.Nullable<System.DateTime> _expires;
	
	private string _sessionID;
	
	private EntityRef<Account> _account = new EntityRef<Account>();
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAccountIDChanged();
		
		partial void OnAccountIDChanging(int value);
		
		partial void OnActiveChanged();
		
		partial void OnActiveChanging(bool value);
		
		partial void OnCreatedChanged();
		
		partial void OnCreatedChanging(System.DateTime value);
		
		partial void OnExpiresChanged();
		
		partial void OnExpiresChanging(System.Nullable<System.DateTime> value);
		
		partial void OnSessionIDChanged();
		
		partial void OnSessionIDChanging(string value);
		#endregion
	
	
	public AccountSession()
	{
		this.OnCreated();
	}
	
	[Column(Storage="_accountID", Name="accountId", DbType="int", AutoSync=AutoSync.Never, CanBeNull=false)]
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
	
	[Column(Storage="_active", Name="active", DbType="bit(1)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public bool Active
	{
		get
		{
			return this._active;
		}
		set
		{
			if ((_active != value))
			{
				this.OnActiveChanging(value);
				this.SendPropertyChanging();
				this._active = value;
				this.SendPropertyChanged("Active");
				this.OnActiveChanged();
			}
		}
	}
	
	[Column(Storage="_created", Name="created", DbType="timestamp", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public System.DateTime Created
	{
		get
		{
			return this._created;
		}
		set
		{
			if ((_created != value))
			{
				this.OnCreatedChanging(value);
				this.SendPropertyChanging();
				this._created = value;
				this.SendPropertyChanged("Created");
				this.OnCreatedChanged();
			}
		}
	}
	
	[Column(Storage="_expires", Name="expires", DbType="timestamp", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<System.DateTime> Expires
	{
		get
		{
			return this._expires;
		}
		set
		{
			if ((_expires != value))
			{
				this.OnExpiresChanging(value);
				this.SendPropertyChanging();
				this._expires = value;
				this.SendPropertyChanged("Expires");
				this.OnExpiresChanged();
			}
		}
	}
	
	[Column(Storage="_sessionID", Name="sessionId", DbType="varchar(36)", IsPrimaryKey=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string SessionID
	{
		get
		{
			return this._sessionID;
		}
		set
		{
			if (((_sessionID == value) 
						== false))
			{
				this.OnSessionIDChanging(value);
				this.SendPropertyChanging();
				this._sessionID = value;
				this.SendPropertyChanged("SessionID");
				this.OnSessionIDChanged();
			}
		}
	}
	
	#region Parents
	[Association(Storage="_account", OtherKey="AccountID", ThisKey="AccountID", Name="FK__account", IsForeignKey=true)]
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
					previousAccount.AccountSessions.Remove(this);
				}
				this._account.Entity = value;
				if ((value != null))
				{
					value.AccountSessions.Add(this);
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

[Table(Name="pd.avatar")]
public partial class Avatar : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _accountID;
	
	private int _avatarID;
	
	private int _cityID;
	
	private System.Nullable<System.DateTime> _created;
	
	private string _description;
	
	private bool _gender;
	
	private string _name;
	
	private sbyte _status;
	
	private System.DateTime _updated;
	
	private System.Nullable<System.DateTime> _updatedName;
	
	private string _uuid;
	
	private EntitySet<AvatarInBox> _avatarInBoxes;
	
	private EntitySet<AvatarRelationship> _avatarRelationships;
	
	private EntitySet<AvatarMetric> _avatarMetrics;
	
	private EntityRef<Account> _account = new EntityRef<Account>();
	
	private EntityRef<City> _city = new EntityRef<City>();
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAccountIDChanged();
		
		partial void OnAccountIDChanging(int value);
		
		partial void OnAvatarIDChanged();
		
		partial void OnAvatarIDChanging(int value);
		
		partial void OnCityIDChanged();
		
		partial void OnCityIDChanging(int value);
		
		partial void OnCreatedChanged();
		
		partial void OnCreatedChanging(System.Nullable<System.DateTime> value);
		
		partial void OnDescriptionChanged();
		
		partial void OnDescriptionChanging(string value);
		
		partial void OnGenderChanged();
		
		partial void OnGenderChanging(bool value);
		
		partial void OnNameChanged();
		
		partial void OnNameChanging(string value);
		
		partial void OnStatusChanged();
		
		partial void OnStatusChanging(sbyte value);
		
		partial void OnUpdatedChanged();
		
		partial void OnUpdatedChanging(System.DateTime value);
		
		partial void OnUpdatedNameChanged();
		
		partial void OnUpdatedNameChanging(System.Nullable<System.DateTime> value);
		
		partial void OnUUIDChanged();
		
		partial void OnUUIDChanging(string value);
		#endregion
	
	
	public Avatar()
	{
		_avatarInBoxes = new EntitySet<AvatarInBox>(new Action<AvatarInBox>(this.AvatarInBoxes_Attach), new Action<AvatarInBox>(this.AvatarInBoxes_Detach));
		_avatarRelationships = new EntitySet<AvatarRelationship>(new Action<AvatarRelationship>(this.AvatarRelationships_Attach), new Action<AvatarRelationship>(this.AvatarRelationships_Detach));
		_avatarMetrics = new EntitySet<AvatarMetric>(new Action<AvatarMetric>(this.AvatarMetrics_Attach), new Action<AvatarMetric>(this.AvatarMetrics_Detach));
		this.OnCreated();
	}
	
	[Column(Storage="_accountID", Name="accountId", DbType="int", AutoSync=AutoSync.Never, CanBeNull=false)]
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
	
	[Column(Storage="_avatarID", Name="avatarId", DbType="int(10)", IsPrimaryKey=true, IsDbGenerated=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int AvatarID
	{
		get
		{
			return this._avatarID;
		}
		set
		{
			if ((_avatarID != value))
			{
				this.OnAvatarIDChanging(value);
				this.SendPropertyChanging();
				this._avatarID = value;
				this.SendPropertyChanged("AvatarID");
				this.OnAvatarIDChanged();
			}
		}
	}
	
	[Column(Storage="_cityID", Name="cityId", DbType="int", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int CityID
	{
		get
		{
			return this._cityID;
		}
		set
		{
			if ((_cityID != value))
			{
				this.OnCityIDChanging(value);
				this.SendPropertyChanging();
				this._cityID = value;
				this.SendPropertyChanged("CityID");
				this.OnCityIDChanged();
			}
		}
	}
	
	[Column(Storage="_created", Name="created", DbType="timestamp", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<System.DateTime> Created
	{
		get
		{
			return this._created;
		}
		set
		{
			if ((_created != value))
			{
				this.OnCreatedChanging(value);
				this.SendPropertyChanging();
				this._created = value;
				this.SendPropertyChanged("Created");
				this.OnCreatedChanged();
			}
		}
	}
	
	[Column(Storage="_description", Name="description", DbType="varchar(600)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string Description
	{
		get
		{
			return this._description;
		}
		set
		{
			if (((_description == value) 
						== false))
			{
				this.OnDescriptionChanging(value);
				this.SendPropertyChanging();
				this._description = value;
				this.SendPropertyChanged("Description");
				this.OnDescriptionChanged();
			}
		}
	}
	
	[Column(Storage="_gender", Name="gender", DbType="bit(1)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public bool Gender
	{
		get
		{
			return this._gender;
		}
		set
		{
			if ((_gender != value))
			{
				this.OnGenderChanging(value);
				this.SendPropertyChanging();
				this._gender = value;
				this.SendPropertyChanged("Gender");
				this.OnGenderChanged();
			}
		}
	}
	
	[Column(Storage="_name", Name="name", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
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
	
	[Column(Storage="_status", Name="status", DbType="tinyint(4)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public sbyte Status
	{
		get
		{
			return this._status;
		}
		set
		{
			if ((_status != value))
			{
				this.OnStatusChanging(value);
				this.SendPropertyChanging();
				this._status = value;
				this.SendPropertyChanged("Status");
				this.OnStatusChanged();
			}
		}
	}
	
	[Column(Storage="_updated", Name="updated", DbType="timestamp", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public System.DateTime Updated
	{
		get
		{
			return this._updated;
		}
		set
		{
			if ((_updated != value))
			{
				this.OnUpdatedChanging(value);
				this.SendPropertyChanging();
				this._updated = value;
				this.SendPropertyChanged("Updated");
				this.OnUpdatedChanged();
			}
		}
	}
	
	[Column(Storage="_updatedName", Name="updatedName", DbType="timestamp", AutoSync=AutoSync.Never)]
	[DebuggerNonUserCode()]
	public System.Nullable<System.DateTime> UpdatedName
	{
		get
		{
			return this._updatedName;
		}
		set
		{
			if ((_updatedName != value))
			{
				this.OnUpdatedNameChanging(value);
				this.SendPropertyChanging();
				this._updatedName = value;
				this.SendPropertyChanged("UpdatedName");
				this.OnUpdatedNameChanged();
			}
		}
	}
	
	[Column(Storage="_uuid", Name="uuid", DbType="char(36)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string UUID
	{
		get
		{
			return this._uuid;
		}
		set
		{
			if (((_uuid == value) 
						== false))
			{
				this.OnUUIDChanging(value);
				this.SendPropertyChanging();
				this._uuid = value;
				this.SendPropertyChanged("UUID");
				this.OnUUIDChanged();
			}
		}
	}
	
	#region Children
	[Association(Storage="_avatarInBoxes", OtherKey="AvatarID", ThisKey="AvatarID", Name="FK_avatar_inbox_avatar")]
	[DebuggerNonUserCode()]
	public EntitySet<AvatarInBox> AvatarInBoxes
	{
		get
		{
			return this._avatarInBoxes;
		}
		set
		{
			this._avatarInBoxes = value;
		}
	}
	
	[Association(Storage="_avatarRelationships", OtherKey="AvatarFrom", ThisKey="AvatarID", Name="FK_avatar_relationship_avatar")]
	[DebuggerNonUserCode()]
	public EntitySet<AvatarRelationship> AvatarRelationships
	{
		get
		{
			return this._avatarRelationships;
		}
		set
		{
			this._avatarRelationships = value;
		}
	}
	
	[Association(Storage="_avatarMetrics", OtherKey="AvatarID", ThisKey="AvatarID", Name="FK__avatar")]
	[DebuggerNonUserCode()]
	public EntitySet<AvatarMetric> AvatarMetrics
	{
		get
		{
			return this._avatarMetrics;
		}
		set
		{
			this._avatarMetrics = value;
		}
	}
	#endregion
	
	#region Parents
	[Association(Storage="_account", OtherKey="AccountID", ThisKey="AccountID", Name="FK_avatar_account", IsForeignKey=true)]
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
					previousAccount.Avatar.Remove(this);
				}
				this._account.Entity = value;
				if ((value != null))
				{
					value.Avatar.Add(this);
					_accountID = value.AccountID;
				}
				else
				{
					_accountID = default(int);
				}
			}
		}
	}
	
	[Association(Storage="_city", OtherKey="CityID", ThisKey="CityID", Name="FK_avatar_city", IsForeignKey=true)]
	[DebuggerNonUserCode()]
	public City City
	{
		get
		{
			return this._city.Entity;
		}
		set
		{
			if (((this._city.Entity == value) 
						== false))
			{
				if ((this._city.Entity != null))
				{
					City previousCity = this._city.Entity;
					this._city.Entity = null;
					previousCity.Avatar.Remove(this);
				}
				this._city.Entity = value;
				if ((value != null))
				{
					value.Avatar.Add(this);
					_cityID = value.CityID;
				}
				else
				{
					_cityID = default(int);
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
	
	#region Attachment handlers
	private void AvatarInBoxes_Attach(AvatarInBox entity)
	{
		this.SendPropertyChanging();
		entity.Avatar = this;
	}
	
	private void AvatarInBoxes_Detach(AvatarInBox entity)
	{
		this.SendPropertyChanging();
		entity.Avatar = null;
	}
	
	private void AvatarRelationships_Attach(AvatarRelationship entity)
	{
		this.SendPropertyChanging();
		entity.Avatar = this;
	}
	
	private void AvatarRelationships_Detach(AvatarRelationship entity)
	{
		this.SendPropertyChanging();
		entity.Avatar = null;
	}
	
	private void AvatarMetrics_Attach(AvatarMetric entity)
	{
		this.SendPropertyChanging();
		entity.Avatar = this;
	}
	
	private void AvatarMetrics_Detach(AvatarMetric entity)
	{
		this.SendPropertyChanging();
		entity.Avatar = null;
	}
	#endregion
}

[Table(Name="pd.avatar_inbox")]
public partial class AvatarInBox : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _avatarID;
	
	private string _body;
	
	private System.DateTime _created;
	
	private int _fromAvatar;
	
	private int _fromType;
	
	private int _msgID;
	
	private bool _read;
	
	private string _subject;
	
	private EntityRef<Avatar> _avatar = new EntityRef<Avatar>();
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAvatarIDChanged();
		
		partial void OnAvatarIDChanging(int value);
		
		partial void OnBodyChanged();
		
		partial void OnBodyChanging(string value);
		
		partial void OnCreatedChanged();
		
		partial void OnCreatedChanging(System.DateTime value);
		
		partial void OnFromAvatarChanged();
		
		partial void OnFromAvatarChanging(int value);
		
		partial void OnFromTypeChanged();
		
		partial void OnFromTypeChanging(int value);
		
		partial void OnMsgIDChanged();
		
		partial void OnMsgIDChanging(int value);
		
		partial void OnReadChanged();
		
		partial void OnReadChanging(bool value);
		
		partial void OnSubjectChanged();
		
		partial void OnSubjectChanging(string value);
		#endregion
	
	
	public AvatarInBox()
	{
		this.OnCreated();
	}
	
	[Column(Storage="_avatarID", Name="avatarId", DbType="int(10)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int AvatarID
	{
		get
		{
			return this._avatarID;
		}
		set
		{
			if ((_avatarID != value))
			{
				this.OnAvatarIDChanging(value);
				this.SendPropertyChanging();
				this._avatarID = value;
				this.SendPropertyChanged("AvatarID");
				this.OnAvatarIDChanged();
			}
		}
	}
	
	[Column(Storage="_body", Name="body", DbType="varchar(1000)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string Body
	{
		get
		{
			return this._body;
		}
		set
		{
			if (((_body == value) 
						== false))
			{
				this.OnBodyChanging(value);
				this.SendPropertyChanging();
				this._body = value;
				this.SendPropertyChanged("Body");
				this.OnBodyChanged();
			}
		}
	}
	
	[Column(Storage="_created", Name="created", DbType="timestamp", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public System.DateTime Created
	{
		get
		{
			return this._created;
		}
		set
		{
			if ((_created != value))
			{
				this.OnCreatedChanging(value);
				this.SendPropertyChanging();
				this._created = value;
				this.SendPropertyChanged("Created");
				this.OnCreatedChanged();
			}
		}
	}
	
	[Column(Storage="_fromAvatar", Name="fromAvatar", DbType="int(10)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int FromAvatar
	{
		get
		{
			return this._fromAvatar;
		}
		set
		{
			if ((_fromAvatar != value))
			{
				this.OnFromAvatarChanging(value);
				this.SendPropertyChanging();
				this._fromAvatar = value;
				this.SendPropertyChanged("FromAvatar");
				this.OnFromAvatarChanged();
			}
		}
	}
	
	[Column(Storage="_fromType", Name="fromType", DbType="int(10)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int FromType
	{
		get
		{
			return this._fromType;
		}
		set
		{
			if ((_fromType != value))
			{
				this.OnFromTypeChanging(value);
				this.SendPropertyChanging();
				this._fromType = value;
				this.SendPropertyChanged("FromType");
				this.OnFromTypeChanged();
			}
		}
	}
	
	[Column(Storage="_msgID", Name="msgId", DbType="int(10)", IsPrimaryKey=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int MsgID
	{
		get
		{
			return this._msgID;
		}
		set
		{
			if ((_msgID != value))
			{
				this.OnMsgIDChanging(value);
				this.SendPropertyChanging();
				this._msgID = value;
				this.SendPropertyChanged("MsgID");
				this.OnMsgIDChanged();
			}
		}
	}
	
	[Column(Storage="_read", Name="read", DbType="bit(1)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public bool Read
	{
		get
		{
			return this._read;
		}
		set
		{
			if ((_read != value))
			{
				this.OnReadChanging(value);
				this.SendPropertyChanging();
				this._read = value;
				this.SendPropertyChanged("Read");
				this.OnReadChanged();
			}
		}
	}
	
	[Column(Storage="_subject", Name="subject", DbType="varchar(200)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string Subject
	{
		get
		{
			return this._subject;
		}
		set
		{
			if (((_subject == value) 
						== false))
			{
				this.OnSubjectChanging(value);
				this.SendPropertyChanging();
				this._subject = value;
				this.SendPropertyChanged("Subject");
				this.OnSubjectChanged();
			}
		}
	}
	
	#region Parents
	[Association(Storage="_avatar", OtherKey="AvatarID", ThisKey="AvatarID", Name="FK_avatar_inbox_avatar", IsForeignKey=true)]
	[DebuggerNonUserCode()]
	public Avatar Avatar
	{
		get
		{
			return this._avatar.Entity;
		}
		set
		{
			if (((this._avatar.Entity == value) 
						== false))
			{
				if ((this._avatar.Entity != null))
				{
					Avatar previousAvatar = this._avatar.Entity;
					this._avatar.Entity = null;
					previousAvatar.AvatarInBoxes.Remove(this);
				}
				this._avatar.Entity = value;
				if ((value != null))
				{
					value.AvatarInBoxes.Add(this);
					_avatarID = value.AvatarID;
				}
				else
				{
					_avatarID = default(int);
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

[Table(Name="pd.avatar_metric")]
public partial class AvatarMetric : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _avatarID;
	
	private int _metricID;
	
	private System.DateTime _updated;
	
	private string _value;
	
	private EntityRef<SettingsAvatarMetric> _settingsAvatarMetric = new EntityRef<SettingsAvatarMetric>();
	
	private EntityRef<Avatar> _avatar = new EntityRef<Avatar>();
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAvatarIDChanged();
		
		partial void OnAvatarIDChanging(int value);
		
		partial void OnMetricIDChanged();
		
		partial void OnMetricIDChanging(int value);
		
		partial void OnUpdatedChanged();
		
		partial void OnUpdatedChanging(System.DateTime value);
		
		partial void OnValueChanged();
		
		partial void OnValueChanging(string value);
		#endregion
	
	
	public AvatarMetric()
	{
		this.OnCreated();
	}
	
	[Column(Storage="_avatarID", Name="avatarId", DbType="int(10)", IsPrimaryKey=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int AvatarID
	{
		get
		{
			return this._avatarID;
		}
		set
		{
			if ((_avatarID != value))
			{
				this.OnAvatarIDChanging(value);
				this.SendPropertyChanging();
				this._avatarID = value;
				this.SendPropertyChanged("AvatarID");
				this.OnAvatarIDChanged();
			}
		}
	}
	
	[Column(Storage="_metricID", Name="metricId", DbType="int(10)", IsPrimaryKey=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int MetricID
	{
		get
		{
			return this._metricID;
		}
		set
		{
			if ((_metricID != value))
			{
				this.OnMetricIDChanging(value);
				this.SendPropertyChanging();
				this._metricID = value;
				this.SendPropertyChanged("MetricID");
				this.OnMetricIDChanged();
			}
		}
	}
	
	[Column(Storage="_updated", Name="updated", DbType="timestamp", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public System.DateTime Updated
	{
		get
		{
			return this._updated;
		}
		set
		{
			if ((_updated != value))
			{
				this.OnUpdatedChanging(value);
				this.SendPropertyChanging();
				this._updated = value;
				this.SendPropertyChanged("Updated");
				this.OnUpdatedChanged();
			}
		}
	}
	
	[Column(Storage="_value", Name="value", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string Value
	{
		get
		{
			return this._value;
		}
		set
		{
			if (((_value == value) 
						== false))
			{
				this.OnValueChanging(value);
				this.SendPropertyChanging();
				this._value = value;
				this.SendPropertyChanged("Value");
				this.OnValueChanged();
			}
		}
	}
	
	#region Parents
	[Association(Storage="_settingsAvatarMetric", OtherKey="AvatarMetricID", ThisKey="MetricID", Name="FK_avatar_metric_settings_avatarmetrics", IsForeignKey=true)]
	[DebuggerNonUserCode()]
	public SettingsAvatarMetric SettingsAvatarMetric
	{
		get
		{
			return this._settingsAvatarMetric.Entity;
		}
		set
		{
			if (((this._settingsAvatarMetric.Entity == value) 
						== false))
			{
				if ((this._settingsAvatarMetric.Entity != null))
				{
					SettingsAvatarMetric previousSettingsAvatarMetric = this._settingsAvatarMetric.Entity;
					this._settingsAvatarMetric.Entity = null;
					previousSettingsAvatarMetric.AvatarMetrics.Remove(this);
				}
				this._settingsAvatarMetric.Entity = value;
				if ((value != null))
				{
					value.AvatarMetrics.Add(this);
					_metricID = value.AvatarMetricID;
				}
				else
				{
					_metricID = default(int);
				}
			}
		}
	}
	
	[Association(Storage="_avatar", OtherKey="AvatarID", ThisKey="AvatarID", Name="FK__avatar", IsForeignKey=true)]
	[DebuggerNonUserCode()]
	public Avatar Avatar
	{
		get
		{
			return this._avatar.Entity;
		}
		set
		{
			if (((this._avatar.Entity == value) 
						== false))
			{
				if ((this._avatar.Entity != null))
				{
					Avatar previousAvatar = this._avatar.Entity;
					this._avatar.Entity = null;
					previousAvatar.AvatarMetrics.Remove(this);
				}
				this._avatar.Entity = value;
				if ((value != null))
				{
					value.AvatarMetrics.Add(this);
					_avatarID = value.AvatarID;
				}
				else
				{
					_avatarID = default(int);
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

[Table(Name="pd.avatar_relationship")]
public partial class AvatarRelationship : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _avatarFrom;
	
	private int _avatarTo;
	
	private sbyte _score;
	
	private EntityRef<Avatar> _avatar = new EntityRef<Avatar>();
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAvatarFromChanged();
		
		partial void OnAvatarFromChanging(int value);
		
		partial void OnAvatarToChanged();
		
		partial void OnAvatarToChanging(int value);
		
		partial void OnScoreChanged();
		
		partial void OnScoreChanging(sbyte value);
		#endregion
	
	
	public AvatarRelationship()
	{
		this.OnCreated();
	}
	
	[Column(Storage="_avatarFrom", Name="avatarFrom", DbType="int(10)", IsPrimaryKey=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int AvatarFrom
	{
		get
		{
			return this._avatarFrom;
		}
		set
		{
			if ((_avatarFrom != value))
			{
				this.OnAvatarFromChanging(value);
				this.SendPropertyChanging();
				this._avatarFrom = value;
				this.SendPropertyChanged("AvatarFrom");
				this.OnAvatarFromChanged();
			}
		}
	}
	
	[Column(Storage="_avatarTo", Name="avatarTo", DbType="int(10)", IsPrimaryKey=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int AvatarTo
	{
		get
		{
			return this._avatarTo;
		}
		set
		{
			if ((_avatarTo != value))
			{
				this.OnAvatarToChanging(value);
				this.SendPropertyChanging();
				this._avatarTo = value;
				this.SendPropertyChanged("AvatarTo");
				this.OnAvatarToChanged();
			}
		}
	}
	
	[Column(Storage="_score", Name="score", DbType="tinyint(4)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public sbyte Score
	{
		get
		{
			return this._score;
		}
		set
		{
			if ((_score != value))
			{
				this.OnScoreChanging(value);
				this.SendPropertyChanging();
				this._score = value;
				this.SendPropertyChanged("Score");
				this.OnScoreChanged();
			}
		}
	}
	
	#region Parents
	[Association(Storage="_avatar", OtherKey="AvatarID", ThisKey="AvatarFrom", Name="FK_avatar_relationship_avatar", IsForeignKey=true)]
	[DebuggerNonUserCode()]
	public Avatar Avatar
	{
		get
		{
			return this._avatar.Entity;
		}
		set
		{
			if (((this._avatar.Entity == value) 
						== false))
			{
				if ((this._avatar.Entity != null))
				{
					Avatar previousAvatar = this._avatar.Entity;
					this._avatar.Entity = null;
					previousAvatar.AvatarRelationships.Remove(this);
				}
				this._avatar.Entity = value;
				if ((value != null))
				{
					value.AvatarRelationships.Add(this);
					_avatarFrom = value.AvatarID;
				}
				else
				{
					_avatarFrom = default(int);
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

[Table(Name="pd.city")]
public partial class City : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _cityID;
	
	private string _mapID;
	
	private string _name;
	
	private bool _online;
	
	private int _status;
	
	private string _uuid;
	
	private EntitySet<Avatar> _avatar;
	
	private EntitySet<CityNeighborhood> _cityNeighborhoods;
	
	private EntitySet<CityMotD> _cityMotD;
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnCityIDChanged();
		
		partial void OnCityIDChanging(int value);
		
		partial void OnMapIDChanged();
		
		partial void OnMapIDChanging(string value);
		
		partial void OnNameChanged();
		
		partial void OnNameChanging(string value);
		
		partial void OnOnlineChanged();
		
		partial void OnOnlineChanging(bool value);
		
		partial void OnStatusChanged();
		
		partial void OnStatusChanging(int value);
		
		partial void OnUUIDChanged();
		
		partial void OnUUIDChanging(string value);
		#endregion
	
	
	public City()
	{
		_avatar = new EntitySet<Avatar>(new Action<Avatar>(this.Avatar_Attach), new Action<Avatar>(this.Avatar_Detach));
		_cityNeighborhoods = new EntitySet<CityNeighborhood>(new Action<CityNeighborhood>(this.CityNeighborhoods_Attach), new Action<CityNeighborhood>(this.CityNeighborhoods_Detach));
		_cityMotD = new EntitySet<CityMotD>(new Action<CityMotD>(this.CityMotD_Attach), new Action<CityMotD>(this.CityMotD_Detach));
		this.OnCreated();
	}
	
	[Column(Storage="_cityID", Name="cityId", DbType="int(10)", IsPrimaryKey=true, IsDbGenerated=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int CityID
	{
		get
		{
			return this._cityID;
		}
		set
		{
			if ((_cityID != value))
			{
				this.OnCityIDChanging(value);
				this.SendPropertyChanging();
				this._cityID = value;
				this.SendPropertyChanged("CityID");
				this.OnCityIDChanged();
			}
		}
	}
	
	[Column(Storage="_mapID", Name="mapId", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string MapID
	{
		get
		{
			return this._mapID;
		}
		set
		{
			if (((_mapID == value) 
						== false))
			{
				this.OnMapIDChanging(value);
				this.SendPropertyChanging();
				this._mapID = value;
				this.SendPropertyChanged("MapID");
				this.OnMapIDChanged();
			}
		}
	}
	
	[Column(Storage="_name", Name="name", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
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
	
	[Column(Storage="_online", Name="online", DbType="bit(1)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public bool Online
	{
		get
		{
			return this._online;
		}
		set
		{
			if ((_online != value))
			{
				this.OnOnlineChanging(value);
				this.SendPropertyChanging();
				this._online = value;
				this.SendPropertyChanged("Online");
				this.OnOnlineChanged();
			}
		}
	}
	
	[Column(Storage="_status", Name="status", DbType="int", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int Status
	{
		get
		{
			return this._status;
		}
		set
		{
			if ((_status != value))
			{
				this.OnStatusChanging(value);
				this.SendPropertyChanging();
				this._status = value;
				this.SendPropertyChanged("Status");
				this.OnStatusChanged();
			}
		}
	}
	
	[Column(Storage="_uuid", Name="uuid", DbType="char(36)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string UUID
	{
		get
		{
			return this._uuid;
		}
		set
		{
			if (((_uuid == value) 
						== false))
			{
				this.OnUUIDChanging(value);
				this.SendPropertyChanging();
				this._uuid = value;
				this.SendPropertyChanged("UUID");
				this.OnUUIDChanged();
			}
		}
	}
	
	#region Children
	[Association(Storage="_avatar", OtherKey="CityID", ThisKey="CityID", Name="FK_avatar_city")]
	[DebuggerNonUserCode()]
	public EntitySet<Avatar> Avatar
	{
		get
		{
			return this._avatar;
		}
		set
		{
			this._avatar = value;
		}
	}
	
	[Association(Storage="_cityNeighborhoods", OtherKey="CityID", ThisKey="CityID", Name="FK_city.neighborhood_city")]
	[DebuggerNonUserCode()]
	public EntitySet<CityNeighborhood> CityNeighborhoods
	{
		get
		{
			return this._cityNeighborhoods;
		}
		set
		{
			this._cityNeighborhoods = value;
		}
	}
	
	[Association(Storage="_cityMotD", OtherKey="CityID", ThisKey="CityID", Name="FK__city")]
	[DebuggerNonUserCode()]
	public EntitySet<CityMotD> CityMotD
	{
		get
		{
			return this._cityMotD;
		}
		set
		{
			this._cityMotD = value;
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
	private void Avatar_Attach(Avatar entity)
	{
		this.SendPropertyChanging();
		entity.City = this;
	}
	
	private void Avatar_Detach(Avatar entity)
	{
		this.SendPropertyChanging();
		entity.City = null;
	}
	
	private void CityNeighborhoods_Attach(CityNeighborhood entity)
	{
		this.SendPropertyChanging();
		entity.City = this;
	}
	
	private void CityNeighborhoods_Detach(CityNeighborhood entity)
	{
		this.SendPropertyChanging();
		entity.City = null;
	}
	
	private void CityMotD_Attach(CityMotD entity)
	{
		this.SendPropertyChanging();
		entity.City = this;
	}
	
	private void CityMotD_Detach(CityMotD entity)
	{
		this.SendPropertyChanging();
		entity.City = null;
	}
	#endregion
}

[Table(Name="pd.city_motd")]
public partial class CityMotD : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private string _body;
	
	private int _cityID;
	
	private System.DateTime _created;
	
	private System.DateTime _expires;
	
	private string _from;
	
	private int _motdID;
	
	private string _subject;
	
	private EntityRef<City> _city = new EntityRef<City>();
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnBodyChanged();
		
		partial void OnBodyChanging(string value);
		
		partial void OnCityIDChanged();
		
		partial void OnCityIDChanging(int value);
		
		partial void OnCreatedChanged();
		
		partial void OnCreatedChanging(System.DateTime value);
		
		partial void OnExpiresChanged();
		
		partial void OnExpiresChanging(System.DateTime value);
		
		partial void OnFromChanged();
		
		partial void OnFromChanging(string value);
		
		partial void OnMotdIDChanged();
		
		partial void OnMotdIDChanging(int value);
		
		partial void OnSubjectChanged();
		
		partial void OnSubjectChanging(string value);
		#endregion
	
	
	public CityMotD()
	{
		this.OnCreated();
	}
	
	[Column(Storage="_body", Name="body", DbType="varchar(1000)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string Body
	{
		get
		{
			return this._body;
		}
		set
		{
			if (((_body == value) 
						== false))
			{
				this.OnBodyChanging(value);
				this.SendPropertyChanging();
				this._body = value;
				this.SendPropertyChanged("Body");
				this.OnBodyChanged();
			}
		}
	}
	
	[Column(Storage="_cityID", Name="cityId", DbType="int(10)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int CityID
	{
		get
		{
			return this._cityID;
		}
		set
		{
			if ((_cityID != value))
			{
				this.OnCityIDChanging(value);
				this.SendPropertyChanging();
				this._cityID = value;
				this.SendPropertyChanged("CityID");
				this.OnCityIDChanged();
			}
		}
	}
	
	[Column(Storage="_created", Name="created", DbType="timestamp", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public System.DateTime Created
	{
		get
		{
			return this._created;
		}
		set
		{
			if ((_created != value))
			{
				this.OnCreatedChanging(value);
				this.SendPropertyChanging();
				this._created = value;
				this.SendPropertyChanged("Created");
				this.OnCreatedChanged();
			}
		}
	}
	
	[Column(Storage="_expires", Name="expires", DbType="timestamp", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public System.DateTime Expires
	{
		get
		{
			return this._expires;
		}
		set
		{
			if ((_expires != value))
			{
				this.OnExpiresChanging(value);
				this.SendPropertyChanging();
				this._expires = value;
				this.SendPropertyChanged("Expires");
				this.OnExpiresChanged();
			}
		}
	}
	
	[Column(Storage="_from", Name="from", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string From
	{
		get
		{
			return this._from;
		}
		set
		{
			if (((_from == value) 
						== false))
			{
				this.OnFromChanging(value);
				this.SendPropertyChanging();
				this._from = value;
				this.SendPropertyChanged("From");
				this.OnFromChanged();
			}
		}
	}
	
	[Column(Storage="_motdID", Name="motdId", DbType="int(10)", IsPrimaryKey=true, IsDbGenerated=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int MotdID
	{
		get
		{
			return this._motdID;
		}
		set
		{
			if ((_motdID != value))
			{
				this.OnMotdIDChanging(value);
				this.SendPropertyChanging();
				this._motdID = value;
				this.SendPropertyChanged("MotdID");
				this.OnMotdIDChanged();
			}
		}
	}
	
	[Column(Storage="_subject", Name="subject", DbType="varchar(200)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string Subject
	{
		get
		{
			return this._subject;
		}
		set
		{
			if (((_subject == value) 
						== false))
			{
				this.OnSubjectChanging(value);
				this.SendPropertyChanging();
				this._subject = value;
				this.SendPropertyChanged("Subject");
				this.OnSubjectChanged();
			}
		}
	}
	
	#region Parents
	[Association(Storage="_city", OtherKey="CityID", ThisKey="CityID", Name="FK__city", IsForeignKey=true)]
	[DebuggerNonUserCode()]
	public City City
	{
		get
		{
			return this._city.Entity;
		}
		set
		{
			if (((this._city.Entity == value) 
						== false))
			{
				if ((this._city.Entity != null))
				{
					City previousCity = this._city.Entity;
					this._city.Entity = null;
					previousCity.CityMotD.Remove(this);
				}
				this._city.Entity = value;
				if ((value != null))
				{
					value.CityMotD.Add(this);
					_cityID = value.CityID;
				}
				else
				{
					_cityID = default(int);
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

[Table(Name="pd.city_neighborhood")]
public partial class CityNeighborhood : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _cityID;
	
	private string _name;
	
	private int _nhoodID;
	
	private short _size;
	
	private string _uuid;
	
	private int _x;
	
	private int _y;
	
	private EntityRef<City> _city = new EntityRef<City>();
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnCityIDChanged();
		
		partial void OnCityIDChanging(int value);
		
		partial void OnNameChanged();
		
		partial void OnNameChanging(string value);
		
		partial void OnNhoodIDChanged();
		
		partial void OnNhoodIDChanging(int value);
		
		partial void OnSizeChanged();
		
		partial void OnSizeChanging(short value);
		
		partial void OnUUIDChanged();
		
		partial void OnUUIDChanging(string value);
		
		partial void OnXChanged();
		
		partial void OnXChanging(int value);
		
		partial void OnYChanged();
		
		partial void OnYChanging(int value);
		#endregion
	
	
	public CityNeighborhood()
	{
		this.OnCreated();
	}
	
	[Column(Storage="_cityID", Name="cityId", DbType="int(10)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int CityID
	{
		get
		{
			return this._cityID;
		}
		set
		{
			if ((_cityID != value))
			{
				this.OnCityIDChanging(value);
				this.SendPropertyChanging();
				this._cityID = value;
				this.SendPropertyChanged("CityID");
				this.OnCityIDChanged();
			}
		}
	}
	
	[Column(Storage="_name", Name="name", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
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
	
	[Column(Storage="_nhoodID", Name="nhoodId", DbType="int(10)", IsPrimaryKey=true, IsDbGenerated=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int NhoodID
	{
		get
		{
			return this._nhoodID;
		}
		set
		{
			if ((_nhoodID != value))
			{
				this.OnNhoodIDChanging(value);
				this.SendPropertyChanging();
				this._nhoodID = value;
				this.SendPropertyChanged("NhoodID");
				this.OnNhoodIDChanged();
			}
		}
	}
	
	[Column(Storage="_size", Name="size", DbType="smallint(6)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public short Size
	{
		get
		{
			return this._size;
		}
		set
		{
			if ((_size != value))
			{
				this.OnSizeChanging(value);
				this.SendPropertyChanging();
				this._size = value;
				this.SendPropertyChanged("Size");
				this.OnSizeChanged();
			}
		}
	}
	
	[Column(Storage="_uuid", Name="uuid", DbType="char(36)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string UUID
	{
		get
		{
			return this._uuid;
		}
		set
		{
			if (((_uuid == value) 
						== false))
			{
				this.OnUUIDChanging(value);
				this.SendPropertyChanging();
				this._uuid = value;
				this.SendPropertyChanged("UUID");
				this.OnUUIDChanged();
			}
		}
	}
	
	[Column(Storage="_x", Name="x", DbType="int", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int X
	{
		get
		{
			return this._x;
		}
		set
		{
			if ((_x != value))
			{
				this.OnXChanging(value);
				this.SendPropertyChanging();
				this._x = value;
				this.SendPropertyChanged("X");
				this.OnXChanged();
			}
		}
	}
	
	[Column(Storage="_y", Name="y", DbType="int", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int Y
	{
		get
		{
			return this._y;
		}
		set
		{
			if ((_y != value))
			{
				this.OnYChanging(value);
				this.SendPropertyChanging();
				this._y = value;
				this.SendPropertyChanged("Y");
				this.OnYChanged();
			}
		}
	}
	
	#region Parents
	[Association(Storage="_city", OtherKey="CityID", ThisKey="CityID", Name="FK_city.neighborhood_city", IsForeignKey=true)]
	[DebuggerNonUserCode()]
	public City City
	{
		get
		{
			return this._city.Entity;
		}
		set
		{
			if (((this._city.Entity == value) 
						== false))
			{
				if ((this._city.Entity != null))
				{
					City previousCity = this._city.Entity;
					this._city.Entity = null;
					previousCity.CityNeighborhoods.Remove(this);
				}
				this._city.Entity = value;
				if ((value != null))
				{
					value.CityNeighborhoods.Add(this);
					_cityID = value.CityID;
				}
				else
				{
					_cityID = default(int);
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

[Table(Name="pd.security_roles")]
public partial class SecurityRole : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private string _key;
	
	private int _roleID;
	
	private EntitySet<AccountRole> _accountRoles;
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnKeyChanged();
		
		partial void OnKeyChanging(string value);
		
		partial void OnRoleIDChanged();
		
		partial void OnRoleIDChanging(int value);
		#endregion
	
	
	public SecurityRole()
	{
		_accountRoles = new EntitySet<AccountRole>(new Action<AccountRole>(this.AccountRoles_Attach), new Action<AccountRole>(this.AccountRoles_Detach));
		this.OnCreated();
	}
	
	[Column(Storage="_key", Name="key", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string Key
	{
		get
		{
			return this._key;
		}
		set
		{
			if (((_key == value) 
						== false))
			{
				this.OnKeyChanging(value);
				this.SendPropertyChanging();
				this._key = value;
				this.SendPropertyChanged("Key");
				this.OnKeyChanged();
			}
		}
	}
	
	[Column(Storage="_roleID", Name="roleId", DbType="int(10)", IsPrimaryKey=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int RoleID
	{
		get
		{
			return this._roleID;
		}
		set
		{
			if ((_roleID != value))
			{
				this.OnRoleIDChanging(value);
				this.SendPropertyChanging();
				this._roleID = value;
				this.SendPropertyChanged("RoleID");
				this.OnRoleIDChanged();
			}
		}
	}
	
	#region Children
	[Association(Storage="_accountRoles", OtherKey="RoleID", ThisKey="RoleID", Name="FK_account_roles_security_roles")]
	[DebuggerNonUserCode()]
	public EntitySet<AccountRole> AccountRoles
	{
		get
		{
			return this._accountRoles;
		}
		set
		{
			this._accountRoles = value;
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
	private void AccountRoles_Attach(AccountRole entity)
	{
		this.SendPropertyChanging();
		entity.SecurityRole = this;
	}
	
	private void AccountRoles_Detach(AccountRole entity)
	{
		this.SendPropertyChanging();
		entity.SecurityRole = null;
	}
	#endregion
}

[Table(Name="pd.settings")]
public partial class Setting : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _id;
	
	private string _key;
	
	private string _value;
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnIDChanged();
		
		partial void OnIDChanging(int value);
		
		partial void OnKeyChanged();
		
		partial void OnKeyChanging(string value);
		
		partial void OnValueChanged();
		
		partial void OnValueChanging(string value);
		#endregion
	
	
	public Setting()
	{
		this.OnCreated();
	}
	
	[Column(Storage="_id", Name="id", DbType="int(10)", IsPrimaryKey=true, IsDbGenerated=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int ID
	{
		get
		{
			return this._id;
		}
		set
		{
			if ((_id != value))
			{
				this.OnIDChanging(value);
				this.SendPropertyChanging();
				this._id = value;
				this.SendPropertyChanged("ID");
				this.OnIDChanged();
			}
		}
	}
	
	[Column(Storage="_key", Name="key", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string Key
	{
		get
		{
			return this._key;
		}
		set
		{
			if (((_key == value) 
						== false))
			{
				this.OnKeyChanging(value);
				this.SendPropertyChanging();
				this._key = value;
				this.SendPropertyChanged("Key");
				this.OnKeyChanged();
			}
		}
	}
	
	[Column(Storage="_value", Name="value", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string Value
	{
		get
		{
			return this._value;
		}
		set
		{
			if (((_value == value) 
						== false))
			{
				this.OnValueChanging(value);
				this.SendPropertyChanging();
				this._value = value;
				this.SendPropertyChanged("Value");
				this.OnValueChanged();
			}
		}
	}
	
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

[Table(Name="pd.settings_avatarmetrics")]
public partial class SettingsAvatarMetric : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _avatarMetricID;
	
	private string _key;
	
	private EntitySet<AvatarMetric> _avatarMetrics;
	
	private EntitySet<SettingsDefaultMetric> _settingsDefaultMetrics;
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAvatarMetricIDChanged();
		
		partial void OnAvatarMetricIDChanging(int value);
		
		partial void OnKeyChanged();
		
		partial void OnKeyChanging(string value);
		#endregion
	
	
	public SettingsAvatarMetric()
	{
		_avatarMetrics = new EntitySet<AvatarMetric>(new Action<AvatarMetric>(this.AvatarMetrics_Attach), new Action<AvatarMetric>(this.AvatarMetrics_Detach));
		_settingsDefaultMetrics = new EntitySet<SettingsDefaultMetric>(new Action<SettingsDefaultMetric>(this.SettingsDefaultMetrics_Attach), new Action<SettingsDefaultMetric>(this.SettingsDefaultMetrics_Detach));
		this.OnCreated();
	}
	
	[Column(Storage="_avatarMetricID", Name="avatarMetricId", DbType="int(10)", IsPrimaryKey=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int AvatarMetricID
	{
		get
		{
			return this._avatarMetricID;
		}
		set
		{
			if ((_avatarMetricID != value))
			{
				this.OnAvatarMetricIDChanging(value);
				this.SendPropertyChanging();
				this._avatarMetricID = value;
				this.SendPropertyChanged("AvatarMetricID");
				this.OnAvatarMetricIDChanged();
			}
		}
	}
	
	[Column(Storage="_key", Name="key", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string Key
	{
		get
		{
			return this._key;
		}
		set
		{
			if (((_key == value) 
						== false))
			{
				this.OnKeyChanging(value);
				this.SendPropertyChanging();
				this._key = value;
				this.SendPropertyChanged("Key");
				this.OnKeyChanged();
			}
		}
	}
	
	#region Children
	[Association(Storage="_avatarMetrics", OtherKey="MetricID", ThisKey="AvatarMetricID", Name="FK_avatar_metric_settings_avatarmetrics")]
	[DebuggerNonUserCode()]
	public EntitySet<AvatarMetric> AvatarMetrics
	{
		get
		{
			return this._avatarMetrics;
		}
		set
		{
			this._avatarMetrics = value;
		}
	}
	
	[Association(Storage="_settingsDefaultMetrics", OtherKey="MetricID", ThisKey="AvatarMetricID", Name="FK__settings_avatarmetrics")]
	[DebuggerNonUserCode()]
	public EntitySet<SettingsDefaultMetric> SettingsDefaultMetrics
	{
		get
		{
			return this._settingsDefaultMetrics;
		}
		set
		{
			this._settingsDefaultMetrics = value;
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
	private void AvatarMetrics_Attach(AvatarMetric entity)
	{
		this.SendPropertyChanging();
		entity.SettingsAvatarMetric = this;
	}
	
	private void AvatarMetrics_Detach(AvatarMetric entity)
	{
		this.SendPropertyChanging();
		entity.SettingsAvatarMetric = null;
	}
	
	private void SettingsDefaultMetrics_Attach(SettingsDefaultMetric entity)
	{
		this.SendPropertyChanging();
		entity.SettingsAvatarMetric = this;
	}
	
	private void SettingsDefaultMetrics_Detach(SettingsDefaultMetric entity)
	{
		this.SendPropertyChanging();
		entity.SettingsAvatarMetric = null;
	}
	#endregion
}

[Table(Name="pd.settings_defaultmetrics")]
public partial class SettingsDefaultMetric : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _metricID;
	
	private string _value;
	
	private EntityRef<SettingsAvatarMetric> _settingsAvatarMetric = new EntityRef<SettingsAvatarMetric>();
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnMetricIDChanged();
		
		partial void OnMetricIDChanging(int value);
		
		partial void OnValueChanged();
		
		partial void OnValueChanging(string value);
		#endregion
	
	
	public SettingsDefaultMetric()
	{
		this.OnCreated();
	}
	
	[Column(Storage="_metricID", Name="metricId", DbType="int(10)", IsPrimaryKey=true, AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int MetricID
	{
		get
		{
			return this._metricID;
		}
		set
		{
			if ((_metricID != value))
			{
				this.OnMetricIDChanging(value);
				this.SendPropertyChanging();
				this._metricID = value;
				this.SendPropertyChanged("MetricID");
				this.OnMetricIDChanged();
			}
		}
	}
	
	[Column(Storage="_value", Name="value", DbType="varchar(50)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public string Value
	{
		get
		{
			return this._value;
		}
		set
		{
			if (((_value == value) 
						== false))
			{
				this.OnValueChanging(value);
				this.SendPropertyChanging();
				this._value = value;
				this.SendPropertyChanged("Value");
				this.OnValueChanged();
			}
		}
	}
	
	#region Parents
	[Association(Storage="_settingsAvatarMetric", OtherKey="AvatarMetricID", ThisKey="MetricID", Name="FK__settings_avatarmetrics", IsForeignKey=true)]
	[DebuggerNonUserCode()]
	public SettingsAvatarMetric SettingsAvatarMetric
	{
		get
		{
			return this._settingsAvatarMetric.Entity;
		}
		set
		{
			if (((this._settingsAvatarMetric.Entity == value) 
						== false))
			{
				if ((this._settingsAvatarMetric.Entity != null))
				{
					SettingsAvatarMetric previousSettingsAvatarMetric = this._settingsAvatarMetric.Entity;
					this._settingsAvatarMetric.Entity = null;
					previousSettingsAvatarMetric.SettingsDefaultMetrics.Remove(this);
				}
				this._settingsAvatarMetric.Entity = value;
				if ((value != null))
				{
					value.SettingsDefaultMetrics.Add(this);
					_metricID = value.AvatarMetricID;
				}
				else
				{
					_metricID = default(int);
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
