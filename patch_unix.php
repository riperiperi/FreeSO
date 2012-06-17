<?php

/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is patch.php.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

include 'createzip.php';

if(isset($_GET['Version']))
{
	$ClientVersion = intval(htmlspecialchars($_GET['Version']));
	$NewManifest = "";

	if($handle = opendir(realpath('./patches/')))
	{
		//Find out if there are any manifests newer than the client's version.
		while (false !== ($entry = readdir($handle)))
		{
        		if(intval(str_replace('.manifest', '', $entry)) > $ClientVersion)
			{
				$NewManifest = realpath('./patches') . '/' . $entry . '.manifest';
				break;
			}
		}

		if(empty($NewManifest) || strcmp($NewManifest, realpath('./patches')) == 0)
		{
			header('Content-Description: No New Manifest');
			header('Content-Type: text/html; charset=utf-8');
			echo('<html><body><p>No new manifest!</p></body></html>');
		}
		else
		{
			//Open the new manifest and check if it has a child, which means an
			//incremental update needs to take place...
			$FileHandle = fopen($NewManifest, 'r');

			$Parent = fgets($FileHandle);
			$CurrentChild = fgets($FileHandle);
			$Path = realpath('./patches');

			$Parent = str_replace('Parent=', '', $Parent);
			$Parent = str_replace('"', '', $Parent);
			$Parent = trim($Parent);
			$CurrentChild = str_replace('Child=', '', $CurrentChild);
			$CurrentChild = str_replace('"', '', $CurrentChild);
			$CurrentChild = trim($CurrentChild);

			fclose($FileHandle);
			
			//This is a compromise. Instead of checking for all childs, just check for the first one,
			//and let the client handle the other children (if any).
			if(empty($CurrentChild) !== true && empty($Parent) !== true)
			{
				$Parent = $Path . '/' . $Parent;
				$CurrentChild = $Path . '/' . $CurrentChild;

				$FilesToZip = array($CurrentChild, $NewManifest);
				$ZipName = './' . strval(rand()) . '.zip';
				create_zip($FilesToZip, $ZipName);
				
				//Client should check the Content-Description header to figure out what to expect.
				header('Content-Description: Zipped File Transfer');
    				header('Content-Type: application/octet-stream');
    				header('Content-Disposition: attachment; filename='.basename($ZipName));
    				header('Content-Transfer-Encoding: binary');
    				header('Expires: 0');
    				header('Cache-Control: must-revalidate, post-check=0, pre-check=0');
    				header('Pragma: public');
    				header('Content-Length: ' . filesize($ZipName));
    				ob_clean();
    				flush();
    				readfile($ZipName);

				unlink($ZipName);
				exit();
			}
			//There was a child, but no parent.
			else if(empty($CurrentChild) !== true && empty($Parent) == true)
			{
				$CurrentChild = $Path . '/' . $CurrentChild;

				$FilesToZip = array($CurrentChild, $NewManifest);
				$ZipName = './' . strval(rand()) . '.zip';
				create_zip($FilesToZip, $ZipName);
				
				//Client should check the Content-Description header to figure out what to expect.
				header('Content-Description: Zipped File Transfer');
    				header('Content-Type: application/octet-stream');
    				header('Content-Disposition: attachment; filename='.basename($ZipName));
    				header('Content-Transfer-Encoding: binary');
    				header('Expires: 0');
    				header('Cache-Control: must-revalidate, post-check=0, pre-check=0');
    				header('Pragma: public');
    				header('Content-Length: ' . filesize($ZipName));
    				ob_clean();
    				flush();
    				readfile($ZipName);

				unlink($ZipName);
				exit();
			}
			//There was a parent, but no child.
			else if(empty($Parent) !== true && empty($CurrentChild) == true)
			{
				$Parent = $Path . '/' . $Parent;

				$FilesToZip = array($Parent, $NewManifest);
				$ZipName = './' . strval(rand()) . '.zip';
				create_zip($FilesToZip, $ZipName);
				
				//Client should check the Content-Description header to figure out what to expect.
				header('Content-Description: Zipped File Transfer');
    				header('Content-Type: application/octet-stream');
    				header('Content-Disposition: attachment; filename='.basename($ZipName));
    				header('Content-Transfer-Encoding: binary');
    				header('Expires: 0');
    				header('Cache-Control: must-revalidate, post-check=0, pre-check=0');
    				header('Pragma: public');
    				header('Content-Length: ' . filesize($ZipName));
    				ob_clean();
    				flush();
    				readfile($ZipName);

				unlink($ZipName);
				exit();
			}
			//There was neither a parent nor a child. This should really never occur.
			else
			{
				header('Content-Description: File Transfer');
    				header('Content-Type: application/octet-stream');
    				header('Content-Disposition: attachment; filename='.basename($NewManifest));
    				header('Content-Transfer-Encoding: binary');
    				header('Expires: 0');
    				header('Cache-Control: must-revalidate, post-check=0, pre-check=0');
    				header('Pragma: public');
    				header('Content-Length: ' . filesize($NewManifest));
    				ob_clean();
    				flush();
    				readfile($NewManifest);

				exit();
			}
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