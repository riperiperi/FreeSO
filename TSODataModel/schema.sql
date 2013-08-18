-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               5.5.27-log - MySQL Community Server (GPL)
-- Server OS:                    Win32
-- HeidiSQL version:             7.0.0.4053
-- Date/time:                    2013-08-18 13:26:18
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!40014 SET FOREIGN_KEY_CHECKS=0 */;

-- Dumping database structure for tso
DROP DATABASE IF EXISTS `tso`;
CREATE DATABASE IF NOT EXISTS `tso` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `tso`;


-- Dumping structure for table tso.account
DROP TABLE IF EXISTS `account`;
CREATE TABLE IF NOT EXISTS `account` (
  `AccountID` int(10) NOT NULL AUTO_INCREMENT,
  `AccountName` varchar(50) NOT NULL,
  `Password` varchar(200) NOT NULL,
  PRIMARY KEY (`AccountID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.


-- Dumping structure for table tso.character
DROP TABLE IF EXISTS `character`;
CREATE TABLE IF NOT EXISTS `character` (
  `CharacterID` int(10) NOT NULL AUTO_INCREMENT,
  `AccountID` int(11) NOT NULL DEFAULT '0',
  `GUID` varchar(36) NOT NULL DEFAULT '0',
  `LastCached` varchar(50) NOT NULL DEFAULT '0',
  `Name` varchar(50) NOT NULL DEFAULT '0',
  `Sex` varchar(50) NOT NULL DEFAULT '0',
  `City` varchar(50) NOT NULL DEFAULT '0',
  PRIMARY KEY (`CharacterID`),
  UNIQUE KEY `Name` (`Name`,`CharacterID`),
  UNIQUE KEY `GUID` (`GUID`),
  KEY `FK_character_account` (`AccountID`),
  CONSTRAINT `FK_character_account` FOREIGN KEY (`AccountID`) REFERENCES `account` (`AccountID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.
/*!40014 SET FOREIGN_KEY_CHECKS=1 */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
