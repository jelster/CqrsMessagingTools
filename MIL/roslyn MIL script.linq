<Query Kind="Program">
  <GACReference>Roslyn.Compilers, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35</GACReference>
  <GACReference>Roslyn.Compilers.CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35</GACReference>
  <GACReference>Roslyn.Services, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35</GACReference>
  <GACReference>Roslyn.Services.CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35</GACReference>
  <Namespace>Roslyn.Compilers.CSharp</Namespace>
  <Namespace>Roslyn.Services</Namespace>
  <Namespace>Roslyn.Compilers</Namespace>
</Query>

void Main()
{
	var cancel = new CancellationToken(false);
	ISolution sln = Solution.Load(@"<base path or remove all and replace with your own sln>\cqrs-journey-code\source\Conference.sln");
 
	var projs = sln.Projects;
	var commandHandlerVisitor = new CommandHandlerSyntaxVisitor();
	
	var creationFormat = new SymbolDisplayFormat(
													typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
													genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
													memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
													localStyle: SymbolDisplayLocalStyle.NameAndType,
													miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
												);
	foreach (var p in projs)
	{
		var allCommandHandlersInProject = (from allTrees in p.GetCompilation(cancel).SyntaxTrees
												from classes in allTrees.Root.DescendentNodes().OfType<ClassDeclarationSyntax>()
													where commandHandlerVisitor.Visit(classes).Any()
											   select classes);
		if (allCommandHandlersInProject.Any())
		{
			var display = allCommandHandlersInProject.Select(x => string.Format("{0}{1}", x.Identifier.GetFullText(), x.BaseListOpt.GetFullText()));
			display.Dump(p.AssemblyName + " Command Handler Listing");
		}
		
		foreach (var document in p.Documents)
		{
			SemanticModel model = (SemanticModel)document.GetSemanticModel(cancel);
			var syntaxTree = document.GetSyntaxTree(cancel);
			
			#region Command Creation
			var creationStmts = from node in syntaxTree.Root.DescendentNodes().OfType<ObjectCreationExpressionSyntax>()
								where model.GetSemanticInfo(node).Type.Interfaces.Any(i => i.Name == "ICommand")
								select node;
			
			if (creationStmts.Any())
			{
				var display = creationStmts.Select(x => 
				{
					var info = model.GetSemanticInfo(x);
					var displays = info.Symbol.Locations.AsList().Cast<Location>().Select(lo => 
					{
							var line = lo.SourceTree.GetLineSpan(x.FullSpan, true);
							return string.Format("File: {1}, line {2}{0}Source Context:{0}{3}", 
							Environment.NewLine,
							line.FileName,
							line.StartLinePosition.Line,
							lo.InSource ? x.Ancestors().Skip(4).First().GetText() : lo.MetadataModule.ToDisplayString());
					}).DefaultIfEmpty();
					return string.Join(Environment.NewLine, displays);
				});
				display.Dump(document.DisplayName + " Command creation statements");			
			}			
			#endregion
			
			var commandPublications = syntaxTree.Root.DescendentNodes()
				.OfType<InvocationExpressionSyntax>()
				.Select(x => x.Expression)
					.OfType<MemberAccessExpressionSyntax>()
					.Where(x => x.Name.GetFullText() == "Send");
					
			if (commandPublications.Any())
			{
				var display = commandPublications.Select(x => 
				{
					var info = model.GetSemanticInfo(x);
					var lo = syntaxTree.GetLocation(x);  
					var line = lo.SourceTree.GetLineSpan(x.FullSpan, true);
					return string.Format("File: {1}, line {2}{0}Source:{0}{3}", 
							Environment.NewLine,
							line.FileName,
							line.StartLinePosition.Line,
							x.Parent.GetText());
				}).DefaultIfEmpty();
				display.Dump("Invocations of Send");				
			}
		}
	}
}
public class MilSyntaxWalker : SyntaxWalker
{
	
}

public class CommandHandlerSyntaxVisitor : SyntaxVisitor<IEnumerable<GenericNameSyntax>>
{
	private const string CommandHandlerInterfaceName = "ICommandHandler";
	protected override IEnumerable<GenericNameSyntax> VisitClassDeclaration(ClassDeclarationSyntax node)
	{
		return node.BaseListOpt != null ? 
			node.BaseListOpt.Types.OfType<GenericNameSyntax>().Where(x => x.PlainName == CommandHandlerInterfaceName) : 
			Enumerable.Empty<GenericNameSyntax>();
	}		
}
// Define other methods and classes here