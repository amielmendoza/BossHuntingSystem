using System;

public class HashGenerator
{
    public static void Main()
    {
        string password1 = "Admin@123";
        string password2 = "SuperAdmin@123";

        string hash1 = BCrypt.Net.BCrypt.HashPassword(password1, workFactor: 12);
        string hash2 = BCrypt.Net.BCrypt.HashPassword(password2, workFactor: 12);

        Console.WriteLine($"Password: {password1}");
        Console.WriteLine($"Hash: {hash1}");
        Console.WriteLine();
        Console.WriteLine($"Password: {password2}");
        Console.WriteLine($"Hash: {hash2}");

        // Test verification
        Console.WriteLine();
        Console.WriteLine($"Verify {password1}: {BCrypt.Net.BCrypt.Verify(password1, hash1)}");
        Console.WriteLine($"Verify {password2}: {BCrypt.Net.BCrypt.Verify(password2, hash2)}");
    }
}
