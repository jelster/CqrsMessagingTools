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
			}
		}
	}

	public class given_a_MIL_statement
	{
		private readonly Queue<MilToken> statement = new Queue<MilToken>();

		public given_a_MIL_statement()
		{
		/* Corresponds to the following MIL
		 * 
		 *  
			 MakeSeatReservation? -> SeatsAvailabilityHandler 
				@SeatsAvailability:SeatsReserved! -> 
					-> RegistrationProcessRouter::RegistrationProcess
					*RegistrationProcess.State = AwaitingPayment
						:MarkSeatsReserved? ->
						:ExpireRegistrationProcess? -> [Delay]
		 *  
		 *  
		 */
			statement.Enqueue(TokenFactory.GetCommand("MakeSeatReservation"));
			statement.Enqueue(TokenFactory.GetPublish());
            statement.Enqueue(TokenFactory.GetCommandHandler("SeatsAvailabilityHandler"));
            statement.Enqueue(TokenFactory.GetStatementTerminator());
            //statement.Enqueue(TokenFactory.GetAggregateRoot("SeatsAvailability"));
            //statement.Enqueue(TokenFactory.GetAssociation(AssociationType.Origin));
            //statement.Enqueue(TokenFactory.GetEvent("SeatsReserved"));
            //statement.Enqueue(TokenFactory.GetPublish());
            //statement.Enqueue(TokenFactory.GetStatementTerminator());
            //statement.Enqueue(TokenFactory.GetReceive());
            //statement.Enqueue(TokenFactory.GetEventHandler("RegistrationProcessRouter"));
            //statement.Enqueue(TokenFactory.GetAssociation(AssociationType.Origin));
            //statement.Enqueue(TokenFactory.GetAssociation(AssociationType.Destination));
            //statement.Enqueue(TokenFactory.GetEventHandler("RegistrationProcess"));
            //statement.Enqueue(TokenFactory.GetStatementTerminator());
            //statement.Enqueue(TokenFactory.GetStateChangeExpression("RegistrationProcess.State", "AwaitingPayment"));
            //statement.Enqueue(TokenFactory.GetPublish());
            //statement.Enqueue(TokenFactory.GetStatementTerminator());
		}

		[Fact]
		public void when_statement_is_output_forms_valid_MIL()
		{
		    var expected = "MakeSeatReservation? -> SeatsAvailabilityHandler" + Environment.NewLine;
            string acting = "";
			while (statement.Count > 0  && statement.Peek() != null)
			{
				var sut = statement.Dequeue();
				//if (sut == null) break;
			    acting += sut.ToString();
			}
            Assert.Equal(expected, acting);

		}
	}


}