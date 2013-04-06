<?php
	
	/**
	 * This services validates login credentials,
	 * Input:
	 *		{user: "", pass: ""}
	 *
	 * Response Wrapper Body:
	 *		{token: ..., expires: ...}
	 */
	
	include_once("utils.php");
	include_once("model.php");
	
	/** Validate **/
	$username = requireParam("user");
	$password = requireParam("pass");
	$passwordHash = md5(md5($password) . $salt);
	
	/** Check the DB for this user / pass combination **/
	$account = Account::fromCredentials($username, $passwordHash);
	
	if($account == false){
		error('User not found', 600);
	}
	if($account->status != 1){
		error('Account suspended', 601);
	}
	
	/** Kill old sessions **/
	Session::clearForAccount($account->accountId);
	
	
	/** Create a session **/
	$session = new Session;
	$session->sessionId = gen_uuid();
	$session->accountId = $account->accountId;
	$session->expires = sqlDate(time() + 1200);
	
	if($session->create())
	{
		success(array(
			'valid' => true,
			'uid' => $account->uuid,
			'sessionID' => $session->sessionId,
			'sessionStart' => $session->created,
			'sessionEnd' => $session->expires
		));	
	}else{
		error('Failed to create session', 602);
	}
	
	
	
?>