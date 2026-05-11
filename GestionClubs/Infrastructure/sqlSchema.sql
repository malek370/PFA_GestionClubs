-- Script Date: 09/05/2026 11:31  - ErikEJ.SqlCeScripting version 3.5.2.103
-- Database information:
-- Database: C:\Users\mbokri\Desktop\PFA\PFA_GestionClubs\GestionClubs\Infrastructure\gestionclubs.db
-- ServerVersion: 3.46.1
-- DatabaseSize: 88 KB
-- Created: 01/05/2026 23:47

-- User Table information:
-- Number of tables: 9
-- __EFMigrationsHistory: -1 row(s)
-- __EFMigrationsLock: -1 row(s)
-- Adhesions: -1 row(s)
-- Annoucements: -1 row(s)
-- Clubs: -1 row(s)
-- Events: -1 row(s)
-- EventUser: -1 row(s)
-- Members: -1 row(s)
-- Users: -1 row(s)

SELECT 1;
PRAGMA foreign_keys=OFF;
BEGIN TRANSACTION;
CREATE TABLE [Users] (
  [Id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [Email] text NOT NULL
, [FirstName] text NOT NULL
, [LastName] text NOT NULL
, [CreatinDate] text NOT NULL
);
CREATE TABLE [Clubs] (
  [Id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [Name] text NOT NULL
, [Description] text NOT NULL
, [Documents] text NOT NULL
, [CreatinDate] text NOT NULL
);
CREATE TABLE [Events] (
  [Id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [ClubId] bigint NOT NULL
, [Title] text NOT NULL
, [Description] text NOT NULL
, [IsPublic] bigint NOT NULL
, [Location] text NULL
, [StartDate] text NOT NULL
, [Tags] text NOT NULL
, [CreatinDate] text NOT NULL
, CONSTRAINT [FK_Events_0_0] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE CASCADE ON UPDATE NO ACTION
);
CREATE TABLE [EventUser] (
  [EventsId] bigint NOT NULL
, [ParticipentId] bigint NOT NULL
, CONSTRAINT [sqlite_autoindex_EventUser_1] PRIMARY KEY ([EventsId],[ParticipentId])
, CONSTRAINT [FK_EventUser_0_0] FOREIGN KEY ([ParticipentId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE ON UPDATE NO ACTION
, CONSTRAINT [FK_EventUser_1_0] FOREIGN KEY ([EventsId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE ON UPDATE NO ACTION
);
CREATE TABLE [Members] (
  [Id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [ClubId] bigint NOT NULL
, [CreatinDate] text NOT NULL
, [PostInClub] bigint NOT NULL
, [UserId] bigint NOT NULL
, CONSTRAINT [FK_Members_0_0] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE ON UPDATE NO ACTION
, CONSTRAINT [FK_Members_1_0] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE CASCADE ON UPDATE NO ACTION
);
CREATE TABLE [Annoucements] (
  [Id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [ClubId] bigint NOT NULL
, [Title] text NOT NULL
, [Content] text NOT NULL
, [IsPublic] bigint NOT NULL
, [CreatinDate] text NOT NULL
, CONSTRAINT [FK_Annoucements_0_0] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE CASCADE ON UPDATE NO ACTION
);
CREATE TABLE [Adhesions] (
  [Id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
, [ClubId] bigint NOT NULL
, [CreatinDate] text NOT NULL
, [Status] bigint NOT NULL
, [UserId] bigint NOT NULL
, CONSTRAINT [FK_Adhesions_0_0] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE CASCADE ON UPDATE NO ACTION
, CONSTRAINT [FK_Adhesions_1_0] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE ON UPDATE NO ACTION
);
CREATE INDEX [IX_Events_ClubId] ON [Events] ([ClubId] ASC);
CREATE INDEX [IX_EventUser_ParticipentId] ON [EventUser] ([ParticipentId] ASC);
CREATE INDEX [IX_Members_UserId] ON [Members] ([UserId] ASC);
CREATE INDEX [IX_Members_ClubId] ON [Members] ([ClubId] ASC);
CREATE INDEX [IX_Annoucements_ClubId] ON [Annoucements] ([ClubId] ASC);
CREATE INDEX [IX_Adhesions_ClubId] ON [Adhesions] ([ClubId] ASC);
CREATE INDEX [IX_Adhesions_UserId] ON [Adhesions] ([UserId] ASC);
CREATE TRIGGER [fki_Events_ClubId_Clubs_Id] BEFORE Insert ON [Events] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table Events violates foreign key constraint FK_Events_0_0') WHERE NOT EXISTS (SELECT * FROM Clubs WHERE  Id = NEW.ClubId); END;
CREATE TRIGGER [fku_Events_ClubId_Clubs_Id] BEFORE Update ON [Events] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table Events violates foreign key constraint FK_Events_0_0') WHERE NOT EXISTS (SELECT * FROM Clubs WHERE  Id = NEW.ClubId); END;
CREATE TRIGGER [fki_EventUser_ParticipentId_Users_Id] BEFORE Insert ON [EventUser] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table EventUser violates foreign key constraint FK_EventUser_0_0') WHERE NOT EXISTS (SELECT * FROM Users WHERE  Id = NEW.ParticipentId); END;
CREATE TRIGGER [fku_EventUser_ParticipentId_Users_Id] BEFORE Update ON [EventUser] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table EventUser violates foreign key constraint FK_EventUser_0_0') WHERE NOT EXISTS (SELECT * FROM Users WHERE  Id = NEW.ParticipentId); END;
CREATE TRIGGER [fki_EventUser_EventsId_Events_Id] BEFORE Insert ON [EventUser] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table EventUser violates foreign key constraint FK_EventUser_1_0') WHERE NOT EXISTS (SELECT * FROM Events WHERE  Id = NEW.EventsId); END;
CREATE TRIGGER [fku_EventUser_EventsId_Events_Id] BEFORE Update ON [EventUser] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table EventUser violates foreign key constraint FK_EventUser_1_0') WHERE NOT EXISTS (SELECT * FROM Events WHERE  Id = NEW.EventsId); END;
CREATE TRIGGER [fki_Members_UserId_Users_Id] BEFORE Insert ON [Members] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table Members violates foreign key constraint FK_Members_0_0') WHERE NOT EXISTS (SELECT * FROM Users WHERE  Id = NEW.UserId); END;
CREATE TRIGGER [fku_Members_UserId_Users_Id] BEFORE Update ON [Members] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table Members violates foreign key constraint FK_Members_0_0') WHERE NOT EXISTS (SELECT * FROM Users WHERE  Id = NEW.UserId); END;
CREATE TRIGGER [fki_Members_ClubId_Clubs_Id] BEFORE Insert ON [Members] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table Members violates foreign key constraint FK_Members_1_0') WHERE NOT EXISTS (SELECT * FROM Clubs WHERE  Id = NEW.ClubId); END;
CREATE TRIGGER [fku_Members_ClubId_Clubs_Id] BEFORE Update ON [Members] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table Members violates foreign key constraint FK_Members_1_0') WHERE NOT EXISTS (SELECT * FROM Clubs WHERE  Id = NEW.ClubId); END;
CREATE TRIGGER [fki_Annoucements_ClubId_Clubs_Id] BEFORE Insert ON [Annoucements] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table Annoucements violates foreign key constraint FK_Annoucements_0_0') WHERE NOT EXISTS (SELECT * FROM Clubs WHERE  Id = NEW.ClubId); END;
CREATE TRIGGER [fku_Annoucements_ClubId_Clubs_Id] BEFORE Update ON [Annoucements] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table Annoucements violates foreign key constraint FK_Annoucements_0_0') WHERE NOT EXISTS (SELECT * FROM Clubs WHERE  Id = NEW.ClubId); END;
CREATE TRIGGER [fki_Adhesions_ClubId_Clubs_Id] BEFORE Insert ON [Adhesions] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table Adhesions violates foreign key constraint FK_Adhesions_0_0') WHERE NOT EXISTS (SELECT * FROM Clubs WHERE  Id = NEW.ClubId); END;
CREATE TRIGGER [fku_Adhesions_ClubId_Clubs_Id] BEFORE Update ON [Adhesions] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table Adhesions violates foreign key constraint FK_Adhesions_0_0') WHERE NOT EXISTS (SELECT * FROM Clubs WHERE  Id = NEW.ClubId); END;
CREATE TRIGGER [fki_Adhesions_UserId_Users_Id] BEFORE Insert ON [Adhesions] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Insert on table Adhesions violates foreign key constraint FK_Adhesions_1_0') WHERE NOT EXISTS (SELECT * FROM Users WHERE  Id = NEW.UserId); END;
CREATE TRIGGER [fku_Adhesions_UserId_Users_Id] BEFORE Update ON [Adhesions] FOR EACH ROW BEGIN SELECT RAISE(ROLLBACK, 'Update on table Adhesions violates foreign key constraint FK_Adhesions_1_0') WHERE NOT EXISTS (SELECT * FROM Users WHERE  Id = NEW.UserId); END;
COMMIT;

