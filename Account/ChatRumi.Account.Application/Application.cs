using System.Reflection;

namespace ChatRumi.Account.Application;

public class Application
{
    public static Assembly Assembly => typeof(Application).Assembly;
}