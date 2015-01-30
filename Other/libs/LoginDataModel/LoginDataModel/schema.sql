use tso;

CREATE TABLE `account` (
  `AccountID` int(10) NOT NULL AUTO_INCREMENT,
  `AccountName` varchar(50) NOT NULL,
  `Password` varchar(200) NOT NULL,
  `NumCharacters` int(11) DEFAULT NULL,
  PRIMARY KEY (`AccountID`)
) ENGINE=InnoDB AUTO_INCREMENT=778 DEFAULT CHARSET=utf8;

CREATE TABLE `house` (
  `HouseID` int(11) NOT NULL AUTO_INCREMENT,
  `X` int(11) NOT NULL,
  `Y` int(11) NOT NULL,
  `Flags` tinyint(4) NOT NULL,
  PRIMARY KEY (`HouseID`),
  KEY `GUID` (`HouseID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

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
  `City` varchar(50) NOT NULL DEFAULT 'c78d14c4-a153-4a44-8b5e-f0c602e50f95',
  `CityName` varchar(65) NOT NULL DEFAULT 'East Jerome',
  `CityThumb` bigint(20) NOT NULL DEFAULT '197158417324972',
  `CityMap` bigint(20) NOT NULL DEFAULT '10531259809793',
  `CityIP` varchar(16) NOT NULL DEFAULT '173.248.136.133',
  `CityPort` int(11) NOT NULL DEFAULT '2107',
  `House` int(11) DEFAULT NULL,
  PRIMARY KEY (`CharacterID`),
  UNIQUE KEY `Name` (`Name`,`CharacterID`),
  UNIQUE KEY `GUID` (`GUID`),
  KEY `House_idx` (`House`),
  CONSTRAINT `House` FOREIGN KEY (`House`) REFERENCES `house` (`HouseID`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1499 DEFAULT CHARSET=utf8;

use tsocity;

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
