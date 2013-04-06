<?php
	
	/**
	 * Basic Database Entity
	 */
	class DBEntity {
		public static $selector = '*';
		public static $table = '';
		
		protected static function query($qry){
			if(!isset($dbLink)){
				db();
			}
			$result = mysql_query($qry) or die(mysql_error());
			return $result;
		}
		
		/** Gets all records for the given condition **/
		protected static function first($condition){
			$query = DBEntity::query('SELECT ' . self::$selector . ' FROM ' . static::table() . ' WHERE ' . $condition);
			if($query == false){ return null; }
			$row = mysql_fetch_array($query, MYSQL_ASSOC);
			$result = new self();
			$result->init($row);
			
			return $result;
		}
		
		public static function table(){
			return 'NOT_A_REAL_TABLE';
		}
		
		public function init($row){
			foreach($row as $key => $value){
				$this->{$key} = $value;
			}
		}
		
		/**
		 * Create a new entity
		 */
		public function create(){
			$schema = static::schema();
			$cols = $schema['columns'];
			
			$colLabels = array();
			$colValues = array();
			
			foreach($cols as $col){
				if(isset($this->{$col})){
					array_push($colLabels, $col);
					$val = $this->{$col};
					if(is_string($val)){
						$val = safeSQLString($val);
					}
					array_push($colValues, $val);
				}
			}
			
			$result = DBEntity::query('INSERT INTO ' . $this->table() . ' (' . implode(',', $colLabels) . ') VALUES (' . implode(',', $colValues) . ')');
			
			if($result == true){
				/** 
				 * Re-select the record to get computed field values
				 */
				$insertID = mysql_insert_id();
				if($insertID != NULL){
					$this->{$schema->pk} = $insertID;
				}
				$this->reload();
				return true;
			}
			return false;
		}
		
		/**
		 * Get latest values
		 */
		public function reload(){
			$schema = static::schema();
			$pkValue = $this->{$schema['pk']};
			if(is_string($pkValue)){
				$pkValue = safeSQLString($pkValue);
			}
					
			$result = DBEntity::query('SELECT * FROM ' . $this->table() . ' WHERE ' . $schema['pk'] . ' = ' . $pkValue);
			if($result == false){ return false; }
			
			$row = mysql_fetch_array($result, MYSQL_ASSOC);
			$this->init($row);
			return true;
		}
		
		
	}
	
	/**
	 * User account
	 */
	class Account extends DBEntity {
		public static function table(){
			return 'account';
		}
		
		public static function fromKey($pk){
			return Account::first(sprintf("accountId='%s'", safeSQL($pk)));
		}
		
		public static function fromCredentials($user, $passHash){
			return Account::first('username=' . safeSQLString($user) . ' AND password=' . safeSQLString($passHash));
		}
	}
	
	/**
	 * Auth session
	 */
	class Session extends DBEntity {
		public $active = 1;
		
		public static function table(){
			return 'account_session';
		}
		public static function schema(){
			return array(
				'columns' => array(
					'sessionId', 
					'accountId', 
					'created', 
					'expires',
					'active'
				),
				'pk' => 'sessionId'
			);
		}
		
		public static function fromKey($pk){
			return Session::first("sessionId=" . safeSQLString($pk));
		}
		
		public static function clearForAccount($accID){
			return DBEntity::query('DELETE FROM account_session WHERE accountId=' . $accID);
		}
	}
	
	
?>