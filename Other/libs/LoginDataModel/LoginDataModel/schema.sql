CREATE TABLE `account` (
  `AccountID` int(10) NOT NULL AUTO_INCREMENT,
  `AccountName` varchar(50) NOT NULL,
  `Password` varchar(200) NOT NULL,
  `NumCharacters` int(11) DEFAULT NULL,
  PRIMARY KEY (`AccountID`)
) ENGINE=InnoDB AUTO_INCREMENT=300 DEFAULT CHARSET=utf8;

CREATE TABLE `character` (
  `CharacterID` int(10) NOT NULL,
  `AccountID` int(10) NOT NULL,
  `GUID` varchar(36) NOT NULL DEFAULT '0',
  `LastCached` datetime NOT NULL,
  `Name` varchar(50) NOT NULL DEFAULT '0',
  `Sex` varchar(50) NOT NULL DEFAULT '0',
  `Description` varchar(400) NOT NULL,
  `HeadOutfitID` bigint(20) NOT NULL,
  `BodyOutfitID` bigint(20) NOT NULL,
  `AppearanceType` int(11) NOT NULL,
  `City` varchar(50) DEFAULT '0',
  `CityName` varchar(65) DEFAULT NULL,
  `CityThumb` bigint(20) DEFAULT NULL,
  `CityMap` bigint(20) DEFAULT NULL,
  `CityIP` varchar(16) DEFAULT NULL,
  `CityPort` int(11) DEFAULT NULL,
  `House` int(11) DEFAULT NULL,
  PRIMARY KEY (`CharacterID`),
  UNIQUE KEY `Name` (`Name`,`CharacterID`),
  UNIQUE KEY `GUID` (`GUID`),
  KEY `FK_character_account` (`AccountID`),
  KEY `FK_character_house_idx` (`House`),
  CONSTRAINT `FK_character_house` FOREIGN KEY (`House`) REFERENCES `house` (`GUID`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_character_account` FOREIGN KEY (`AccountID`) REFERENCES `account` (`AccountID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

CREATE TABLE `house` (
  `GUID` int(11) NOT NULL DEFAULT '0',
  `X` int(11) NOT NULL,
  `Y` int(11) NOT NULL,
  `ThumbnailID` int(11) NOT NULL,
  PRIMARY KEY (`GUID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
USE tsocity;

CREATE TABLE `character` (
  `CharacterID` int(10) NOT NULL AUTO_INCREMENT,
  `AccountID` int(10) NOT NULL,
  `GUID` varchar(36) NOT NULL DEFAULT '0',
  `LastCached` datetime NOT NULL,
  `Name` varchar(50) NOT NULL DEFAULT '0',
  `Sex` varchar(50) NOT NULL DEFAULT '0',
  `Description` varchar(400) NOT NULL,
  `HeadOutfitID` bigint(20) NOT NULL,
  `BodyOutfitID` bigint(20) NOT NULL,
  `AppearanceType` int(11) NOT NULL,
  PRIMARY KEY (`CharacterID`),
  UNIQUE KEY `Name` (`Name`,`CharacterID`),
  UNIQUE KEY `GUID` (`GUID`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8;
