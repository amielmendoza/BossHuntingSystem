-- SQL Script to revert member names from ARQdelilah back to Delilaah and ARQxerneas back to xerneas
-- This script reverts all instances in both the Members table and attendance records in BossDefeats table

BEGIN TRANSACTION;

-- Revert Members table
UPDATE Members
SET Name = 'Delilaah'
WHERE Name = 'ARQdelilah';

UPDATE Members
SET Name = 'xerneas'
WHERE Name = 'ARQxerneas';

-- Revert AttendeesJson in BossDefeats table (legacy attendees list)
UPDATE BossDefeats
SET AttendeesJson = REPLACE(AttendeesJson, '"ARQdelilah"', '"Delilaah"')
WHERE AttendeesJson LIKE '%"ARQdelilah"%';

UPDATE BossDefeats
SET AttendeesJson = REPLACE(AttendeesJson, '"ARQxerneas"', '"xerneas"')
WHERE AttendeesJson LIKE '%"ARQxerneas"%';

-- Revert AttendeeDetailsJson in BossDefeats table (detailed attendee info with points)
UPDATE BossDefeats
SET AttendeeDetailsJson = REPLACE(AttendeeDetailsJson, '"Name":"ARQdelilah"', '"Name":"Delilaah"')
WHERE AttendeeDetailsJson LIKE '%"Name":"ARQdelilah"%';

UPDATE BossDefeats
SET AttendeeDetailsJson = REPLACE(AttendeeDetailsJson, '"Name":"ARQxerneas"', '"Name":"xerneas"')
WHERE AttendeeDetailsJson LIKE '%"Name":"ARQxerneas"%';

-- Revert Owner field in BossDefeats table
UPDATE BossDefeats
SET Owner = 'Delilaah'
WHERE Owner = 'ARQdelilah';

UPDATE BossDefeats
SET Owner = 'xerneas'
WHERE Owner = 'ARQxerneas';

-- Display affected records count for verification
SELECT 'Members reverted' as TableName, COUNT(*) as RecordsAffected
FROM Members
WHERE Name IN ('Delilaah', 'xerneas')

UNION ALL

SELECT 'BossDefeats with reverted AttendeesJson' as TableName, COUNT(*) as RecordsAffected
FROM BossDefeats
WHERE AttendeesJson LIKE '%"Delilaah"%' OR AttendeesJson LIKE '%"xerneas"%'

UNION ALL

SELECT 'BossDefeats with reverted AttendeeDetailsJson' as TableName, COUNT(*) as RecordsAffected
FROM BossDefeats
WHERE AttendeeDetailsJson LIKE '%"Delilaah"%' OR AttendeeDetailsJson LIKE '%"xerneas"%'

UNION ALL

SELECT 'BossDefeats with reverted Owner' as TableName, COUNT(*) as RecordsAffected
FROM BossDefeats
WHERE Owner IN ('Delilaah', 'xerneas');

-- Commit the transaction
COMMIT TRANSACTION;

-- Optional: Display sample records to verify the revert
SELECT 'Sample reverted Members:' as Info;
SELECT Id, Name, CombatPower FROM Members WHERE Name IN ('Delilaah', 'xerneas');

SELECT 'Sample BossDefeats with reverted attendance:' as Info;
SELECT TOP 5 Id, BossName, AttendeesJson, AttendeeDetailsJson, Owner
FROM BossDefeats
WHERE AttendeesJson LIKE '%Delilaah%'
   OR AttendeesJson LIKE '%xerneas%'
   OR AttendeeDetailsJson LIKE '%Delilaah%'
   OR AttendeeDetailsJson LIKE '%xerneas%'
   OR Owner IN ('Delilaah', 'xerneas');