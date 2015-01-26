// 
//  ____  _     __  __      _        _ 
// |  _ \| |__ |  \/  | ___| |_ __ _| |
// | | | | '_ \| |\/| |/ _ \ __/ _` | |
// | |_| | |_) | |  | |  __/ || (_| | |
// |____/|_.__/|_|  |_|\___|\__\__,_|_|
//
// Auto-generated from tsocity on 2014-11-13 12:12:25Z.
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


public partial class TSoCity : DataContext
{
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		#endregion
	
	
	public TSoCity(string connectionString) : 
			base(connectionString)
	{
		this.OnCreated();
	}
	
	public TSoCity(string connection, MappingSource mappingSource) : 
			base(connection, mappingSource)
	{
		this.OnCreated();
	}
	
	public TSoCity(IDbConnection connection, MappingSource mappingSource) : 
			base(connection, mappingSource)
	{
		this.OnCreated();
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

public partial class TSoCity
{
	
	public TSoCity(IDbConnection connection) : 
			base(connection)
	{
		this.OnCreated();
	}
}
#region End MONO_STRICT
	#endregion
#else     // MONO_STRICT

public partial class TSoCity
{
	
	public TSoCity(IDbConnection connection) : 
			base(connection, new DbLinq.MySql.MySqlVendor())
	{
		this.OnCreated();
	}
	
	public TSoCity(IDbConnection connection, IVendor sqlDialect) : 
			base(connection, sqlDialect)
	{
		this.OnCreated();
	}
	
	public TSoCity(IDbConnection connection, MappingSource mappingSource, IVendor sqlDialect) : 
			base(connection, mappingSource, sqlDialect)
	{
		this.OnCreated();
	}
}
#region End Not MONO_STRICT
	#endregion
#endif     // MONO_STRICT
#endregion

[Table(Name="tsocity.character")]
public partial class Character : System.ComponentModel.INotifyPropertyChanging, System.ComponentModel.INotifyPropertyChanged
{
	
	private static System.ComponentModel.PropertyChangingEventArgs emptyChangingEventArgs = new System.ComponentModel.PropertyChangingEventArgs("");
	
	private int _accountID;
	
	private int _appearanceType;
	
	private long _bodyOutfitID;
	
	private int _characterID;
	
	private string _description;
	
	private System.Guid _guid;
	
	private long _headOutfitID;
	
	private System.DateTime _lastCached;
	
	private string _name;
	
	private string _sex;
	
	#region Extensibility Method Declarations
		partial void OnCreated();
		
		partial void OnAccountIDChanged();
		
		partial void OnAccountIDChanging(int value);
		
		partial void OnAppearanceTypeChanged();
		
		partial void OnAppearanceTypeChanging(int value);
		
		partial void OnBodyOutfitIDChanged();
		
		partial void OnBodyOutfitIDChanging(long value);
		
		partial void OnCharacterIDChanged();
		
		partial void OnCharacterIDChanging(int value);
		
		partial void OnDescriptionChanged();
		
		partial void OnDescriptionChanging(string value);
		
		partial void OnGUIDChanged();
		
		partial void OnGUIDChanging(System.Guid value);
		
		partial void OnHeadOutfitIDChanged();
		
		partial void OnHeadOutfitIDChanging(long value);
		
		partial void OnLastCachedChanged();
		
		partial void OnLastCachedChanging(System.DateTime value);
		
		partial void OnNameChanged();
		
		partial void OnNameChanging(string value);
		
		partial void OnSexChanged();
		
		partial void OnSexChanging(string value);
		#endregion
	
	
	public Character()
	{
		this.OnCreated();
	}
	
	[Column(Storage="_accountID", Name="AccountID", DbType="int(10)", AutoSync=AutoSync.Never, CanBeNull=false)]
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
	
	[Column(Storage="_appearanceType", Name="AppearanceType", DbType="int", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public int AppearanceType
	{
		get
		{
			return this._appearanceType;
		}
		set
		{
			if ((_appearanceType != value))
			{
				this.OnAppearanceTypeChanging(value);
				this.SendPropertyChanging();
				this._appearanceType = value;
				this.SendPropertyChanged("AppearanceType");
				this.OnAppearanceTypeChanged();
			}
		}
	}
	
	[Column(Storage="_bodyOutfitID", Name="BodyOutfitID", DbType="bigint(20)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public long BodyOutfitID
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
				this.SendPropertyChanging();
				this._bodyOutfitID = value;
				this.SendPropertyChanged("BodyOutfitID");
				this.OnBodyOutfitIDChanged();
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
	
	[Column(Storage="_description", Name="Description", DbType="varchar(400)", AutoSync=AutoSync.Never, CanBeNull=false)]
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
	
	[Column(Storage="_headOutfitID", Name="HeadOutfitID", DbType="bigint(20)", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public long HeadOutfitID
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
				this.SendPropertyChanging();
				this._headOutfitID = value;
				this.SendPropertyChanged("HeadOutfitID");
				this.OnHeadOutfitIDChanged();
			}
		}
	}
	
	[Column(Storage="_lastCached", Name="LastCached", DbType="datetime", AutoSync=AutoSync.Never, CanBeNull=false)]
	[DebuggerNonUserCode()]
	public System.DateTime LastCached
	{
		get
		{
			return this._lastCached;
		}
		set
		{
			if ((_lastCached != value))
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
