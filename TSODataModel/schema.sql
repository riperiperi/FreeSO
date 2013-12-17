DROP DATABASE IF EXISTS `tso`;
CREATE DATABASE `tso` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `tso`;
CREATE TABLE `account` (
  `AccountID` int(10) NOT NULL AUTO_INCREMENT,
  `AccountName` varchar(50) NOT NULL,
  `Password` varchar(200) NOT NULL,
  `NumCharacters` int(11) DEFAULT NULL,
  `Character1` int(11) DEFAULT NULL,
  `Character2` int(11) DEFAULT NULL,
  `Character3` int(11) DEFAULT NULL,
  PRIMARY KEY (`AccountID`),
  KEY `CharacterID_idx` (`Character1`,`Character2`,`Character3`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=latin1;
CREATE TABLE `character` (
  `CharacterID` int(10) NOT NULL,
  `AccountID` int(11) NOT NULL AUTO_INCREMENT,
  `GUID` varchar(36) NOT NULL DEFAULT '0',
  `LastCached` varchar(50) NOT NULL DEFAULT '0',
  `Name` varchar(50) NOT NULL DEFAULT '0',
  `Sex` varchar(50) NOT NULL DEFAULT '0',
  `Description` varchar(45) NOT NULL,
  `City` varchar(50) NOT NULL DEFAULT '0',
  `HeadOutfitID` bigint(20) NOT NULL,
  `BodyOutfitID` bigint(20) NOT NULL,
  `AppearanceType` int(11) NOT NULL,
  PRIMARY KEY (`CharacterID`),
  UNIQUE KEY `Name` (`Name`,`CharacterID`),
  UNIQUE KEY `GUID` (`GUID`),
  KEY `FK_character_account` (`AccountID`),
  CONSTRAINT `FK_character_account` FOREIGN KEY (`AccountID`) REFERENCES `account` (`AccountID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

DROP DATABASE IF EXISTS `tso`;
CREATE DATABASE `tsocity` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `tsocity`;
CREATE TABLE `account` (
  `AccountID` int(10) NOT NULL AUTO_INCREMENT,
  `AccountName` varchar(50) NOT NULL,
  `Password` varchar(200) NOT NULL,
  `NumCharacters` int(11) DEFAULT NULL,
  `Character1` int(11) DEFAULT NULL,
  `Character2` int(11) DEFAULT NULL,
  `Character3` int(11) DEFAULT NULL,
  PRIMARY KEY (`AccountID`),
  KEY `CharacterID_idx` (`Character1`,`Character2`,`Character3`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=latin1;
CREATE TABLE `character` (
  `CharacterID` int(10) NOT NULL,
  `AccountID` int(11) NOT NULL AUTO_INCREMENT,
  `GUID` varchar(36) NOT NULL DEFAULT '0',
  `LastCached` varchar(50) NOT NULL DEFAULT '0',
  `Name` varchar(50) NOT NULL DEFAULT '0',
  `Sex` varchar(50) NOT NULL DEFAULT '0',
  `Description` varchar(45) NOT NULL,
  `City` varchar(50) NOT NULL DEFAULT '0',
  `HeadOutfitID` bigint(20) NOT NULL,
  `BodyOutfitID` bigint(20) NOT NULL,
  `AppearanceType` int(11) NOT NULL,
  PRIMARY KEY (`CharacterID`),
  UNIQUE KEY `Name` (`Name`,`CharacterID`),
  UNIQUE KEY `GUID` (`GUID`),
  KEY `FK_character_account` (`AccountID`),
  CONSTRAINT `FK_character_account` FOREIGN KEY (`AccountID`) REFERENCES `account` (`AccountID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

