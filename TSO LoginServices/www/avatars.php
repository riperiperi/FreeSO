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
	$session = validateSession();
	$avatars = Avatar::fromAccount($session->accountId);
	
	$serviceResult = array();
	foreach($avatars as $row){
		array_push($serviceResult, array(
			'id' => $row->avatarId,
			'name' => $row->name,
			'description' => $row->description,
			'uuid' => $row->uuid,
			'cityId' => $row->cityId,
			'status' => intval($row->status)
		));
	}
	
	success(array(
		'avatars' => $serviceResult
	));
	
	
	
?>