CQRS Messaging Tools
Readme

These concepts wouldn’t have happened without the MS PnP CQRS-Journey project and team. See below for instructions and warnings

Tool Installation Instructions and Walkthrough
These tools are the product of a week’s spent poking around a lot of unfamiliar territory, so be warned: Your Mileage May Vary!
1.	Clone or download project source
2.	Although the LINQPad scripts do not require it, the Roslyn CTP must be installed in order to install and run the tools for Visual Studio.
3.	Open the CqrsMessagingTools solution in VS. You should be able to Ctrl+F5 to build and run the tools. A new instance of VS will start up – it distinguishes itself from other instances by having (Roslyn) in the title bars. The tools are now loaded and listening. 
4.	(optional, but best supported) Open the CQRS Journey Conference solution in the Roslyn instance of VS. Browse to the ReigstrationController.cs in the public conference registration website project and observe syntax highlighting in action!
5.	(use at your own risk!) Go to the View -> Other Windows -> Roslyn Syntax Visualizer. The tools window has two tabs: one for the syntax visualizer sample for reference and troubleshooting, and the other is the command listing. 
6.	(use at your own risk!) If you don’t see anything in the interface drop-down box, click the refresh button and try again. Make sure you have a solution loaded. Select an interface in your solution from the drop-down, and you’ll see a list of classes implementing that interface. These are all of the possible commands that are known to this solution and projects. 


Making the case: Here’s the issue
First: Definitions in context
?	Messaging: Command and event publication grouped together, usually by means of a bus, queue, or some other pub/sub delivery service(s)
?	PaaS: Platform-as-a-service, e.g. Azure, AppHarbor, Amazon E3, etc
1.	Loose coupling slows knowledge production
?	Easy: Component-level understanding with focused, simple object diagrams
?	Hard: Understanding how the various components interact with each other in source code
?	Easy: Comprehending domain interactions within an AR using sequence diagrams
?	Hard: Conceptualizing the message flow of a distributed business in source code
2.	Hidden requirements add complexity
?	Availability, idempotency, transactionality, are technical specifications that fulfill a business requirement, but are not a part of the business domain
?	PaaS is virtually a necessity to achieve some architectural requirements, which adds more to the pile
3.	Advanced concepts raise barriers to entry
?	It’s not enough to have quality, stable, code if only a few devs are comfortable with it in an organization
?	Is it too much to expect developers to have read the Blue Book (Eric Evans’ work defining DDD) before “gaining entry” to the world of CQRS?



What the tools can accomplish
Source code assistance and intervention
Syntax highlighting and classification
Code Issues and Refactoring
Intellisense and AutoCompletion
Visualization
Diagramming, e.g. DGML
Message Flow Explorer
Code generation and ‘f’rameworks
Messaging Intermediate Language (DSL)
Templated code generation
Auto-discovery and wiring components
Automated generation and integration of infrastructure components with Domain

Current components of the tools
Syntax Highlighting
Highlights source code where commands are being instantiated and sent
MessageFlow Explorer (prototype - you’ve been warned!)
Provides a facile means of navigating and discovering messaging interactions
Code Issue
Raises code warning if more than one class handles a command – currently uses ICommand and ICommandHandler<T>  as hard-coded interfaces (for now)

Messaging Intermediate (or –ary) Language (MIL) 

Goals:
•	Simple plain-text DSL (Domain-Specific-Language)
•	Readable and understandable by flesh and silicone alike (the format is self-describing and already tokenized)
•	Goal is to provide minimum immediate detail necessary for comprehension in order to avoid information saturation
•	Provides flexibility in presentation (format can be adapted to output to almost any rendering format) 
•	Can be used to make assertions when proving code (format can be directly processed into data and logic in the domain of the application)

Example
1.  MakeSeatReservation? -> SeatsAvailabilityHandler 
2.     @SeatsAvailability:SeatsReserved! -> 
3.           -> RegistrationProcessRouter::RegistrationProcess
4.  	     *RegistrationProcess.State = AwaitingPayment
5. 	     :MarkSeatsReserved? -> 
6.  	     :ExpireRegistrationProcess? -> [Delay]
7.
8.   MarkSeatsReserved? -> OrderCommandHandler
9.     @Order::OrderPartiallyReserved! ->
10.	-> OrderViewModelGenerator::.

Line 1 - a command (note the ?) is published to the bus and handled by the SeatsAvailabilityHandler
Line 2 – as a result of the message being handled by the SeatsAvailabilityHandler, the AR @SeatsAvailability pushes the SeatsReserved! event to listeners
Line 3 – The RPR receives the SeatsReserved! event and routes it to the appropriate RP. 
Line 4 - *RP changes state in response to event
Line 5 – The RegistrationProcess publishes the MarkSeatsReserved? command to the bus
