using System.Reflection;

namespace ChatRumi.Feed.Api;

public class Application
{
    public static Assembly Assembly => typeof(Application).Assembly;
}