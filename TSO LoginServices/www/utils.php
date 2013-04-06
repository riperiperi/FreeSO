<?php
	include_once("settings.php");
	
	
	function db(){
		global $dbPath, $dbName, $dbUser, $dbPass, $dbLink;
		
		$result = mysql_connect($dbPath, $dbUser, $dbPass) or die("<p>Error connecting to the database<br /><strong>" . mysql_error() ."</strong></p>" );
		$dbLink = $result;
		
		mysql_select_db($dbName) or die("<p>Error selecting the database<br />" . mysql_error() . "</strong></p>");
		return $result;
	}
	
	function dbFirst($query){
		db();
		try {
			$result = mysql_query($query) or die(mysql_error());
			$row = mysql_fetch_assoc($result);
			return $row;
		}catch(Exception $e){
		}
	}
	
	function dbAll($query){
		db();
		try {
			$resultArray = array();
			
			$result = mysql_query($query) or die(mysql_error());
			while ($row = mysql_fetch_assoc($result)){
				array_push($resultArray, $row);
			}
			return $resultArray;
		}catch(Exception $e){
		}
	}
	
	function safeSQL($txt){
		return $txt;
	}
	
	function safeSQLString($txt){
		return "'" . $txt . "'";
	}
	
	function sqlDate($stamp){
		return date('Y-m-d H:i:s', $stamp);
	}
	
	function getJSON(){
		global $_jsonBody;
		
		if(isset($_jsonBody)){ return $_jsonBody; }
		
		$requestBody = @file_get_contents('php://input');
		$requestBody = json_decode($requestBody);
		$_jsonBody = $requestBody;
		return $requestBody;
	}
	
	function requireParam($argName){
		$body = getJSON();
		if(!isset($body) || !property_exists($body, $argName)){
			error('Missing parameter: ' . $argName, 501);
			return;
		}
		return $body->$argName;
	}
	
	function validateSession(){
		if(!isset($_GET['auth'])){
			error('Missing parameter: auth', 501);
		}
		$session = Session::fromKey($_GET['auth']);
		if($session == null){
			error('Invalid auth token', 600);
		}
		
		return $session;
	}
	
	function success($body){
		header('Content-Type: text/json');
		
		$response = array(
			'status' => 'ok',
			'body' => $body
		);
		
		echo json_encode($response);
		exit();
	}
	
	function error($msg, $code){
		header('Content-Type: text/json');
		
		$response = array(
			'status' => 'error',
			'errors' => array(
				array(
					'msg' => $msg,
					'code' => $code
				)
			)
		);
		
		echo json_encode($response);
		exit();
	}
	
	function gen_uuid() {
		return sprintf( '%04x%04x-%04x-%04x-%04x-%04x%04x%04x',
			// 32 bits for "time_low"
			mt_rand( 0, 0xffff ), mt_rand( 0, 0xffff ),

			// 16 bits for "time_mid"
			mt_rand( 0, 0xffff ),

			// 16 bits for "time_hi_and_version",
			// four most significant bits holds version number 4
			mt_rand( 0, 0x0fff ) | 0x4000,

			// 16 bits, 8 bits for "clk_seq_hi_res",
			// 8 bits for "clk_seq_low",
			// two most significant bits holds zero and one for variant DCE1.1
			mt_rand( 0, 0x3fff ) | 0x8000,

			// 48 bits for "node"
			mt_rand( 0, 0xffff ), mt_rand( 0, 0xffff ), mt_rand( 0, 0xffff )
		);
	}
	
	
?>