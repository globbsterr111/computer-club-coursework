using Club;
int count=0;
void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
var h=Passwords.Hash("Example-password-2026!");
Check(Passwords.Verify("Example-password-2026!",h),"correct password");
Check(!Passwords.Verify("wrong",h),"wrong password");
Check(!Passwords.Verify("example-password-2026!",h),"case sensitivity");
Check(Passwords.Hash("Example-password-2026!")!=h,"random salt");
Check(!Passwords.Verify("x","invalid"),"invalid encoding");
Check(!Passwords.Verify("x","210000:???:???"),"invalid base64");
Check(Passwords.Verify("Кириллица-123!",Passwords.Hash("Кириллица-123!")),"unicode password");
Console.WriteLine($"Passed {count}/7");