DROP DATABASE IF EXISTS `tso`;
CREATE DATABASE `tso` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `tso`;

CREATE TABLE `account` (
  `AccountID` int(10) NOT NULL AUTO_INCREMENT,
  `AccountName` varchar(50) NOT NULL,
  `Password` varchar(200) NOT NULL,
  PRIMARY KEY (`AccountID`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=latin1;

CREATE TABLE `character` (
  `CharacterID` int(10) NOT NULL AUTO_INCREMENT,
  `AccountID` int(10) NOT NULL,
  `GUID` varchar(36) NOT NULL DEFAULT '0',
  `LastCached` varchar(50) NOT NULL DEFAULT '0',
  `Name` varchar(50) NOT NULL DEFAULT '0',
  `Sex` varchar(50) NOT NULL DEFAULT '0',
  `Description` varchar(400) NOT NULL,
  `HeadOutfitID` bigint(20) NOT NULL,
  `BodyOutfitID` bigint(20) NOT NULL,
  `AppearanceType` int(11) NOT NULL,
 PRIMARY KEY (`CharacterID`),
  UNIQUE KEY `Name` (`Name`,`CharacterID`),
  UNIQUE KEY `GUID` (`GUID`),
  KEY `FK_character_account` (`AccountID`),
  CONSTRAINT `FK_character_account` FOREIGN KEY (`AccountID`) REFERENCES `account` (`AccountID`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=latin1;

DROP DATABASE IF EXISTS `tsocity`;
CREATE DATABASE `tsocity` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `tsocity`;

CREATE TABLE `character` (
  `CharacterID` int(10) NOT NULL AUTO_INCREMENT,
  `AccountID` int(10) NOT NULL,
  `GUID` varchar(36) NOT NULL DEFAULT '0',
  `LastCached` varchar(50) NOT NULL DEFAULT '0',
  `Name` varchar(50) NOT NULL DEFAULT '0',
  `Sex` varchar(50) NOT NULL DEFAULT '0',
  `Description` varchar(400) NOT NULL,
  `HeadOutfitID` bigint(20) NOT NULL,
  `BodyOutfitID` bigint(20) NOT NULL,
  `AppearanceType` int(11) NOT NULL,

 PRIMARY KEY (`CharacterID`),
  UNIQUE KEY `Name` (`Name`,`CharacterID`),
  UNIQUE KEY `GUID` (`GUID`)
) ENGINE=InnoDB AUTO_INCREMENT=55 DEFAULT CHARSET=latin1;


