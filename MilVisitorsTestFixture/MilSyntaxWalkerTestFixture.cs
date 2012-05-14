using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MIL.Visitors;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Xunit;

namespace MilVisitorsTestFixture
{
	public class MilSyntaxWalkerTestFixture
	{
		public class given_a_syntax_tree
		{
			private readonly MilSyntaxWalker sut;

			public given_a_syntax_tree()
			{
				sut = new MilSyntaxWalker();
			}

			[Fact]
			public void when_walker_visits_command_class_declaration_adds_to_command_list()
			{
				var tree = SyntaxTree.ParseCompilationUnit("public class Foomand : ICommand {}");
				SyntaxNode node = tree.Root;
				sut.Visit(node);

				Assert.NotEmpty(sut.Commands);
				Assert.True(sut.Commands.Count == 1);
				Assert.True(sut.Commands.First().Identifier.GetText() == "Foomand");
			}

			[Fact]
			public void when_walker_visits_command_handler_class_declaration_added_to_cmd_handler_list()
			{
				var tree = SyntaxTree.ParseCompilationUnit("public class FooHandler : ICommandHandler<Foomand> {}");
				var node = tree.Root;
				sut.Visit(node);

				Assert.NotEmpty(sut.CommandHandlers);
				Assert.True(sut.CommandHandlers.Count() == 1);
				Assert.True(sut.CommandHandlers.First().Identifier.GetText() == "FooHandler");
			}

			[Fact]
			public void when_walker_visits_node_with_publish_operation_adds_to_publication_list()
			{
				var tree = SyntaxTree.ParseCompilationUnit("public class HaHa { public void Bar() { var a = new Foomand(); messageBus.Send(a); }}");
				var node = tree.Root;
				sut.Visit(node);

				Assert.NotEmpty(sut.PublicationCalls);
				Assert.True(sut.PublicationCalls.Count == 1);
			}

			[Fact]
			public void when_walker_visits_event_class_declaration_adds_to_event_list()
			{
				var tree = SyntaxTree.ParseCompilationUnit("public class Foovent : IEvent {}");
				var node = tree.Root;
				sut.Visit(node);

				Assert.NotEmpty(sut.Events);
				Assert.True(sut.Events.Count == 1);
				Assert.True(sut.Events.First().Identifier.GetText() == "Foovent");
			}

			[Fact]
			public void when_walker_visits_event_handler_class_adds_to_event_handlers_list()
			{
				var tree = SyntaxTree.ParseCompilationUnit("public class FooventHandler : IEventHandler<Foo> {}");
				var node = tree.Root;
				sut.Visit(node);

				Assert.NotEmpty(sut.EventHandlers);
				Assert.True(sut.EventHandlers.Count() == 1);
				Assert.True(sut.EventHandlers.First().Identifier.GetText() == "FooventHandler");
			}

			[Fact]
			public void walker_correctly_finds_publications_in_complete_program()
			{
				const string code =
					@"
namespace TestCode 
{
	using System;  
	
	public interface ICommand {}
	
	public interface ICommandHandler<T> where T : ICommand
	{
		void Handles(T command);
	}
	public class Foo : ICommand { }	                           
	public class FoomandHandler : ICommandHandler<Foo>
	{
		public void Handles(Foo command)
		{
			Console.Write(""Foomand handled {0}"", command.Name);
		}

		public void Send(ICommand cmd) { Handles(cmd); }
	}   
	public class BadFooHandler : ICommandHandler<Foo> 
	{ 
		public void Handles(Foo command) { throw new NotImplementedException(); }
	}                
	public class Program
	{
		public static void Main()
		{
			var handler = new FoomandHandler();
			var cmd = new Foo();
			
			handler.Send(cmd);

		}		
	}
							
}";
				var compilation = Compilation.Create("test.exe")
					.AddSyntaxTrees(SyntaxTree.ParseCompilationUnit(code))
					.UpdateOptions(new CompilationOptions("TestCode.Program", "Program", AssemblyKind.ConsoleApplication))
					.AddReferences(new AssemblyFileReference(typeof (object).Assembly.Location));

				var d = compilation.GetDeclarationDiagnostics();
				Assert.Empty(d);

				var walker = this.AnalyzeInitiatingSequence(compilation);
				Assert.NotEmpty(walker.PublicationCalls);
			}

			private Func<NamespaceOrTypeSymbol, IEnumerable<Symbol>> nameExtractor;

			private MilSyntaxWalker AnalyzeInitiatingSequence(Compilation compilation)
			{
				nameExtractor = name =>
									{
										var members = name.GetMembers().ToList();
										return members.Concat(members.OfType<NamespaceSymbol>()
																  .SelectMany(x => nameExtractor(x)));
									};

				var walker = new MIL.Visitors.MilSyntaxWalker();
                //var types = compilation.SourceModule.GlobalNamespace
                //    .GetMembers().OfType<NamespaceOrTypeSymbol>()
                //    .SelectMany(nameExtractor).OfType<TypeSymbol>();

			    
			    foreach (var tree in compilation.SyntaxTrees)
			    {
                    //var model = compilation.GetSemanticModel(tree);
                    //model.GetDeclaredSymbol(
                    //    tree.Root.DescendentNodes().OfType<InvocationExpressionSyntax>().First(
                    //        x => ((MemberAccessExpressionSyntax) x.Expression).GetText() == "Main"));
			        walker.Visit(tree.Root);
			    }
                    
				//	Console.WriteLine(string.Join("",t.GetText().Take(200).ToList()));
				
			    
				return walker;
			}
		}
	}

}
