using System.Reflection;

namespace ChatRumi.Chat.Application;

public class Application
{
    public static Assembly Assembly => typeof(Application).Assembly;
}