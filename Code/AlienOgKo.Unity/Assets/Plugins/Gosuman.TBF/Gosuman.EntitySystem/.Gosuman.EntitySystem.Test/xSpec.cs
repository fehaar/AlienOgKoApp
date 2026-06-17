using System;

namespace Gosuman.EntitySystem.Test
{
    public class xSpec
    {
        protected void Given(string description, Action act)
        {
            System.Console.WriteLine("Given " + description);
            act();
        }

        protected void With(string description, Action act)
        {
            System.Console.WriteLine("With " + description);
            act();
        }

        protected void Where(string description, Action act)
        {
            System.Console.WriteLine("Where " + description);
            act();
        }
        protected void And(string description, Action act)
        {
            System.Console.WriteLine("And " + description);
            act();
        }


        protected void That(string description, Action act)
        {
            System.Console.WriteLine("That " + description);
            act();
        }

        protected void Expect(string description, Action act)
        {
            System.Console.WriteLine("Expect " + description);
            act();
        }
    }
}
