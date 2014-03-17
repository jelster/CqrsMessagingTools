# CQRS Messaging Tools
## Readme

These concepts wouldn't have happened without the [Microsoft Patterns & Practices group's CQRS-Journey](http://cqrsjourney.github.com/) project and team. See below for instructions and warnings

##6-14-2012 IMPORTANT UPDATE:
* The new CTP of the Roslyn tooling introduces a ton of new features as well as reworked API's. As a consequence, the tooling will not currently build using the new tooling. I'm hoping to correct this in the near future, but for now the CqrsMessagingTools (SawMIL sound like a good name?) do not support it. 
* Questions, comments, feedback, etc are all welcome - visit the [https://github.com/jelster/CqrsMessagingTools/issues](issue tracker) to do so.

# NEW! MilGenerator command-line tool
The MilGenerator.exe utility will analyze a given solution file, outputting (optionally) a raw dump of messaging information to the console. 
##Basic Usage:
1. the -s parameter is the full path to the VS .SLN file of the target application
2. pass the -v option for greater detail
3. pass in (after all other options) the name(s) of assemblies you want ignored in the analysis. Separate assembly names with a single empty space (" ")
4. help on options is available by passing -?, -h, or --help as a parameter

##Sample MilGenerator output
Below is a sample of output obtained by executing the MilGenerator against the [CQRS-Journey Code](//github.com/mspnp/cqrs-journey-code). The output has been edited to remove messages relating to missing Azure SDK assemblies on the local system where it was ran. The output has been edited to remove erroneously reported data on test assemblies.
````
>.\milgenerator.exe -s "c:\users\josh\documents\github\cqrs-journey-code\source\conference.noazuresdk.sln" -v Registration.Tests Registration.IntegrationTests Conference.Web.Public.Tests Azure.IntegrationTests Conference.IntegrationTests Payments.Tests Infrastructure.Sql.IntegrationTests Infrastructure.Tests

Loading solution C:\users\josh\documents\github\cqrs-journey-code\source\conference.noazuresdk.sln
Using IProcess for process discovery
Ignored assemblies: 
Registration.Tests
Registration.IntegrationTests
Conference.Web.Public.Tests
Azure.IntegrationTests
Conference.IntegrationTests
Payments.Tests
Infrastructure.Sql.IntegrationTests
Infrastructure.Tests

# Processing assembly Registration

# Processing assembly Infrastructure.Azure

# Processing assembly Conference.Web.Public
<messages elided>

# Processing assembly Azure.Tests

# Processing assembly Conference.Web.Admin
<messages elided>

# Processing assembly Conference

# Processing assembly Conference.Contracts

# Processing assembly Payments

# Processing assembly Payments.Contracts

# Processing assembly Infrastructure

# Processing assembly Infrastructure.Sql

# Processing assembly Conference.Common

# Processing assembly CommandProcessor
<messages elided>

# Processing assembly DatabaseInitializer

# Processing assembly Registration.Contracts

# Aggregate roots
@Order
@SeatAssignments
@SeatsAvailability

# Commands
AssignRegistrantDetails? -> OrderCommandHandler
ConfirmOrder? -> OrderCommandHandler
SeatsAvailabilityCommand? -> 
UnassignSeat? -> SeatAssignmentsHandler
AssignSeat? -> SeatAssignmentsHandler
ExpireRegistrationProcess? -> RegistrationProcessManagerRouter
RejectOrder? -> OrderCommandHandler
MarkSeatsAsReserved? -> OrderCommandHandler
RegisterToConference? -> OrderCommandHandler
FooCommand? -> 
CommandA? -> 
CommandB? -> 
CommandC? -> 
FooCommand? -> 
CancelThirdPartyProcessorPayment? -> ThirdPartyProcessorPaymentCommandHandler
CompleteThirdPartyProcessorPayment? -> ThirdPartyProcessorPaymentCommandHandler
InitiateInvoicePayment? -> 
InitiateThirdPartyProcessorPayment? -> ThirdPartyProcessorPaymentCommandHandler

# Events
FakeEvent! -> 
ConferenceEvent! -> 
ConferencePublished! -> 
     -> ConferenceViewModelGenerator
ConferenceUnpublished! -> 
     -> ConferenceViewModelGenerator
SeatUpdated! -> 
     -> ConferenceViewModelGenerator
     -> PricedOrderViewModelGenerator
SeatCreated! -> 
     -> ConferenceViewModelGenerator
     -> PricedOrderViewModelGenerator
PaymentAccepted! -> 
PaymentCompleted! -> 
     -> RegistrationProcessManagerRouter
PaymentInitiated! -> 
PaymentRejected! -> 

# Message publications
Registration.Handlers.ConferenceViewModelGenerator.Handle:[6850..7155)
Registration.Handlers.ConferenceViewModelGenerator.Handle:[8048..8369)
Registration.Handlers.ConferenceViewModelGenerator.Handle:[8468..8800)
Infrastructure.Azure.Messaging.CommandBus.Send:[2265..2310)
Infrastructure.Azure.Messaging.CommandBus.Send:[2483..2501)
Infrastructure.Azure.Messaging.EventBus.Publish:[2628..2672)
Infrastructure.Azure.Messaging.SynchronousCommandBusDecorator.Send:[2257..2286)
Infrastructure.Azure.Messaging.SynchronousCommandBusDecorator.Send:[2963..2992)
Infrastructure.Messaging.CommandBusExtensions.Send:[1457..1498)
Infrastructure.Messaging.CommandBusExtensions.Send:[1624..1681)
Infrastructure.Sql.Messaging.CommandBus.Send:[2434..2459)
Infrastructure.Sql.Messaging.CommandBus.Send:[2647..2673)
Infrastructure.Sql.Messaging.EventBus.Publish:[2407..2432)
Infrastructure.Sql.Messaging.EventBus.Publish:[2702..2728)
Infrastructure.Sql.Processes.SqlProcessManagerDataContext.DispatchMessages:[8859..8909)

````

# Tool Installation Instructions and Walkthrough
These tools are the product of a week's spent poking around a lot of unfamiliar territory, so be warned: Your Mileage May Vary!

1. Clone or download project source
2.	Although the LINQPad scripts do not require it, the Roslyn CTP must be installed in order to install and run the tools for Visual Studio.
3.	Open the CqrsMessagingTools solution in VS. You should be able to Ctrl+F5 to build and run the tools. A new instance of VS will start up. It distinguishes itself from other instances by having (Roslyn) in the title bars. The tools are now loaded and listening. 
4.	(optional, but best supported) Open the CQRS Journey Conference solution in the Roslyn instance of VS. Browse to the ReigstrationController.cs in the public conference registration website project and observe syntax highlighting in action!
5.	(use at your own risk!) Go to the View -> Other Windows -> Roslyn Syntax Visualizer. The tools window has two tabs: one for the syntax visualizer sample for reference and troubleshooting, and the other is the command listing. 
6.	(use at your own risk!) If you don't see anything in the interface drop-down box, click the refresh button and try again. Make sure you have a solution loaded. Select an interface in your solution from the drop-down, and you'll see a list of classes implementing that interface. These are all of the possible commands that are known to this solution and projects. 


# Making the case: Here's the issue
* First: Definitions in context
   * Messaging: Command and event publication grouped together, usually by means of a bus, queue, or some other pub/sub delivery service(s)
   * PaaS: Platform-as-a-service, e.g. Azure, AppHarbor, Amazon E3, etc
* Challenges
  1.	Loose coupling slows knowledge production
     * Easy: Component-level understanding with focused, simple object diagrams
     * Hard: Understanding how the various components interact with each other in source code
     * Easy: Comprehending domain interactions within an AR using sequence diagrams
     * Hard: Conceptualizing the message flow of a distributed business in source code
  2.	Hidden requirements add complexity
     *	Availability, idempotency, transactionality, are technical specifications that fulfill a business requirement, but are not a part of the business domain
     *	PaaS is virtually a necessity to achieve some architectural requirements, which adds more to the pile
  3.	Advanced concepts raise barriers to entry
     *	It's not enough to have quality, stable, code if only a few devs are comfortable with it in an organization


# What the tools can accomplish
* Source code assistance and intervention
  1. Syntax highlighting and classification
  2. Code Issues and Refactoring
  3. Intellisense and AutoCompletion
* Visualization
  1. Diagramming, e.g. DGML
  2. Message Flow Explorer
* Code generation and 'f'rameworks
  1. Messaging Intermediate Language (DSL)
  3. Auto-discovery and wiring components
  4. Automated generation and integration of infrastructure components with Domain

# Current components of the tools
* Syntax Highlighting
  * Highlights source code where commands are being instantiated and sent
* MessageFlow Explorer (prototype - you've been warned!)
  * Provides a facile means of navigating and discovering messaging interactions
* Code Issue
  * Raises code error if more than one class handles a command. Currently uses ICommand and ICommandHandler<T>  as hard-coded interfaces (for now)

# Messaging Intermediate (or -ary) Language (MIL) 
For a detailed walkthrough of MIL, click [here](https://github.com/jelster/CqrsMessagingTools/wiki/MIL-Walkthrough)
## Goals:
* Simple plain-text DSL (Domain-Specific-Language)
* Readable and understandable by flesh and silicone alike (the format is self-describing and already tokenized)
* Goal is to provide minimum immediate detail necessary for comprehension in order to avoid information saturation
* Provides flexibility in presentation (format can be adapted to output to almost any rendering format) 
* Can be used to make assertions when proving code (format can be directly processed into data and logic in the domain of the application)

## MIL Example
```
1.  MakeSeatReservation? -> SeatsAvailabilityHandler 
2.     @SeatsAvailability:SeatsReserved! -> 
3.           -> RegistrationProcessRouter::RegistrationProcess
4.  	     *RegistrationProcess.State = AwaitingPayment
5. 	     	 :MarkSeatsReserved? -> 
6.  	     :ExpireRegistrationProcess? -> [Delay]
7.
8.   MarkSeatsReserved? -> OrderCommandHandler
9.     @Order::OrderPartiallyReserved! ->
10.		-> OrderViewModelGenerator
```
* Line 1 - a command (note the ?) is published to the bus and handled by the `SeatsAvailabilityHandler`
* Line 2 - as a result of the message being handled by the `SeatsAvailabilityHandler`, the Aggregate Route `@SeatsAvailability` pushes the `SeatsReserved!` event to listeners
* Line 3 - The RegistrationProcessRouter receives the `SeatsReserved!` event and routes it to the appropriate `RegistrationProcess` instance. 
* Line 4 - `RegistrationProcess` changes state in response to event
* Line 5 - The `RegistrationProcess` publishes the `MarkSeatsReserved?` command to the bus
* Line 6 - An additional command is issued and published, but this time with the optional `[Delay]` token, denoting that the message will not immediately be processed or handled
* Line 9 - The `Order` Aggregate Root raises and publishes the `OrderPartiallyReserved` event to the bus in response to the `OrderCommandHandler`'s command processing.
* Line 10 - The event is picked up and handled by the `OrderViewModelGenerator`. The handler doesn't perform any messaging-relevant activities as a result of this, and so the interaction concludes
