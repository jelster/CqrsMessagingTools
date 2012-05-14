using System;
using System.Collections.Generic;
using System.Text;
using MIL.Visitors;
using Xunit;

namespace MilVisitorsTestFixture
{
	public abstract class MilTokenFormatterTestFixture
	{
		protected MilToken sut;
		protected string result;
		public virtual void returns_properly_formatted_string()
		{
			result = sut.ToString();
			Assert.True(result.Contains(sut.Token.MilToken));
			Console.Write(result);
		}
	}
	public class given_a_MIL_token
	{
		public class when_command_token_converted_to_string : MilTokenFormatterTestFixture
		{
			public when_command_token_converted_to_string()
			{
				sut = TokenFactory.GetCommand("Barmand");
			}

			[Fact]
			public override void returns_properly_formatted_string()
			{
				base.returns_properly_formatted_string();

				Assert.True(result == "Barmand?");
			    Console.WriteLine(result);
			}
		}

		public class when_event_token_converted_to_string : MilTokenFormatterTestFixture
		{
			public when_event_token_converted_to_string()
			{
				sut = TokenFactory.GetEvent("Foovent");
			}

			[Fact]
			public override void returns_properly_formatted_string()
			{
				base.returns_properly_formatted_string();

				Assert.True(result == "Foovent!");
                Console.WriteLine(result);
			}
		}
	}

	public class given_a_MIL_statement
	{
		private readonly Queue<MilToken> statement = new Queue<MilToken>();

		public given_a_MIL_statement()
		{
		 
			 const string fullMil = 
        @"MakeSeatReservation? -> SeatsAvailabilityHandler 
				@SeatsAvailability:SeatsReserved! -> 
					-> RegistrationProcessRouter::RegistrationProcess
					*RegistrationProcess.State = AwaitingPayment
						:MarkSeatsReserved? ->
						:ExpireRegistrationProcess? -> [Delay]";
		}

		[Fact]
		public void command_to_command_handler_via_bus_outputs_correct_mil()
		{
            statement.Enqueue(TokenFactory.GetCommand("MakeSeatReservation"));
            statement.Enqueue(TokenFactory.GetPublish());
            statement.Enqueue(TokenFactory.GetCommandHandler("SeatsAvailabilityHandler"));
            statement.Enqueue(TokenFactory.GetStatementTerminator());
		    string expected = "MakeSeatReservation? -> SeatsAvailabilityHandler" + Environment.NewLine;
                
            AssertMilOutput(expected);
		}

	    [Fact]
        public void aggregate_root_creates_event_and_publishes_outputs_correct_mil()
        {
            statement.Enqueue(TokenFactory.GetAggregateRoot("SeatsAvailability"));
            statement.Enqueue(TokenFactory.GetAssociation(AssociationType.Origin));
            statement.Enqueue(TokenFactory.GetEvent("SeatsReserved"));
            statement.Enqueue(TokenFactory.GetPublish());
            statement.Enqueue(TokenFactory.GetStatementTerminator());
            string expected = "@SeatsAvailability:SeatsReserved! -> " + Environment.NewLine;

	        AssertMilOutput(expected);
        }

	    [Fact]
        public void parent_receives_event_routes_to_destination_handler_outputs_correct_mil()
        {
            statement.Enqueue(TokenFactory.GetReceive());
            statement.Enqueue(TokenFactory.GetEventHandler("RegistrationProcessRouter"));
            statement.Enqueue(TokenFactory.GetAssociation(AssociationType.Origin));
            statement.Enqueue(TokenFactory.GetAssociation(AssociationType.Destination));
            statement.Enqueue(TokenFactory.GetEventHandler("RegistrationProcess"));
            statement.Enqueue(TokenFactory.GetStatementTerminator());

            string expected = " -> RegistrationProcessRouter::RegistrationProcess" + Environment.NewLine;

            AssertMilOutput(expected);
        }

	    [Fact]
        public void state_change_outputs_correct_mil()
        {
            statement.Enqueue(TokenFactory.GetStateChangeExpression("RegistrationProcess.State", "AwaitingPayment"));
            statement.Enqueue(TokenFactory.GetStatementTerminator());

            string expected = "*RegistrationProcess.State = AwaitingPayment" + Environment.NewLine;

            AssertMilOutput(expected);
        }

        [Fact]
        public void associated_event_created_and_published_and_publish_with_delay_outputs_correct_mil()
        {
            statement.Enqueue(TokenFactory.GetAssociation(AssociationType.Origin));
            statement.Enqueue(TokenFactory.GetCommand("MarkSeatsReserved"));
            statement.Enqueue(TokenFactory.GetPublish());
            statement.Enqueue(TokenFactory.GetStatementTerminator());
            statement.Enqueue(TokenFactory.GetAssociation(AssociationType.Origin));
            statement.Enqueue(TokenFactory.GetCommand("ExpireRegistrationProcess"));
            statement.Enqueue(TokenFactory.GetPublish());
            statement.Enqueue(TokenFactory.GetDelay());
            statement.Enqueue(TokenFactory.GetStatementTerminator());

            string expected = ":MarkSeatsReserved? -> " + Environment.NewLine +
                              ":ExpireRegistrationProcess? ->  [Delay] " + Environment.NewLine;

            AssertMilOutput(expected);
        }

	    private void AssertMilOutput(string expected)
	    {
	        string acting = "";
	        while (statement.Count > 0 && statement.Peek() != null)
	        {
	            var sut = statement.Dequeue();
	            acting += sut.ToString();
	        }
	        Assert.Equal(expected, acting);
	        Console.WriteLine(acting);
	    }
	}


}