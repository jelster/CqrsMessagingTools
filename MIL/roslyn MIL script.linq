<Query Kind="Program">
  <Reference Relative="..\MIL.Visitors\bin\Debug\MIL.Visitors.dll">D:\Source\CqrsMessagingTools\MIL.Visitors\bin\Debug\MIL.Visitors.dll</Reference>
  <GACReference>Roslyn.Compilers, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35</GACReference>
  <GACReference>Roslyn.Compilers.CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35</GACReference>
  <GACReference>Roslyn.Services, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35</GACReference>
  <GACReference>Roslyn.Services.CSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35</GACReference>
  <Namespace>Roslyn.Compilers.CSharp</Namespace>
  <Namespace>Roslyn.Services</Namespace>
  <Namespace>Roslyn.Compilers</Namespace>
  <Namespace>MIL.Visitors</Namespace>
  <Namespace>Roslyn.Compilers.Common</Namespace>
</Query>

void Main()
{
	var cancel = new CancellationToken(false);
	ISolution sln = Solution.Load(@"d:\source\cqrs-journey-code\source\Conference.sln");
 
	var projs = sln.Projects;
	
	
	var creationFormat = new SymbolDisplayFormat(
													typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
													genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
													memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
													localStyle: SymbolDisplayLocalStyle.NameAndType,
													miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
												);
												
	
	
	foreach (var p in projs)
	{
		var comp = (Compilation)p.GetCompilation();
		var ma = new MilSyntaxAnalysis();
		
		var tokens = ma.AnalyzeInitiatingSequence(comp);
		//tokens.Dump();		
	}
		
}
public class MilSyntaxAnalysis
{
	public Queue<MilToken> ExternalInputStatements = new Queue<MilToken>();
	Func<NamespaceOrTypeSymbol, IEnumerable<TypeSymbol>> nameExtractor; 
	
	public MilSyntaxAnalysis()
	{
		nameExtractor = name => 
		{
			var members = name.GetMembers().ToList();
			return members.Concat(members.OfType<NamespaceSymbol>()
				.SelectMany(x => nameExtractor(x)))
				.OfType<TypeSymbol>();
		};
	}
	
	public IEnumerable<MilToken> AnalyzeInitiatingSequence(Compilation compilation)
	{
		var walker = new MIL.Visitors.MilSyntaxWalker();
		var types = compilation.SourceModule.GlobalNamespace
		.GetMembers().AsList()
		.Cast<NamespaceOrTypeSymbol>()
		.SelectMany(nameExtractor);
		 
		foreach (var s in types)
		{
	//	s.ToDisplayString().Dump();
		}		
		
		var trees = compilation.SyntaxTrees;
		foreach (var t in trees)
		{
			walker.Visit(t.Root);
			var sourceClassOfCalls = walker.PublicationCalls.Select(x => x.ArgumentList.Arguments.ToList().Select(y => y.Expression).Cast<TypeSyntax>().First());
			sourceClassOfCalls.Select(x => x.GetFullText()).Dump();
		}
		
	//	var refs = compilation.
		return null;
	}
	
}
// Define other methods and classes here