-- Update default user passwords with correct BCrypt hashes
-- Password for admin: Admin@123
-- Password for superadmin: SuperAdmin@123

UPDATE Users
SET PasswordHash = '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIeWIgNZkq'
WHERE Username = 'admin';

UPDATE Users
SET PasswordHash = '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5GyYIeWIgNZkq'
WHERE Username = 'superadmin';

SELECT Username, PasswordHash FROM Users WHERE Username IN ('admin', 'superadmin');
