namespace TestCode 
{
    using System;  
    using MessagingToolsRoslynTest;
    using MessagingToolsRoslynTest.Interfaces;

    public class FoomandHandler : ICommandHandler<Bar> //,ICommandHandler<Foo>
    {
        public bool WasCalled { get; private set; }
        public void Handles(Foo command)
        {
            WasCalled = true;
            Console.Write("Foomand handled {0}", command.Name);
        }

        #region Implementation of ICommandHandler<Foo>

        public void Send(Foo command)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Implementation of ICommandHandler<Bar>

        public void Send(Bar command)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class Bar : ICommand
    {
        #region Implementation of ICommand

        public string Name { get { return "Baaarrrffff! I'm a Mog - half man, half dog. I'm my own best friend!"; } }

        #endregion
    }
}