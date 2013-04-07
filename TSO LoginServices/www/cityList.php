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
	
	/**
	 * Get a list of cities & their MOTD
	 */
	$query = 'SELECT *, (SELECT motdId FROM city_motd motd WHERE motd.cityId = c.cityId AND motd.expires > CURRENT_TIMESTAMP ORDER BY motd.created DESC LIMIT 1) as MOTDID FROM city c';
	$result = dbAll($query);
	
	$serviceResult = array();
	foreach($result as $row){
		$motd = array();
		if($row['MOTDID'] != NULL){
			$motdRow = dbFirst('SELECT * FROM city_motd WHERE motdId=' . $row['MOTDID']);
			array_push($motd, array(
				'from' => $motdRow['from'],
				'subject' => $motdRow['subject'],
				'body' => $motdRow['body']
			));
		}
		array_push($serviceResult, array(
			'id' => $row['cityId'],
			'name' => $row['name'],
			'uuid' => $row['uuid'],
			'map' => $row['mapId'],
			'online' => $row['online'] == '1',
			'status' => intval($row['status']),
			'motd' => $motd
		));
	}
	
	success(array(
		'cities' => $serviceResult
	));
	
?>