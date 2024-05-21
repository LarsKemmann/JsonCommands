using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPolymorph
{
    internal sealed class DecoratedRecordHierarchyIdentifier : ISyntaxContextReceiver
    {
        private readonly string decoratingAttributeClassName;
        private readonly string decoratingAttributeFullNamespace;


#if DEBUG
        public List<string> Log { get; } =
            new List<string>();
#endif

        public List<(string Namespace, string RecordType)> DecoratedRecordTypes { get; } =
            new List<(string Namespace, string RecordType)>();

        public List<List<(string Namespace, string RecordType)>> PotentialDerivedRecordTypes { get; } =
            new List<List<(string Namespace, string RecordType)>>();


        public DecoratedRecordHierarchyIdentifier(string decoratingAttributeClassName, string decoratingAttributeFullNamespace)
        {
            this.decoratingAttributeClassName = decoratingAttributeClassName;
            this.decoratingAttributeFullNamespace = decoratingAttributeFullNamespace;
        }


        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            try
            {
                // Record declarations are candidates for decorated and derived types.
                if (context.Node is RecordDeclarationSyntax recordDeclarationSyntax)
                {
                    var candidateRecord = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node);
#if DEBUG
                    Log.Add($"Found a record: {candidateRecord.ToDisplayString()}");
#endif

                    // Identify all record types decorated with the marker attribute as base types in a hierarchy.
                    // While we could filter this further to only records that also have the 'partial' modifier,
                    // this would give a worse design-time experience by failing silently. It's better to let the
                    // compiler emit an error when we try to add the source code file for the record's attributes.
                    var attributes = candidateRecord.GetAttributes();
                    if (attributes.Any(attribute =>
                        attribute.AttributeClass.Name == decoratingAttributeClassName &&
                        attribute.AttributeClass.FullNamespace() == decoratingAttributeFullNamespace))
                    {
#if DEBUG
                        Log.Add($"  * Decorated with {decoratingAttributeFullNamespace}.{decoratingAttributeClassName}");
#endif
                        DecoratedRecordTypes.Add((Namespace: candidateRecord.FullNamespace(), RecordType: candidateRecord.Name));
                    }

                    // Identify all record types inheriting from a base type as potential derived types in a hierarchy.
                    var derivedRecordType = new List<(string Namespace, string RecordType)>();
                    var baseType = candidateRecord;
                    do
                    {
#if DEBUG
                        Log.Add($"  > Type hierarchy: {baseType.ToDisplayString()}");
#endif
                        derivedRecordType.Add((Namespace: baseType.FullNamespace(), RecordType: baseType.Name));
                        baseType = baseType.BaseType;
                    }
                    while (baseType != null && baseType.SpecialType != SpecialType.System_Object);

                    if (derivedRecordType.Count > 1)
                        PotentialDerivedRecordTypes.Add(derivedRecordType);
                }
            }
#if DEBUG
            catch (Exception ex)
            {
                Log.Add("Error parsing syntax: " + ex.ToString());
            }
#else
            catch
            {
                throw;
            }
#endif
        }
    }
}
