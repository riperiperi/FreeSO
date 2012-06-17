<?php
/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is getmanifest.php.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

if(isset($_GET['Manifest']))
{
	$RequestedManifest = htmlspecialchars($_GET['Manifest']);
	$FoundManifest = "";

	if($handle = opendir('./patches/'))
	{
		//Find the requested manifest...
		while (false !== ($entry = readdir($handle)))
		{
        		if($entry === $RequestedManifest)
			{
				$FoundManifest = './patches/' . $entry;
				break;
			}
		}

		if(empty($FoundManifest) || strcmp($FoundManifest, './patches') == 0)
		{
			header('Content-Description: Invalid Request');
			header('Content-Type: text/html; charset=utf-8');
			echo('<html><body><p>No manifest by that name exists!</p></body></html>');
		}
		else
		{
			header('Content-Description: File Transfer');
    			header('Content-Type: application/octet-stream');
    			header('Content-Disposition: attachment; filename='.basename($FoundManifest));
    			header('Content-Transfer-Encoding: binary');
    			header('Expires: 0');
    			header('Cache-Control: must-revalidate, post-check=0, pre-check=0');
    			header('Pragma: public');
    			header('Content-Length: ' . filesize($FoundManifest));
    			ob_clean();
    			flush();
    			readfile($FoundManifest);

			exit();
		}
	}
}
else
{
	header('Content-Description: Invalid Request');
	header('Content-Type: text/html; charset=utf-8');
	echo('<html><body><p>Invalid request!</p></body></html>');
}
?>