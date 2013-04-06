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
	
	success(array(
		'abc' => 'abc'
	));
	
	var_dump($session);
	
	
	
?>