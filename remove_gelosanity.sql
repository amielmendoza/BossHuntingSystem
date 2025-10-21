-- SQL Script to remove GeloSanity from all attendance records and members table
-- This script removes GeloSanity from Members table and all attendance records in BossDefeats table

BEGIN TRANSACTION;

-- Remove from Members table
DELETE FROM Members
WHERE Name = 'GeloSanity';

-- Remove from AttendeesJson in BossDefeats table (legacy attendees list)
-- This removes the name from JSON arrays
UPDATE BossDefeats
SET AttendeesJson =
    CASE
        -- Handle case where GeloSanity is the only attendee
        WHEN AttendeesJson = '["GeloSanity"]' THEN '[]'
        -- Handle case where GeloSanity is first in list
        WHEN AttendeesJson LIKE '["GeloSanity",%' THEN REPLACE(AttendeesJson, '"GeloSanity",', '')
        -- Handle case where GeloSanity is in middle or end of list
        WHEN AttendeesJson LIKE '%,"GeloSanity"%' THEN REPLACE(AttendeesJson, ',"GeloSanity"', '')
        ELSE AttendeesJson
    END
WHERE AttendeesJson LIKE '%"GeloSanity"%';

-- Remove from AttendeeDetailsJson in BossDefeats table (detailed attendee info with points)
-- This is more complex as we need to remove entire JSON objects
UPDATE BossDefeats
SET AttendeeDetailsJson =
    CASE
        -- Handle various JSON array formats
        WHEN AttendeeDetailsJson = '[{"Name":"GeloSanity","IsLate":false,"Points":1.0}]'
             OR AttendeeDetailsJson = '[{"Name":"GeloSanity","IsLate":true,"Points":0.5}]' THEN '[]'
        WHEN AttendeeDetailsJson LIKE '[{"Name":"GeloSanity",%' THEN
            SUBSTRING(AttendeeDetailsJson,
                CHARINDEX('},', AttendeeDetailsJson) + 2,
                LEN(AttendeeDetailsJson) - CHARINDEX('},', AttendeeDetailsJson) - 1)
        WHEN AttendeeDetailsJson LIKE '%,{"Name":"GeloSanity",%"}]' THEN
            REPLACE(AttendeeDetailsJson,
                SUBSTRING(AttendeeDetailsJson,
                    CHARINDEX(',"Name":"GeloSanity"', AttendeeDetailsJson),
                    CHARINDEX('}', AttendeeDetailsJson, CHARINDEX(',"Name":"GeloSanity"', AttendeeDetailsJson)) - CHARINDEX(',"Name":"GeloSanity"', AttendeeDetailsJson) + 1),
                '')
        ELSE AttendeeDetailsJson
    END
WHERE AttendeeDetailsJson LIKE '%"Name":"GeloSanity"%';

-- Alternative approach for AttendeeDetailsJson using simpler pattern matching
-- This handles the most common cases more reliably
UPDATE BossDefeats
SET AttendeeDetailsJson =
    CASE
        -- Remove leading comma and object
        WHEN AttendeeDetailsJson LIKE '%,{"Name":"GeloSanity","IsLate":false,"Points":1.0}%' THEN
            REPLACE(AttendeeDetailsJson, ',{"Name":"GeloSanity","IsLate":false,"Points":1.0}', '')
        WHEN AttendeeDetailsJson LIKE '%,{"Name":"GeloSanity","IsLate":true,"Points":0.5}%' THEN
            REPLACE(AttendeeDetailsJson, ',{"Name":"GeloSanity","IsLate":true,"Points":0.5}', '')
        -- Remove object and trailing comma
        WHEN AttendeeDetailsJson LIKE '%{"Name":"GeloSanity","IsLate":false,"Points":1.0},%' THEN
            REPLACE(AttendeeDetailsJson, '{"Name":"GeloSanity","IsLate":false,"Points":1.0},', '')
        WHEN AttendeeDetailsJson LIKE '%{"Name":"GeloSanity","IsLate":true,"Points":0.5},%' THEN
            REPLACE(AttendeeDetailsJson, '{"Name":"GeloSanity","IsLate":true,"Points":0.5},', '')
        ELSE AttendeeDetailsJson
    END
WHERE AttendeeDetailsJson LIKE '%"Name":"GeloSanity"%';

-- Remove from Owner field in BossDefeats table
UPDATE BossDefeats
SET Owner = NULL
WHERE Owner = 'GeloSanity';

-- Display affected records count for verification
SELECT 'Members deleted' as TableName,
       (SELECT COUNT(*) FROM Members WHERE Name = 'GeloSanity') as RecordsRemaining

UNION ALL

SELECT 'BossDefeats with GeloSanity in AttendeesJson' as TableName,
       COUNT(*) as RecordsRemaining
FROM BossDefeats
WHERE AttendeesJson LIKE '%"GeloSanity"%'

UNION ALL

SELECT 'BossDefeats with GeloSanity in AttendeeDetailsJson' as TableName,
       COUNT(*) as RecordsRemaining
FROM BossDefeats
WHERE AttendeeDetailsJson LIKE '%"GeloSanity"%'

UNION ALL

SELECT 'BossDefeats with GeloSanity as Owner' as TableName,
       COUNT(*) as RecordsRemaining
FROM BossDefeats
WHERE Owner = 'GeloSanity';

-- Show records that still contain GeloSanity for manual review
SELECT 'Records still containing GeloSanity:' as Info;
SELECT Id, BossName, AttendeesJson, AttendeeDetailsJson, Owner
FROM BossDefeats
WHERE AttendeesJson LIKE '%GeloSanity%'
   OR AttendeeDetailsJson LIKE '%GeloSanity%'
   OR Owner = 'GeloSanity';

-- Commit the transaction
COMMIT TRANSACTION;