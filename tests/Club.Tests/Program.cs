using Club.Services;

int passed = 0;
const string password = "Example-password-2026!";
const string unicodePassword = "Кириллица-123!";
string hash = Passwords.Hash(password);

Check(Passwords.Verify(password, hash), "correct password");
Check(!Passwords.Verify("wrong", hash), "wrong password");
Check(!Passwords.Verify("example-password-2026!", hash), "case sensitivity");
Check(Passwords.Hash(password) != hash, "random salt");
Check(!Passwords.Verify("x", "invalid"), "invalid encoding");
Check(!Passwords.Verify("x", "210000:???:???"), "invalid base64");
Check(Passwords.Verify(unicodePassword, Passwords.Hash(unicodePassword)), "unicode password");
Console.WriteLine($"Passed {passed}/7");

void Check(bool result, string name)
{
    if (!result)
        throw new InvalidOperationException(name);

    Console.WriteLine("PASS " + name);
    passed++;
}
