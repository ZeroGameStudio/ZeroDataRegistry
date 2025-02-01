// Copyright Zero Games. All Rights Reserved.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ZeroGames.DataRegistry.Compiler.Frontend.Xml;

public partial class XmlCompilerFrontend
{
	
	private string GetName(XElement element)
	{
		string? name = element.Attribute(_nameAttributeName)?.Value;
		if (!IsValidIdentifier(name))
		{
			throw new ParserException();
		}

		return name;
	}
	
	private string GetNamespace(XElement element)
	{
		string? name = element.Attribute(_namespaceAttributeName)?.Value;
		if (string.IsNullOrWhiteSpace(name))
		{
			return string.Empty;
		}
		
		if (!IsValidNamespace(name))
		{
			throw new ParserException();
		}

		return name;
	}
	
	private bool IsValidIdentifier([NotNullWhen(true)] string? identifier)
	{
		if (string.IsNullOrWhiteSpace(identifier))
		{
			return false;
		}

		if (_reservedKeywords.Contains(identifier))
		{
			return false;
		}

		return _identifierRegex.IsMatch(identifier);
	}
	
	private bool IsValidNamespace([NotNullWhen(true)] string? identifier)
	{
		if (string.IsNullOrWhiteSpace(identifier))
		{
			return false;
		}

		if (_reservedKeywords.Contains(identifier))
		{
			return false;
		}

		return _namespaceRegex.IsMatch(identifier);
	}

	private static readonly XName _schemaElementName = "Schema";
	private static readonly XName _usingElementName = "Using";
	private static readonly XName _metadataElementName = "Metadata";
	private static readonly XName _entityElementName = "Entity";
	private static readonly XName _structElementName = "Struct";
	private static readonly XName _enumElementName = "Enum";
	private static readonly XName _primaryKeyElementName = "PrimaryKey";
	private static readonly XName _foreignKeyElementName = "ForeignKey";
	private static readonly XName _propertyElementName = "Property";
	private static readonly XName _enumElementElementName = "Element";

	private static readonly XName _uriAttributeName = "uri";
	private static readonly XName _nameAttributeName = "name";
	private static readonly XName _namespaceAttributeName = "namespace";
	private static readonly XName _typeAttributeName = "type";
	private static readonly XName _extendsAttributeName = "extends";
	private static readonly XName _keyAttributeName = "key";
	private static readonly XName _valueAttributeName = "value";
	private static readonly XName _defaultAttributeName = "default";

	[GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
	private static partial Regex _identifierRegex { get; }
	
	[GeneratedRegex("^([A-Za-z_][A-Za-z0-9_]*)(\\.[A-Za-z_][A-Za-z0-9_]*)*$")]
	private static partial Regex _namespaceRegex { get; }
	
	private static readonly string[] _reservedKeywords = [
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
		"decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", 
		"fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
		"long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", 
		"public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
		"switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
		"void", "volatile", "while"
	];
	
}


