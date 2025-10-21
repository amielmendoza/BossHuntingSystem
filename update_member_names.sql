-- SQL Script to update member names from Delilaah to ARQdelilah and xerneas to ARQxerneas
-- This script updates all instances in both the Members table and attendance records in BossDefeats table

BEGIN TRANSACTION;

-- Update Members table
UPDATE Members
SET Name = 'ARQdelilah'
WHERE Name = 'Delilaah';

UPDATE Members
SET Name = 'ARQxerneas'
WHERE Name = 'xerneas';

-- Update AttendeesJson in BossDefeats table (legacy attendees list)
UPDATE BossDefeats
SET AttendeesJson = REPLACE(AttendeesJson, '"Delilaah"', '"ARQdelilah"')
WHERE AttendeesJson LIKE '%"Delilaah"%';

UPDATE BossDefeats
SET AttendeesJson = REPLACE(AttendeesJson, '"xerneas"', '"ARQxerneas"')
WHERE AttendeesJson LIKE '%"xerneas"%';

-- Update AttendeeDetailsJson in BossDefeats table (detailed attendee info with points)
UPDATE BossDefeats
SET AttendeeDetailsJson = REPLACE(AttendeeDetailsJson, '"Name":"Delilaah"', '"Name":"ARQdelilah"')
WHERE AttendeeDetailsJson LIKE '%"Name":"Delilaah"%';

UPDATE BossDefeats
SET AttendeeDetailsJson = REPLACE(AttendeeDetailsJson, '"Name":"xerneas"', '"Name":"ARQxerneas"')
WHERE AttendeeDetailsJson LIKE '%"Name":"xerneas"%';

-- Update Owner field in BossDefeats table
UPDATE BossDefeats
SET Owner = 'ARQdelilah'
WHERE Owner = 'Delilaah';

UPDATE BossDefeats
SET Owner = 'ARQxerneas'
WHERE Owner = 'xerneas';

-- Display affected records count for verification
SELECT 'Members updated' as TableName, COUNT(*) as RecordsAffected
FROM Members
WHERE Name IN ('ARQdelilah', 'ARQxerneas')

UNION ALL

SELECT 'BossDefeats with updated AttendeesJson' as TableName, COUNT(*) as RecordsAffected
FROM BossDefeats
WHERE AttendeesJson LIKE '%"ARQdelilah"%' OR AttendeesJson LIKE '%"ARQxerneas"%'

UNION ALL

SELECT 'BossDefeats with updated AttendeeDetailsJson' as TableName, COUNT(*) as RecordsAffected
FROM BossDefeats
WHERE AttendeeDetailsJson LIKE '%"ARQdelilah"%' OR AttendeeDetailsJson LIKE '%"ARQxerneas"%'

UNION ALL

SELECT 'BossDefeats with updated Owner' as TableName, COUNT(*) as RecordsAffected
FROM BossDefeats
WHERE Owner IN ('ARQdelilah', 'ARQxerneas');

-- Commit the transaction
COMMIT TRANSACTION;

-- Optional: Display sample records to verify the changes
SELECT 'Sample updated Members:' as Info;
SELECT Id, Name, CombatPower FROM Members WHERE Name IN ('ARQdelilah', 'ARQxerneas');

SELECT 'Sample BossDefeats with updated attendance:' as Info;
SELECT TOP 5 Id, BossName, AttendeesJson, AttendeeDetailsJson, Owner
FROM BossDefeats
WHERE AttendeesJson LIKE '%ARQdelilah%'
   OR AttendeesJson LIKE '%ARQxerneas%'
   OR AttendeeDetailsJson LIKE '%ARQdelilah%'
   OR AttendeeDetailsJson LIKE '%ARQxerneas%'
   OR Owner IN ('ARQdelilah', 'ARQxerneas');