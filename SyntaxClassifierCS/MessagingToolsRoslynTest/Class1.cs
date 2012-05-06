using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagingToolsRoslynTest.Interfaces;

namespace MessagingToolsRoslynTest.Interfaces
{
    public interface ICommand
    {
        string Name { get; }
    }

    public interface ICommandHandler<T> where T : ICommand
    {
        void Send(T command);
    }
}

namespace MessagingToolsRoslynTest
{

    public class Foo : ICommand
    {
        #region Implementation of ICommand

        public string Name { get; set; }

        #endregion
    }

    public class FooHandler : ICommandHandler<Foo>
    {
        public void Send(Foo command)
        {
            Console.WriteLine("Sent Command!");
        }
    }

    public class MainApp
    {
        public static void Main()
        {
            #region TestsLookHere
            var f = new Foo();
            var h = new FooHandler();
            h.Send(f);
            #endregion

        }
    }

   
}
