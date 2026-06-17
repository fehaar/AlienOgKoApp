using System;

namespace Gosuman.TBF.Shared.Test;
public class xSpec
{
    protected void Given(string description, Action act)
    {
        Console.WriteLine("Given " + description);
        act();
    }

    protected void With(string description, Action act)
    {
        Console.WriteLine("With " + description);
        act();
    }

    protected void Where(string description, Action act)
    {
        Console.WriteLine("Where " + description);
        act();
    }

    protected void And(string description, Action act)
    {
        Console.WriteLine("And " + description);
        act();
    }

    protected void That(string description, Action act)
    {
        Console.WriteLine("That " + description);
        act();
    }

    protected void Expect(string description, Action act)
    {
        Console.WriteLine("Expect " + description);
        act();
    }
}

